namespace FSharp.Data.JsonSchema

open System
open Microsoft.FSharp.Reflection
open NJsonSchema
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
        && typedefof<List<_>> = y.GetGenericTypeDefinition()

    let isOption (y: System.Type) =
        y.IsGenericType
        && typedefof<_ option> = y.GetGenericTypeDefinition()

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
    let isPrimitive (ty: Type) = ty.IsPrimitive || ty = typeof<String>

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

            // Add schemas for each case.
            for case in cases do
                let fields = case.GetFields()

                let caseSchema =
                    if Array.isEmpty fields then
                        let s =
                            JsonSchema(Type = JsonObjectType.String, Default = case.Name)

                        s.Enumeration.Add(case.Name)
                        s.EnumerationNames.Add(case.Name)
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

                                let fieldSchema = context.Generator.Generate(innerTy)

                                if not (isPrimitive innerTy) then
                                    context.Resolver.AppendSchema(fieldSchema, typeNameHint = innerTy.Name)

                                let prop =
                                    if isPrimitive field.PropertyType then
                                        JsonSchemaProperty(Type = fieldSchema.Type)
                                    else
                                        context.Resolver.AppendSchema(
                                            fieldSchema,
                                            typeNameHint = field.PropertyType.Name
                                        )

                                        JsonSchemaProperty(Reference = fieldSchema)

                                s.Properties.Add(camelCaseFieldName, prop)
                            else
                                let fieldSchema =
                                    context.Generator.Generate(field.PropertyType)

                                let prop =
                                    if isPrimitive field.PropertyType then
                                        JsonSchemaProperty(Type = fieldSchema.Type)
                                    else
                                        context.Resolver.AppendSchema(
                                            fieldSchema,
                                            typeNameHint = field.PropertyType.Name
                                        )

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

[<AbstractClass; Sealed>]
type Generator private () =
    static let cache =
        Collections.Concurrent.ConcurrentDictionary<string * Type, JsonSchema>()

    static member internal CreateInternal(?casePropertyName) =
        let settings =
            JsonSchemaGeneratorSettings(
                SerializerOptions = FSharp.Data.Json.DefaultOptions,
                DefaultReferenceTypeNullHandling = ReferenceTypeNullHandling.NotNull
            )

        settings.SchemaProcessors.Add(OptionSchemaProcessor())
        settings.SchemaProcessors.Add(SingleCaseDuSchemaProcessor())
        settings.SchemaProcessors.Add(MultiCaseDuSchemaProcessor(?casePropertyName = casePropertyName))
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
