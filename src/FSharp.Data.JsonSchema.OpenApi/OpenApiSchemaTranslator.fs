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

    /// Builds a schema wrapping a single reference. When `document` is supplied, the
    /// reference is bound to it so `.Target` resolves once the id is registered via
    /// `document.AddComponent` — see `translateCore`. When `document` is `None` (the
    /// document-agnostic `translate` entry point), the reference is left unbound, matching
    /// today's behavior for callers that only inspect the translated shape.
    let private mkRefSchema (document: OpenApiDocument option) (typeId: string) : OpenApiSchema =
        let s = mkSchema ()
        let reference =
            match document with
            | Some doc -> OpenApiSchemaReference(typeId, doc)
            | None -> OpenApiSchemaReference(typeId, null)
        s.AnyOf.Add(reference)
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

    // ── Format-only definition inlining ──

    let rec private inlineNode (inlineable: Map<string, SchemaNode>) (node: SchemaNode) : SchemaNode =
        match node with
        | SchemaNode.Ref typeId ->
            match Map.tryFind typeId inlineable with
            | Some prim -> prim
            | None -> node
        | SchemaNode.Object obj ->
            SchemaNode.Object
                { obj with
                    Properties = obj.Properties |> List.map (fun p -> { p with Schema = inlineNode inlineable p.Schema }) }
        | SchemaNode.Array items -> SchemaNode.Array(inlineNode inlineable items)
        | SchemaNode.AnyOf schemas -> SchemaNode.AnyOf(schemas |> List.map (inlineNode inlineable))
        | SchemaNode.OneOf(schemas, discriminator) -> SchemaNode.OneOf(schemas |> List.map (inlineNode inlineable), discriminator)
        | SchemaNode.Nullable inner -> SchemaNode.Nullable(inlineNode inlineable inner)
        | SchemaNode.Map valueSchema -> SchemaNode.Map(inlineNode inlineable valueSchema)
        | SchemaNode.Primitive _
        | SchemaNode.Enum _
        | SchemaNode.Const _
        | SchemaNode.Any -> node

    /// A definition that's just a bare format-annotated primitive (as produced for
    /// DateTime, Guid, Uri, TimeSpan, etc.) doesn't need its own component schema —
    /// a $ref to it is a needless indirection that shows up as an extra, oddly-named
    /// component in the OpenAPI document (see #29). Inline every reference to such a
    /// definition directly and drop the now-unreferenced definition.
    let inlineFormatOnlyDefinitions (doc: SchemaDocument) : SchemaDocument =
        let inlineable =
            doc.Definitions
            |> List.choose (fun (key, value) ->
                match value with
                | SchemaNode.Primitive _ -> Some(key, value)
                | _ -> None)
            |> Map.ofList
        if Map.isEmpty inlineable then
            doc
        else
            { Root = inlineNode inlineable doc.Root
              Definitions =
                doc.Definitions
                |> List.filter (fun (key, _) -> not (Map.containsKey key inlineable))
                |> List.map (fun (key, value) -> key, inlineNode inlineable value) }

    // ── Core translation ──

    /// Shared translation implementation. `rootTypeId` names the component a self-ref
    /// ("#") binds to. `document`, when supplied (net10 only), is the live OpenApiDocument
    /// to register component schemas into and bind references against, so they actually
    /// resolve once ASP.NET walks the tree. When `None`, references are left unbound,
    /// which is correct for the document-agnostic `translate` entry point.
    let private translateCore
        (doc: SchemaDocument)
        (rootTypeId: string)
        (document: OpenApiDocument option)
        : OpenApiSchema * Map<string, OpenApiSchema> =
        let componentSchemas = Collections.Generic.Dictionary<string, OpenApiSchema>()
        let rootSchema = mkSchema ()

        // Case/definition-level ids are qualified with `rootTypeId` only when bound to a live
        // document, so two different types that happen to share a case name (e.g. both having
        // an "Error" case) register distinct components instead of the second silently
        // overwriting the first. The document-agnostic `translate` entry point (document = None)
        // keeps ids unqualified, since it never registers into a shared namespace.
        // The "." separator (valid in OpenAPI component ids) is required, not cosmetic: without
        // it, two different (rootTypeId, typeId) pairs can concatenate to the same string (e.g.
        // "Order" + "LineItem" = "OrderLine" + "Item" = "OrderLineItem") — same collision class
        // this qualification exists to close, just narrower. Neither a .NET Type.Name nor an
        // F# union case name can contain ".", so the split stays unambiguous.
        let qualify (typeId: string) : string =
            match document with
            | Some _ -> rootTypeId + "." + typeId
            | None -> typeId

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
                let resolvedId = if typeId = "#" then rootTypeId else qualify typeId
#if NET10_0_OR_GREATER
                mkRefSchema document resolvedId
#else
                mkRefSchema resolvedId
#endif

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

        // Translate definitions into component schemas, registering each into the live
        // document (when supplied) as it's produced.
        for (key, value) in doc.Definitions do
            let componentSchema = translateNode value
            let registeredKey = qualify key
            componentSchemas.[registeredKey] <- componentSchema
#if NET10_0_OR_GREATER
            document |> Option.iter (fun d -> d.AddComponent(registeredKey, componentSchema) |> ignore)
#endif

        // Translate root
        let translated = translateNode doc.Root

        // If no definitions, return translated directly.
        // Otherwise copy into rootSchema (which is pre-allocated for self-references),
        // and register it under rootTypeId so a "#" self-ref elsewhere resolves to it.
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
#if NET10_0_OR_GREATER
                // AddComponent is TryAdd (never throws, never overwrites); this runs before ASP.NET's own
                // schema-id registration for the same type, so ours wins on an id collision — relied upon, not accidental.
                document |> Option.iter (fun d -> d.AddComponent(rootTypeId, rootSchema) |> ignore)
#endif
                rootSchema

        (result, componentSchemas |> Seq.map (fun kv -> kv.Key, kv.Value) |> Map.ofSeq)

    /// Translate a SchemaDocument to an OpenApiSchema and component schemas.
    /// On net10, self-refs and component references are left unbound (no host document) —
    /// suitable for structural inspection but not for live OpenAPI document generation,
    /// where `translateForDocument` must be used instead so references actually resolve.
    /// On net9, references are always produced via `OpenApiReference` metadata directly;
    /// this distinction doesn't apply there.
    let translate (doc: SchemaDocument) : OpenApiSchema * Map<string, OpenApiSchema> =
        translateCore doc "root" None

#if NET10_0_OR_GREATER
    /// Translate a SchemaDocument, binding component and self-ref references to a live
    /// OpenApiDocument so they resolve correctly, and registering component schemas
    /// — including the root schema itself, under `rootTypeId`, whenever there are any
    /// definitions — into `document.Components.Schemas`. The returned component map
    /// mirrors `translate`'s signature; registration already happened as a side effect
    /// against `document`, so callers that only need the live document can discard it.
    let translateForDocument (doc: SchemaDocument) (rootTypeId: string) (document: OpenApiDocument) : OpenApiSchema * Map<string, OpenApiSchema> =
        if String.IsNullOrEmpty rootTypeId then
            invalidArg (nameof rootTypeId) "rootTypeId must not be null or empty"
        translateCore doc rootTypeId (Some document)
#endif
