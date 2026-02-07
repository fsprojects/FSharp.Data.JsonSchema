namespace FSharp.Data.JsonSchema

open System
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open Namotion.Reflection
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
        && (typedefof<List<_>>.Equals(y.GetGenericTypeDefinition())
            || typedefof<list<_>>.Equals(y.GetGenericTypeDefinition()))

    let isOption (y: System.Type) =
        y.IsGenericType
        &&
        let def = y.GetGenericTypeDefinition()
        def = typedefof<_ option> || def = typedefof<voption<_>>

    let isObjOption (y: System.Type) =
        y = typedefof<_ option> || y = typedefof<voption<_>>

    let isPrimitive (ty: Type) =
        ty.IsPrimitive || ty = typeof<String> || ty = typeof<Decimal>

    let isIntegerEnum (ty: Type) =
        ty.IsEnum && ty.GetEnumUnderlyingType() = typeof<int>

[<Obsolete("No longer used internally. Use FSharp.Data.JsonSchema.Core.SchemaAnalyzer instead.")>]
module Dictionary =
    let getUniqueKey (dict: IDictionary<string, 'T>) (key: string) =
        let mutable i = 0
        let mutable newKey = key

        while dict.ContainsKey(newKey) do
            i <- i + 1
            newKey <- sprintf "%s%d" key i

        newKey

[<Obsolete("No longer used internally. Use FSharp.Data.JsonSchema.Core.SchemaAnalyzer instead.")>]
type OptionSchemaProcessor() =
    member this.Process(context: SchemaProcessorContext) =
        if
            isNull context.Schema.Reference
            && Reflection.isOption context.ContextualType.Type
        then
            let schema = context.Schema
            let cases = FSharpType.GetUnionCases(context.ContextualType.Type)

            let schemaType =
                [| for case in cases do
                       match case.Name with
                       | "None" | "ValueNone" -> yield JsonObjectType.Null
                       | _ ->
                           let field = case.GetFields() |> Array.head

                           let schema =
                               context.Generator.Generate(field.PropertyType, context.Resolver)

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

[<Obsolete("No longer used internally. Use FSharp.Data.JsonSchema.Core.SchemaAnalyzer instead.")>]
type SingleCaseDuSchemaProcessor() =

    member this.Process(context: SchemaProcessorContext) =
        if
            isNull context.Schema.Reference
            && FSharpType.IsUnion(context.ContextualType.Type)
            && Reflection.allCasesEmpty context.ContextualType.Type
        then
            let schema = context.Schema
            schema.Type <- JsonObjectType.String
            let cases = FSharpType.GetUnionCases(context.ContextualType.Type)

            for case in cases do
                schema.Enumeration.Add(case.Name)
                schema.EnumerationNames.Add(case.Name)

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)

[<Obsolete("No longer used internally. Use FSharp.Data.JsonSchema.Core.SchemaAnalyzer instead.")>]
type MultiCaseDuSchemaProcessor(?casePropertyName) =
    let casePropertyName = defaultArg casePropertyName "kind"

    member this.Process(context: SchemaProcessorContext) =
        if
            isNull context.Schema.Reference
            && FSharpType.IsUnion(context.ContextualType.Type)
            && not (Reflection.allCasesEmpty context.ContextualType.Type)
            && not (Reflection.isList context.ContextualType.Type)
            && not (Reflection.isOption context.ContextualType.Type)
        then
            let cases = FSharpType.GetUnionCases(context.ContextualType.Type)

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

                            let generate ( t : Type) =
                                    let isIntegerEnum = Reflection.isIntegerEnum t
                                    if context.Resolver.HasSchema(t, isIntegerEnum) then
                                        context.Resolver.GetSchema(t, isIntegerEnum)
                                    else
                                        let s = context.Generator.Generate(t, context.Resolver)
                                        if (not << Reflection.isPrimitive ) t
                                             && not (context.Resolver.HasSchema(t, isIntegerEnum))
                                         then
                                              context.Resolver.AddSchema(t, isIntegerEnum, s)
                                        s

                            if Reflection.isOption field.PropertyType then
                                let innerTy =
                                    field.PropertyType.GetGenericArguments().[0]

                                let fieldSchema = generate innerTy

                                let prop =
                                    if Reflection.isPrimitive innerTy then
                                        JsonSchemaProperty(Type = fieldSchema.Type)
                                    else
                                        JsonSchemaProperty(Reference = fieldSchema)

                                s.Properties.Add(camelCaseFieldName, prop)
                            else
                                let fieldSchema = generate field.PropertyType

                                let prop =
                                    if Reflection.isPrimitive field.PropertyType then
                                        JsonSchemaProperty(Type = fieldSchema.Type, Format = fieldSchema.Format)
                                    else
                                        JsonSchemaProperty(Reference = fieldSchema)

                                s.Properties.Add(camelCaseFieldName, prop)
                                s.RequiredProperties.Add(camelCaseFieldName)
                        s

                // Attach each case definition.
                let name = Dictionary.getUniqueKey schema.Definitions case.Name
                // printfn "Adding case %s to dict: %A" name schema.Definitions
                schema.Definitions.Add(name, caseSchema)
                // Add each schema to the anyOf collection.
                schema.AnyOf.Add(JsonSchema(Reference = caseSchema))

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)


[<Obsolete("No longer used internally. Use FSharp.Data.JsonSchema.Core.SchemaAnalyzer instead.")>]
type RecordSchemaProcessor() =

    let isNullableProperty(property: JsonSchemaProperty) =
        property.Type.HasFlag JsonObjectType.Null
        || property.OneOf |> Seq.exists (fun s -> s.Type.HasFlag JsonObjectType.Null)

    member this.Process(context: SchemaProcessorContext) =
        if
            isNull context.Schema.Reference
            && FSharpType.IsRecord(context.ContextualType.Type)
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

        if Reflection.isObjOption cachedType.Type then
            "Any"
        elif Reflection.isOption cachedType.Type then
            this.Generate(cachedType.GenericArguments.[0].OriginalType)
        else
            base.Generate(ty)

[<AbstractClass; Sealed>]
type Generator private () =
    static let cache =
        Collections.Concurrent.ConcurrentDictionary<(string * Core.UnionEncodingStyle) * Type, JsonSchema>()

    static member internal CreateInternal(?casePropertyName, ?unionEncoding) =
        let casePropertyName' = defaultArg casePropertyName FSharp.Data.Json.DefaultCasePropertyName
        let nameGen = SchemaNameGenerator()
        let config =
            { Core.SchemaGeneratorConfig.defaults with
                DiscriminatorPropertyName = casePropertyName'
                UnionEncoding = defaultArg unionEncoding Core.SchemaGeneratorConfig.defaults.UnionEncoding }

        // Collect all types referenced from a root type, keyed by their typeId.
        let collectTypeMap (rootType: Type) =
            let visited = HashSet<Type>()
            let typeByName = Dictionary<string, Type>()
            let rec walk (t: Type) =
                if visited.Add t then
                    let typeId = config.TypeIdResolver t
                    if not (String.IsNullOrEmpty typeId) then
                        typeByName.[typeId] <- t
                    if FSharpType.IsRecord(t, true) then
                        for f in FSharpType.GetRecordFields(t, true) do walk f.PropertyType
                    elif FSharpType.IsUnion(t, true) then
                        for c in FSharpType.GetUnionCases(t, true) do
                            for f in c.GetFields() do walk f.PropertyType
                    elif t.IsArray then
                        walk (t.GetElementType())
                    elif t.IsGenericType then
                        for a in t.GetGenericArguments() do walk a
            walk rootType
            typeByName

        fun (ty: Type) ->
            let doc = Core.SchemaAnalyzer.analyze config ty
            let schema = NJsonSchemaTranslator.translate doc
            // Set title using the same logic as the old SchemaNameGenerator
            // Don't set title for bare option/voption types (they produce empty schemas)
            match doc.Root with
            | Core.SchemaNode.Any -> ()
            | _ ->
                let title = nameGen.Generate(ty)
                if not (System.String.IsNullOrEmpty title) then
                    schema.Title <- title
            // Add empty description for .NET enums (matching NJsonSchema behavior)
            if Reflection.isIntegerEnum ty then
                schema.Description <- ""
            // Set additionalProperties = false for fieldless DU enums
            if FSharpType.IsUnion(ty) && Reflection.allCasesEmpty ty then
                schema.AllowAdditionalProperties <- false
            // Apply post-processing to definitions based on their F# types
            let typeMap = collectTypeMap ty
            for kv in schema.Definitions do
                match typeMap.TryGetValue(kv.Key) with
                | true, defTy ->
                    if Reflection.isIntegerEnum defTy then
                        kv.Value.Description <- ""
                    elif FSharpType.IsUnion(defTy, true) && Reflection.allCasesEmpty defTy then
                        kv.Value.AllowAdditionalProperties <- false
                | _ -> ()
            // Apply DataAnnotation attributes from record fields
            let applyAnnotations (recordTy: Type) (targetSchema: JsonSchema) =
                if FSharpType.IsRecord(recordTy, true) then
                    for field in FSharpType.GetRecordFields(recordTy, true) do
                        let propName = config.PropertyNamingPolicy field.Name
                        match targetSchema.Properties.TryGetValue(propName) with
                        | true, prop ->
                            for attr in field.GetCustomAttributes(true) do
                                match attr with
                                | :? System.ComponentModel.DataAnnotations.RequiredAttribute ->
                                    prop.MinLength <- 1
                                | :? System.ComponentModel.DataAnnotations.MaxLengthAttribute as ml ->
                                    prop.MaxLength <- Nullable ml.Length
                                | :? System.ComponentModel.DataAnnotations.RangeAttribute as r ->
                                    prop.Minimum <- Nullable (Convert.ToDecimal(r.Minimum :> obj))
                                    prop.Maximum <- Nullable (Convert.ToDecimal(r.Maximum :> obj))
                                | _ -> ()
                        | _ -> ()
            applyAnnotations ty schema
            for kv in schema.Definitions do
                match typeMap.TryGetValue(kv.Key) with
                | true, defTy -> applyAnnotations defTy kv.Value
                | _ -> ()
            schema

    /// Creates a generator using the specified casePropertyName and unionEncoding.
    static member Create(?casePropertyName, ?unionEncoding) =
        Generator.CreateInternal(?casePropertyName = casePropertyName, ?unionEncoding = unionEncoding)

    /// Creates a memoized generator that stores generated schemas in a global cache by Type and casePropertyName.
    static member CreateMemoized(?casePropertyName, ?unionEncoding) =
        let casePropertyName =
            defaultArg casePropertyName FSharp.Data.Json.DefaultCasePropertyName
        let unionEncoding =
            defaultArg unionEncoding Core.SchemaGeneratorConfig.defaults.UnionEncoding

        fun ty ->
            cache.GetOrAdd(
                ((casePropertyName, unionEncoding), ty),
                let generator =
                    Generator.CreateInternal(casePropertyName, unionEncoding)

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
