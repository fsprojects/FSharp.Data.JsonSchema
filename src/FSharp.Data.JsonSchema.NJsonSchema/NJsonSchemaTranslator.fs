namespace FSharp.Data.JsonSchema

open System.Collections.Generic
open NJsonSchema
open FSharp.Data.JsonSchema.Core

/// Translates Core SchemaDocument to NJsonSchema.JsonSchema objects.
module internal NJsonSchemaTranslator =

    let private mapPrimitiveType (pt: PrimitiveType) =
        match pt with
        | PrimitiveType.String -> JsonObjectType.String
        | PrimitiveType.Integer -> JsonObjectType.Integer
        | PrimitiveType.Number -> JsonObjectType.Number
        | PrimitiveType.Boolean -> JsonObjectType.Boolean

    let private mkSchema ty =
        let s = JsonSchema()
        s.Type <- ty
        s

    let private mkSchemaWithFormat ty fmt =
        let s = JsonSchema()
        s.Type <- ty
        s.Format <- fmt
        s

    let private mkProp ty =
        let p = JsonSchemaProperty()
        p.Type <- ty
        p

    let private mkPropWithFormat ty fmt =
        let p = JsonSchemaProperty()
        p.Type <- ty
        p.Format <- fmt
        p

    let private mkRefSchema (target: JsonSchema) =
        let s = JsonSchema()
        s.Reference <- target
        s

    let private mkRefProp (target: JsonSchema) =
        let p = JsonSchemaProperty()
        p.Reference <- target
        p

    let private mkConstSchema value =
        let s = JsonSchema()
        s.Type <- JsonObjectType.String
        s.Default <- value
        s.Enumeration.Add(value)
        s.EnumerationNames.Add(value)
        s.AllowAdditionalProperties <- false
        s

    let private mkConstProp value =
        let p = JsonSchemaProperty()
        p.Type <- JsonObjectType.String
        p.Default <- value
        p.Enumeration.Add(value)
        p.EnumerationNames.Add(value)
        p

    let private getOrCreateDef (defs: Dictionary<string, JsonSchema>) typeId =
        match defs.TryGetValue typeId with
        | true, existing -> existing
        | false, _ ->
            let placeholder = JsonSchema()
            defs.[typeId] <- placeholder
            placeholder

    /// Translate a SchemaNode to a JsonSchemaProperty for use in object properties.
    let rec private translateProp
        (rootSchema: JsonSchema)
        (parentSchema: JsonSchema)
        (defs: Dictionary<string, JsonSchema>)
        (node: SchemaNode)
        : JsonSchemaProperty =

        match node with
        | SchemaNode.Ref "#" ->
            mkRefProp rootSchema

        | SchemaNode.Ref typeId ->
            mkRefProp (getOrCreateDef defs typeId)

        | SchemaNode.Primitive(pt, format) ->
            match format with
            | Some f -> mkPropWithFormat (mapPrimitiveType pt) f
            | None -> mkProp (mapPrimitiveType pt)

        | SchemaNode.Const(value, _) ->
            mkConstProp value

        | SchemaNode.Nullable inner ->
            match inner with
            | SchemaNode.Primitive(pt, format) ->
                match format with
                | Some f -> mkPropWithFormat (mapPrimitiveType pt ||| JsonObjectType.Null) f
                | None -> mkProp (mapPrimitiveType pt ||| JsonObjectType.Null)
            | SchemaNode.Array items ->
                let p = mkProp (JsonObjectType.Array ||| JsonObjectType.Null)
                p.Item <- translateNode rootSchema parentSchema defs items
                p
            | SchemaNode.Ref typeId ->
                let p = JsonSchemaProperty()
                let nullSchema = mkSchema JsonObjectType.Null
                p.OneOf.Add(nullSchema)
                p.OneOf.Add(mkRefSchema (getOrCreateDef defs typeId))
                p
            | other ->
                let translated = translateNode rootSchema parentSchema defs (SchemaNode.Nullable other)
                let p = JsonSchemaProperty()
                p.Type <- translated.Type
                p.Format <- translated.Format
                p

        | SchemaNode.Array items ->
            let p = mkProp JsonObjectType.Array
            p.Item <- translateNode rootSchema parentSchema defs items
            p

        | SchemaNode.Any ->
            JsonSchemaProperty()

        | other ->
            let translated = translateNode rootSchema parentSchema defs other
            let p = JsonSchemaProperty()
            p.Type <- translated.Type
            p.Format <- translated.Format
            p.AdditionalPropertiesSchema <- translated.AdditionalPropertiesSchema
            p.AllowAdditionalProperties <- translated.AllowAdditionalProperties
            // Copy AnyOf and OneOf collections for Choice types and other polymorphic schemas
            for item in translated.AnyOf do
                p.AnyOf.Add(item)
            for item in translated.OneOf do
                p.OneOf.Add(item)
            // Copy Properties for nested Object schemas (e.g., AdjacentTag fields property, anonymous records)
            // Only copy if Type is Object to avoid issues with other schema types
            if translated.Type.HasFlag(JsonObjectType.Object) && translated.Properties.Count > 0 then
                for kv in translated.Properties do
                    p.Properties.Add(kv.Key, kv.Value)
                // Copy Required properties
                for req in translated.RequiredProperties do
                    p.RequiredProperties.Add(req)
            p

    /// Translate a SchemaNode to a JsonSchema.
    and private translateNode
        (rootSchema: JsonSchema)
        (parentSchema: JsonSchema)
        (defs: Dictionary<string, JsonSchema>)
        (node: SchemaNode)
        : JsonSchema =

        match node with
        | SchemaNode.Primitive(pt, format) ->
            match format with
            | Some f -> mkSchemaWithFormat (mapPrimitiveType pt) f
            | None -> mkSchema (mapPrimitiveType pt)

        | SchemaNode.Any ->
            JsonSchema()

        | SchemaNode.Nullable inner ->
            match inner with
            | SchemaNode.Primitive(pt, format) ->
                match format with
                | Some f -> mkSchemaWithFormat (mapPrimitiveType pt ||| JsonObjectType.Null) f
                | None -> mkSchema (mapPrimitiveType pt ||| JsonObjectType.Null)
            | SchemaNode.Array items ->
                let s = mkSchema (JsonObjectType.Array ||| JsonObjectType.Null)
                s.Item <- translateNode rootSchema parentSchema defs items
                s
            | SchemaNode.Ref typeId ->
                let s = JsonSchema()
                s.OneOf.Add(mkSchema JsonObjectType.Null)
                s.OneOf.Add(mkRefSchema (getOrCreateDef defs typeId))
                s
            | other ->
                let inner = translateNode rootSchema parentSchema defs other
                inner.Type <- inner.Type ||| JsonObjectType.Null
                inner

        | SchemaNode.Enum(values, _underlyingType) ->
            let s = mkSchema JsonObjectType.String
            for v in values do
                s.Enumeration.Add(v)
                s.EnumerationNames.Add(v)
            s

        | SchemaNode.Const(value, _pt) ->
            mkConstSchema value

        | SchemaNode.Object obj ->
            let s = mkSchema JsonObjectType.Object
            s.AllowAdditionalProperties <- obj.AdditionalProperties

            for prop in obj.Properties do
                let jsonProp = translateProp rootSchema s defs prop.Schema
                s.Properties.Add(prop.Name, jsonProp)

            for req in obj.Required do
                s.RequiredProperties.Add(req)

            s

        | SchemaNode.Array items ->
            let s = mkSchema JsonObjectType.Array
            s.Item <- translateNode rootSchema parentSchema defs items
            s

        | SchemaNode.AnyOf schemas ->
            let s = JsonSchema()
            s.AllowAdditionalProperties <- true

            for caseSchema in schemas do
                match caseSchema with
                | SchemaNode.Const(value, _) ->
                    let constSchema = mkConstSchema value
                    s.Definitions.Add(value, constSchema)
                    s.AnyOf.Add(mkRefSchema constSchema)

                | SchemaNode.Object obj when obj.TypeId.IsSome ->
                    let caseJsonSchema = translateNode rootSchema s defs caseSchema
                    let key = obj.TypeId.Value
                    s.Definitions.Add(key, caseJsonSchema)
                    s.AnyOf.Add(mkRefSchema caseJsonSchema)

                | other ->
                    let translated = translateNode rootSchema s defs other
                    s.AnyOf.Add(translated)

            s

        | SchemaNode.OneOf(schemas, discriminator) ->
            let s = JsonSchema()
            for schema in schemas do
                let translated = translateNode rootSchema s defs schema
                s.OneOf.Add(translated)
            match discriminator with
            | Some d ->
                let disc = OpenApiDiscriminator()
                disc.PropertyName <- d.PropertyName
                for kv in d.Mapping do
                    disc.Mapping.Add(kv.Key, getOrCreateDef defs kv.Value)
                s.DiscriminatorObject <- disc
            | None -> ()
            s

        | SchemaNode.Ref "#" ->
            mkRefSchema rootSchema

        | SchemaNode.Ref typeId ->
            mkRefSchema (getOrCreateDef defs typeId)

        | SchemaNode.Map valueSchema ->
            let s = mkSchema JsonObjectType.Object
            s.AdditionalPropertiesSchema <- translateNode rootSchema parentSchema defs valueSchema
            s.AllowAdditionalProperties <- true
            s

    let private copySchemaInto (source: JsonSchema) (target: JsonSchema) =
        target.Type <- source.Type
        target.Format <- source.Format
        target.AllowAdditionalProperties <- source.AllowAdditionalProperties

        for kv in source.Properties do
            target.Properties.Add(kv.Key, kv.Value)
        for req in source.RequiredProperties do
            target.RequiredProperties.Add(req)
        for item in source.AnyOf do
            target.AnyOf.Add(item)
        for item in source.OneOf do
            target.OneOf.Add(item)
        for kv in source.Definitions do
            target.Definitions.Add(kv.Key, kv.Value)
        for item in source.Enumeration do
            target.Enumeration.Add(item)
        for name in source.EnumerationNames do
            target.EnumerationNames.Add(name)
        if source.Item <> null then
            target.Item <- source.Item
        if source.Default <> null then
            target.Default <- source.Default

    /// Translate a SchemaDocument to an NJsonSchema.JsonSchema.
    let translate (doc: SchemaDocument) : JsonSchema =
        let defs = Dictionary<string, JsonSchema>()

        // Pre-create all definition schemas so refs can resolve
        for (key, _) in doc.Definitions do
            if not (defs.ContainsKey key) then
                defs.[key] <- JsonSchema()

        let rootSchema = JsonSchema()

        // Translate root
        let translatedRoot = translateNode rootSchema rootSchema defs doc.Root

        // Copy translated root into the root schema object
        copySchemaInto translatedRoot rootSchema

        // Translate and populate definitions
        for (key, value) in doc.Definitions do
            let existing = defs.[key]
            let translated = translateNode rootSchema rootSchema defs value
            copySchemaInto translated existing

            if not (rootSchema.Definitions.ContainsKey key) then
                rootSchema.Definitions.Add(key, existing)

        rootSchema
