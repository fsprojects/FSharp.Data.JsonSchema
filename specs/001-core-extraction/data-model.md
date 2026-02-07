# Data Model: Core Extraction and Multi-Target Architecture

## Core IR Types (FSharp.Data.JsonSchema.Core)

### SchemaNode — Root IR Type

A closed discriminated union representing one node in a JSON Schema document.
Each translator (NJsonSchema, OpenApi) pattern-matches over this type exhaustively.

```
SchemaNode
├── Object of ObjectSchema
├── Array of items: SchemaNode
├── AnyOf of schemas: SchemaNode list
├── OneOf of schemas: SchemaNode list * discriminator: Discriminator option
├── Nullable of inner: SchemaNode
├── Primitive of primitiveType: PrimitiveType * format: string option
├── Enum of values: string list * underlyingType: PrimitiveType
├── Ref of typeId: string
├── Map of valueSchema: SchemaNode
├── Const of value: string * primitiveType: PrimitiveType
└── Any  (permissive schema — no type constraint)
```

**Design notes**:
- `[<RequireQualifiedAccess>]` on `SchemaNode` to prevent name collisions
- `Const.value` is `string` (not `obj`) for serializability and equality
- `Any` added for `obj`/`System.Object` fields and bare `option<_>`
- `OneOf` and `Map` exist in the IR for forward-compatibility even if the
  extraction-scope analyzer only produces `AnyOf`

### ObjectSchema

```
ObjectSchema
├── Properties: PropertySchema list
├── Required: string list
├── AdditionalProperties: bool
├── TypeId: string option
├── Description: string option
└── Title: string option
```

### PropertySchema

```
PropertySchema
├── Name: string         (after naming policy applied)
├── Schema: SchemaNode
└── Description: string option
```

### Discriminator

```
Discriminator
├── PropertyName: string
└── Mapping: Map<string, string>   (value → typeId)
```

### PrimitiveType

```
PrimitiveType
├── String
├── Integer
├── Number
└── Boolean
```

### SchemaDocument

The top-level result of analyzing a type.

```
SchemaDocument
├── Root: SchemaNode
└── Definitions: Map<string, SchemaNode>   (typeId → schema)
```

### SchemaGeneratorConfig

Configuration controlling analyzer behavior.

```
SchemaGeneratorConfig
├── UnionEncoding: UnionEncodingStyle        (default: InternalTag)
├── DiscriminatorPropertyName: string        (default: "kind")
├── PropertyNamingPolicy: string -> string   (default: camelCase)
├── AdditionalPropertiesDefault: bool        (default: true)
├── TypeIdResolver: System.Type -> string    (default: short type name)
├── OptionStyle: OptionSchemaStyle           (default: Nullable) [NEW FEATURE]
├── UnwrapSingleCaseDU: bool                 (default: false)    [NEW FEATURE]
├── RecordFieldsRequired: bool               (default: true)     [NEW FEATURE]
└── UnwrapFieldlessTags: bool                (default: true)
```

Fields marked `[NEW FEATURE]` exist in the config type from the start but are
not exercised by the extraction-scope analyzer beyond their default values.

### UnionEncodingStyle

```
UnionEncodingStyle
├── InternalTag     (extraction scope — the only encoding currently implemented)
├── AdjacentTag     [NEW FEATURE]
├── ExternalTag     [NEW FEATURE]
└── Untagged        [NEW FEATURE]
```

### OptionSchemaStyle

```
OptionSchemaStyle
├── Nullable        (extraction scope — current behavior)
└── OmitWhenNone    [NEW FEATURE]
```

## Type Analysis Mappings

### Extraction Scope — What the Analyzer Produces Today

| F# Type | SchemaNode Output |
|----------|------------------|
| Record `{ A: int; B: string }` | `Object({ Properties = [A: Primitive(Integer, "int32"); B: Primitive(String, None)]; Required = ["a"; "b"]; ... })` |
| Struct record | Same as record |
| Multi-case DU (with fields) | `AnyOf([case1_object; case2_object; ...])` where each case is an Object with Const discriminator |
| Fieldless DU (all cases empty) | `Enum(["Case1"; "Case2"; ...], String)` |
| F# enum (int-backed) | `Enum(["Value1"; "Value2"; ...], Integer)` |
| `option<T>` / `voption<T>` | `Nullable(inner_schema)` |
| `option<_>` (bare generic) | `Any` |
| `T list`, `T array`, `T seq` | `Array(inner_schema)` |
| `ResizeArray<T>` | `Array(inner_schema)` |
| Recursive type (cycle) | `Ref(typeId)` + entry in Definitions |
| Primitive (string) | `Primitive(String, None)` |
| Primitive (int) | `Primitive(Integer, Some "int32")` |
| Primitive (int64) | `Primitive(Integer, Some "int64")` |
| Primitive (float/double) | `Primitive(Number, Some "double")` |
| Primitive (decimal) | `Primitive(Number, Some "decimal")` |
| Primitive (bool) | `Primitive(Boolean, None)` |
| .NET class | `Object(...)` (NJsonSchema handles property discovery) |
| `Nullable<T>` | `Nullable(inner_schema)` |
| `obj` / `System.Object` | `Any` |

### New Feature Scope — Future Additions

| F# Type | SchemaNode Output | Requirement |
|----------|------------------|-------------|
| Anonymous record | `Object(...)` | FR-022 |
| `Map<string, T>` | `Map(value_schema)` | FR-023 |
| `Map<K, T>` (non-string key) | `Array(Object with Key + Value)` | FR-023 |
| `Set<T>` | `Array(inner_schema)` | FR-024 |
| `DateTime` | `Primitive(String, Some "date-time")` | FR-025 |
| `DateOnly` | `Primitive(String, Some "date")` | FR-025 |
| `TimeOnly` / `TimeSpan` | `Primitive(String, Some "time")` | FR-025 |
| `Guid` | `Primitive(String, Some "uuid")` | FR-025 |
| `Uri` | `Primitive(String, Some "uri")` | FR-025 |
| `byte[]` | `Primitive(String, Some "byte")` | FR-025 |

## NJsonSchema Translation Mapping

| SchemaNode | NJsonSchema Construction |
|------------|-------------------------|
| `Object` | `JsonSchema(Type = Object)` + Properties + RequiredProperties |
| `Array` | `JsonSchema(Type = Array, Item = translated_items)` |
| `AnyOf` | `JsonSchema()` + AnyOf collection with references |
| `OneOf` | `JsonSchema()` + OneOf collection + Discriminator |
| `Nullable` | Inner schema with `Type \|\|= JsonObjectType.Null` |
| `Primitive` | `JsonSchema(Type = mapped_type, Format = format)` |
| `Enum(_, String)` | `JsonSchema(Type = String)` + Enumeration values |
| `Enum(_, Integer)` | `JsonSchema(Type = Integer)` + Enumeration values |
| `Ref` | `JsonSchema(Reference = definitions[typeId])` |
| `Map` | `JsonSchema(Type = Object, AdditionalPropertiesSchema = value)` |
| `Const` | `JsonSchema(Type = String, Default = value)` + single Enumeration |
| `Any` | `JsonSchema()` (no type set — permits anything) |

## OpenApi Translation Mapping

| SchemaNode | OpenApiSchema Construction (net9.0 / net10.0) |
|------------|-----------------------------------------------|
| `Object` | `Type = "object"` + Properties dict + Required set |
| `Array` | `Type = "array"` + Items |
| `AnyOf` | AnyOf list populated |
| `OneOf` | OneOf list + OpenApiDiscriminator |
| `Nullable` | net9.0: `Nullable = true` / net10.0: `Type \|= Null` |
| `Primitive` | `Type = mapped` + Format |
| `Enum` | Enum list with string/JsonNode values |
| `Ref` | net9.0: `OpenApiReference` / net10.0: `OpenApiSchemaReference` |
| `Map` | `Type = "object"` + AdditionalProperties |
| `Const` | Single-value Enum |
| `Any` | Empty schema (no type constraint) |
