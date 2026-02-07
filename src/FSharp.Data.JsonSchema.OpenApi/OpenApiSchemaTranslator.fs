namespace FSharp.Data.JsonSchema.OpenApi

open System
open FSharp.Data.JsonSchema.Core

#if NET10_0_OR_GREATER
open Microsoft.OpenApi
open System.Text.Json.Nodes
#else
open Microsoft.OpenApi.Models
open Microsoft.OpenApi.Any
#endif

// Alias to avoid collision with Microsoft.OpenApi.Any.PrimitiveType on net9.0
type private CorePrimitiveType = FSharp.Data.JsonSchema.Core.PrimitiveType

/// Translates a SchemaDocument (Core IR) to OpenApiSchema.
module OpenApiSchemaTranslator =

    // ── Schema creation helper ──
    // In OpenApi 2.0, the parameterless constructor does not auto-initialize collections.

    let private mkSchema () =
        let s = OpenApiSchema()
#if NET10_0_OR_GREATER
        s.Properties <- Collections.Generic.Dictionary<string, IOpenApiSchema>()
        s.Required <- Collections.Generic.HashSet<string>()
        s.AnyOf <- Collections.Generic.List<IOpenApiSchema>()
        s.OneOf <- Collections.Generic.List<IOpenApiSchema>()
        s.AllOf <- Collections.Generic.List<IOpenApiSchema>()
        s.Enum <- Collections.Generic.List<JsonNode>()
#endif
        s

    // ── Version-abstracted helpers ──

#if NET10_0_OR_GREATER
    let private setType (schema: OpenApiSchema) (pt: CorePrimitiveType) =
        schema.Type <-
            Nullable(
                match pt with
                | CorePrimitiveType.String -> JsonSchemaType.String
                | CorePrimitiveType.Integer -> JsonSchemaType.Integer
                | CorePrimitiveType.Number -> JsonSchemaType.Number
                | CorePrimitiveType.Boolean -> JsonSchemaType.Boolean
            )

    let private setObjectType (schema: OpenApiSchema) =
        schema.Type <- Nullable(JsonSchemaType.Object)

    let private setArrayType (schema: OpenApiSchema) =
        schema.Type <- Nullable(JsonSchemaType.Array)

    let private makeNullable (schema: OpenApiSchema) =
        match schema.Type with
        | t when t.HasValue -> schema.Type <- Nullable(t.Value ||| JsonSchemaType.Null)
        | _ -> schema.Type <- Nullable(JsonSchemaType.Null)

    let private addEnumValue (schema: OpenApiSchema) (value: string) =
        schema.Enum.Add(JsonValue.Create(value))

    let private setDefault (schema: OpenApiSchema) (value: string) =
        schema.Default <- JsonValue.Create(value)

    let private mkRefSchema (typeId: string) : OpenApiSchema =
        let s = mkSchema ()
        s.AnyOf.Add(OpenApiSchemaReference(typeId, null))
        s
#else
    let private setType (schema: OpenApiSchema) (pt: CorePrimitiveType) =
        schema.Type <-
            match pt with
            | CorePrimitiveType.String -> "string"
            | CorePrimitiveType.Integer -> "integer"
            | CorePrimitiveType.Number -> "number"
            | CorePrimitiveType.Boolean -> "boolean"

    let private setObjectType (schema: OpenApiSchema) =
        schema.Type <- "object"

    let private setArrayType (schema: OpenApiSchema) =
        schema.Type <- "array"

    let private makeNullable (schema: OpenApiSchema) =
        schema.Nullable <- true

    let private addEnumValue (schema: OpenApiSchema) (value: string) =
        schema.Enum.Add(OpenApiString(value))

    let private setDefault (schema: OpenApiSchema) (value: string) =
        schema.Default <- OpenApiString(value)

    let private mkRefSchema (typeId: string) : OpenApiSchema =
        let schema = OpenApiSchema()
        schema.Reference <- OpenApiReference(Type = Nullable(ReferenceType.Schema), Id = typeId)
        schema
#endif

    // ── Core translation ──

    /// Translate a SchemaDocument to an OpenApiSchema and component schemas.
    let translate (doc: SchemaDocument) : OpenApiSchema * Map<string, OpenApiSchema> =
        let componentSchemas = Collections.Generic.Dictionary<string, OpenApiSchema>()
        let rootSchema = mkSchema ()

        let rec translateNode (node: SchemaNode) : OpenApiSchema =
            match node with
            | SchemaNode.Object obj ->
                let schema = mkSchema ()
                setObjectType schema
                for prop in obj.Properties do
                    let propSchema = translateNode prop.Schema
                    schema.Properties.[prop.Name] <- propSchema
                for req in obj.Required do
                    schema.Required.Add(req) |> ignore
                schema.AdditionalPropertiesAllowed <- obj.AdditionalProperties
                schema

            | SchemaNode.Array items ->
                let schema = mkSchema ()
                setArrayType schema
                schema.Items <- translateNode items
                schema

            | SchemaNode.AnyOf schemas ->
                let schema = mkSchema ()
                for s in schemas do
                    schema.AnyOf.Add(translateNode s)
                schema

            | SchemaNode.OneOf (schemas, discriminator) ->
                let schema = mkSchema ()
                for s in schemas do
                    schema.OneOf.Add(translateNode s)
                match discriminator with
                | Some disc ->
                    let d = OpenApiDiscriminator()
                    d.PropertyName <- disc.PropertyName
                    schema.Discriminator <- d
                | None -> ()
                schema

            | SchemaNode.Nullable inner ->
                let innerSchema = translateNode inner
                makeNullable innerSchema
                innerSchema

            | SchemaNode.Primitive (pt, fmt) ->
                let schema = mkSchema ()
                setType schema pt
                match fmt with
                | Some f -> schema.Format <- f
                | None -> ()
                schema

            | SchemaNode.Enum (values, _pt) ->
                let schema = mkSchema ()
                setType schema CorePrimitiveType.String
                for v in values do
                    addEnumValue schema v
                schema

            | SchemaNode.Ref typeId ->
                if typeId = "#" then
                    mkRefSchema (rootSchema.Title |> Option.ofObj |> Option.defaultValue "root")
                else
                    mkRefSchema typeId

            | SchemaNode.Map valueSchema ->
                let schema = mkSchema ()
                setObjectType schema
                schema.AdditionalProperties <- translateNode valueSchema
                schema

            | SchemaNode.Const (value, _pt) ->
                let schema = mkSchema ()
                setType schema CorePrimitiveType.String
                addEnumValue schema value
                setDefault schema value
                schema

            | SchemaNode.Any ->
                mkSchema ()

        // Translate definitions into component schemas
        for (key, value) in doc.Definitions do
            componentSchemas.[key] <- translateNode value

        // Translate root
        let translated = translateNode doc.Root

        // If no definitions, return translated directly.
        // Otherwise copy into rootSchema (which is pre-allocated for self-references).
        let result =
            if List.isEmpty doc.Definitions then
                translated
            else
                rootSchema.Type <- translated.Type
                rootSchema.Format <- translated.Format
                rootSchema.Items <- translated.Items
                rootSchema.AdditionalPropertiesAllowed <- translated.AdditionalPropertiesAllowed
                rootSchema.AdditionalProperties <- translated.AdditionalProperties
                rootSchema.Discriminator <- translated.Discriminator
                rootSchema.Default <- translated.Default
#if !NET10_0_OR_GREATER
                rootSchema.Nullable <- translated.Nullable
#endif
                for kv in translated.Properties do
                    rootSchema.Properties.[kv.Key] <- kv.Value
                for req in translated.Required do
                    rootSchema.Required.Add(req) |> ignore
                for s in translated.AnyOf do
                    rootSchema.AnyOf.Add(s)
                for s in translated.OneOf do
                    rootSchema.OneOf.Add(s)
                for e in translated.Enum do
                    rootSchema.Enum.Add(e)
                rootSchema

        (result, componentSchemas |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq)
