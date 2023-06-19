namespace FSharp.Data.JsonSchema

open System
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open Namotion.Reflection
open NJsonSchema
open NJsonSchema.Annotations
open NJsonSchema.Generation

/// Microsoft.FSharp.Reflection helpers
/// see https://github.com/baronfel/Newtonsoft.Json.FSharp.Idiomatic/blob/master/src/Newtonsoft.Json.FSharp.Idiomatic/Newtonsoft.Json.FSharp.Idiomatic.fs#L52-L54
module Reflection =
    let allCasesEmpty (y: System.Type) =
        y
        |> FSharpType.GetUnionCases
        |> Array.forall (fun case -> case.GetFields() |> Array.isEmpty)

    let isList (y: System.Type) =
        y.IsGenericType
        && (typedefof<List<_>>.Equals(y.GetGenericTypeDefinition())
            || typedefof<list<_>>.Equals(y.GetGenericTypeDefinition()))

    let isOption (y: System.Type) =
        y.IsGenericType
        && typedefof<_ option> = y.GetGenericTypeDefinition()
        
    let isPrimitive (ty: Type) =
        ty.IsPrimitive || ty = typeof<String> || ty = typeof<Decimal>

type OptionSchemaProcessor() =
    static let optionTy = typedefof<option<_>>

    member this.Process(context: SchemaProcessorContext) =
        if
            context.Type.IsGenericType
            && optionTy.Equals(context.Type.GetGenericTypeDefinition())
        then
            let schema = context.Schema
            let cases = FSharpType.GetUnionCases(context.Type)

            let schemaType =
                [| for case in cases do
                       match case.Name with
                       | "None" -> yield JsonObjectType.Null
                       | _ ->
                           let field = case.GetFields() |> Array.head

                           let schema =
                               context.Generator.Generate(field.PropertyType)

                           match schema.Type with
                           | JsonObjectType.None ->
                               yield
                                   JsonObjectType.String
                                   ||| JsonObjectType.Number
                                   ||| JsonObjectType.Integer
                                   ||| JsonObjectType.Boolean
                                   ||| JsonObjectType.Object
                                   ||| JsonObjectType.Array
                           | ty -> yield ty |]
                |> Array.reduce (|||)

            schema.Type <- schemaType

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)

type SingleCaseDuSchemaProcessor() =

    member this.Process(context: SchemaProcessorContext) =
        if
            FSharpType.IsUnion(context.Type)
            && Reflection.allCasesEmpty context.Type
        then
            let schema = context.Schema
            schema.Type <- JsonObjectType.String
            let cases = FSharpType.GetUnionCases(context.Type)

            for case in cases do
                schema.Enumeration.Add(case.Name)
                schema.EnumerationNames.Add(case.Name)

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)

type MultiCaseDuSchemaProcessor(?casePropertyName) =
    let casePropertyName = defaultArg casePropertyName "kind"

    member this.Process(context: SchemaProcessorContext) =
        if
            FSharpType.IsUnion(context.Type)
            && not (Reflection.allCasesEmpty context.Type)
            && not (Reflection.isList context.Type)
            && not (Reflection.isOption context.Type)
        then
            let cases = FSharpType.GetUnionCases(context.Type)

            // Set the core schema definition.
            let schema = context.Schema
            schema.Type <- JsonObjectType.None
            schema.IsAbstract <- false
            schema.AllowAdditionalProperties <- true

            let fieldSchemaCache = Dictionary<Type, JsonSchema>()

            // Add schemas for each case.
            for case in cases do
                let fields = case.GetFields()

                let caseSchema =
                    if Array.isEmpty fields then
                        let s =
                            JsonSchema(Type = JsonObjectType.String, Default = case.Name)

                        s.Enumeration.Add(case.Name)
                        s.EnumerationNames.Add(case.Name)
                        s.AllowAdditionalProperties <- false
                        s
                    else
                        // Create the schema for the additional properties.
                        let s = JsonSchema(Type = JsonObjectType.Object)

                        // Add the discriminator property
                        let caseProp =
                            JsonSchemaProperty(Type = JsonObjectType.String, Default = case.Name)

                        caseProp.Enumeration.Add(case.Name)
                        caseProp.EnumerationNames.Add(case.Name)
                        s.Properties.Add(casePropertyName, caseProp)
                        s.RequiredProperties.Add(casePropertyName)
                        s.AllowAdditionalProperties <- false

                        // Add the remaining fields
                        for field in fields do
                            let camelCaseFieldName =
                                if String.IsNullOrEmpty(field.Name) then
                                    field.Name
                                elif String.length field.Name = 1 then
                                    string (Char.ToLowerInvariant field.Name.[0])
                                else
                                    string (Char.ToLowerInvariant field.Name.[0])
                                    + field.Name.Substring(1)

                            if field.PropertyType.IsGenericType
                               && field.PropertyType.GetGenericTypeDefinition() = typedefof<option<_>> then
                                let innerTy =
                                    field.PropertyType.GetGenericArguments().[0]

                                let fieldSchema =
                                    match fieldSchemaCache.TryGetValue innerTy with
                                    | true, fs -> fs
                                    | _ -> context.Generator.Generate(innerTy)

                                let prop =
                                    if Reflection.isPrimitive innerTy then
                                        JsonSchemaProperty(Type = fieldSchema.Type)
                                    else
                                        if not (fieldSchemaCache.ContainsKey innerTy) then
                                            context.Resolver.AppendSchema(fieldSchema, typeNameHint = innerTy.Name)
                                            fieldSchemaCache.Add(innerTy, fieldSchema)

                                        JsonSchemaProperty(Reference = fieldSchema)

                                s.Properties.Add(camelCaseFieldName, prop)
                            else
                                let fieldSchema =
                                    match fieldSchemaCache.TryGetValue field.PropertyType with
                                    | true, fs -> fs
                                    | _ -> context.Generator.Generate(field.PropertyType)

                                let prop =
                                    if Reflection.isPrimitive field.PropertyType then
                                        JsonSchemaProperty(Type = fieldSchema.Type, Format = fieldSchema.Format)
                                    else
                                        if not (fieldSchemaCache.ContainsKey field.PropertyType) then
                                            context.Resolver.AppendSchema(
                                                fieldSchema,
                                                typeNameHint = field.PropertyType.Name
                                            )

                                            fieldSchemaCache.Add(field.PropertyType, fieldSchema)

                                        JsonSchemaProperty(Reference = fieldSchema)

                                s.Properties.Add(camelCaseFieldName, prop)
                                s.RequiredProperties.Add(camelCaseFieldName)
                        s

                // Attach each case definition.
                schema.Definitions.Add(case.Name, caseSchema)
                // Add each schema to the anyOf collection.
                schema.AnyOf.Add(JsonSchema(Reference = caseSchema))

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)


type RecordSchemaProcessor() =

    let isNullableProperty(property: JsonSchemaProperty) =
        property.Type.HasFlag JsonObjectType.Null
        || property.OneOf |> Seq.exists (fun s -> s.Type.HasFlag JsonObjectType.Null)

    member this.Process(context: SchemaProcessorContext) =
        if
            FSharpType.IsRecord(context.Type)
        then
            let schema = context.Schema

            for KeyValue(propertyName, property) in schema.Properties do
                 if (not << isNullableProperty) property then
                    property.IsRequired <- true

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)


        

[<Sealed>]
type internal SchemaNameGenerator() =
    inherit DefaultSchemaNameGenerator()

    override this.Generate(ty: Type) =
        let cachedType = ty.ToCachedType()

        if cachedType.Type = typeof<option<obj>> then
            "Any"
        elif cachedType.Type.IsGenericType
           && cachedType.Type.GetGenericTypeDefinition() = typedefof<option<_>> then
            this.Generate(cachedType.GenericArguments.[0].OriginalType)
        else
            base.Generate(ty)

[<Sealed>]
type internal ReflectionService() =
    inherit DefaultReflectionService()

    override this.GetDescription(contextualType, defaultReferenceTypeNullHandling, settings) =
        if contextualType.Type = typeof<option<obj>> then
            JsonTypeDescription.Create(contextualType, JsonObjectType.Object, true, null)
        elif contextualType.Type.IsConstructedGenericType
           && contextualType.Type.GetGenericTypeDefinition() = typedefof<option<_>> then
            let typeDescription =
                this.GetDescription(
                    contextualType.OriginalGenericArguments.[0],
                    defaultReferenceTypeNullHandling,
                    settings
                )

            typeDescription.IsNullable <- true
            typeDescription
        else
            base.GetDescription(contextualType, defaultReferenceTypeNullHandling, settings)


[<AbstractClass; Sealed>]
type Generator private () =
    static let cache =
        Collections.Concurrent.ConcurrentDictionary<string * Type, JsonSchema>()

    static member internal CreateInternal(?casePropertyName) =
        let settings =
            JsonSchemaGeneratorSettings(
                SerializerOptions = FSharp.Data.Json.DefaultOptions,
                DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull,
                ReflectionService = ReflectionService(),
                SchemaNameGenerator = SchemaNameGenerator(),
                UseXmlDocumentation = true
            )

        settings.SchemaProcessors.Add(OptionSchemaProcessor())
        settings.SchemaProcessors.Add(SingleCaseDuSchemaProcessor())
        settings.SchemaProcessors.Add(MultiCaseDuSchemaProcessor(?casePropertyName = casePropertyName))
        settings.SchemaProcessors.Add(RecordSchemaProcessor())
        fun ty -> JsonSchema.FromType(ty, settings)

    /// Creates a generator using the specified casePropertyName and generationProviders.
    static member Create(?casePropertyName) =
        Generator.CreateInternal(?casePropertyName = casePropertyName)

    /// Creates a memoized generator that stores generated schemas in a global cache by Type.
    static member CreateMemoized(?casePropertyName) =
        let casePropertyName =
            defaultArg casePropertyName FSharp.Data.Json.DefaultCasePropertyName

        fun ty ->
            cache.GetOrAdd(
                (casePropertyName, ty),
                let generator =
                    Generator.CreateInternal(casePropertyName)

                generator ty
            )

module Validation =

    let validate schema (json: string) =
        let validator = Validation.JsonSchemaValidator()
        let errors = validator.Validate(json, schema)

        if errors.Count > 0 then
            Error(Seq.toArray errors)
        else
            Ok()

    type FSharp.Data.Json with

        static member DeserializeWithValidation<'T>(json, schema) =
            validate schema json
            |> Result.map (fun _ -> FSharp.Data.Json.Deserialize<'T> json)

        static member DeserializeWithValidation<'T>(json, schema, casePropertyName) =
            validate schema json
            |> Result.map (fun _ -> FSharp.Data.Json.Deserialize<'T>(json, casePropertyName))
