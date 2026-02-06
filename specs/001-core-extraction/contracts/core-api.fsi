// FSharp.Data.JsonSchema.Core public API surface
// This is a design contract — not a compilable signature file.

namespace FSharp.Data.JsonSchema.Core

// ── IR Types ──

[<RequireQualifiedAccess>]
type PrimitiveType =
    | String
    | Integer
    | Number
    | Boolean

[<RequireQualifiedAccess>]
type UnionEncodingStyle =
    | InternalTag
    | AdjacentTag
    | ExternalTag
    | Untagged

[<RequireQualifiedAccess>]
type OptionSchemaStyle =
    | Nullable
    | OmitWhenNone

type PropertySchema = {
    Name: string
    Schema: SchemaNode
    Description: string option
}

and ObjectSchema = {
    Properties: PropertySchema list
    Required: string list
    AdditionalProperties: bool
    TypeId: string option
    Description: string option
    Title: string option
}

and Discriminator = {
    PropertyName: string
    Mapping: Map<string, string>
}

and [<RequireQualifiedAccess>] SchemaNode =
    | Object of ObjectSchema
    | Array of items: SchemaNode
    | AnyOf of schemas: SchemaNode list
    | OneOf of schemas: SchemaNode list * discriminator: Discriminator option
    | Nullable of inner: SchemaNode
    | Primitive of primitiveType: PrimitiveType * format: string option
    | Enum of values: string list * underlyingType: PrimitiveType
    | Ref of typeId: string
    | Map of valueSchema: SchemaNode
    | Const of value: string * primitiveType: PrimitiveType
    | Any

type SchemaDocument = {
    Root: SchemaNode
    Definitions: Map<string, SchemaNode>
}

// ── Configuration ──

type SchemaGeneratorConfig = {
    UnionEncoding: UnionEncodingStyle
    DiscriminatorPropertyName: string
    PropertyNamingPolicy: string -> string
    AdditionalPropertiesDefault: bool
    TypeIdResolver: System.Type -> string
    OptionStyle: OptionSchemaStyle
    UnwrapSingleCaseDU: bool
    RecordFieldsRequired: bool
    UnwrapFieldlessTags: bool
}

module SchemaGeneratorConfig =
    /// Default configuration matching current library behavior:
    /// InternalTag encoding, "kind" discriminator, camelCase naming,
    /// additionalProperties true, nullable options, no single-case unwrap,
    /// record fields required, fieldless tags unwrapped.
    val defaults: SchemaGeneratorConfig

// ── Analyzer ──

module SchemaAnalyzer =
    /// Analyze an F# type and produce a SchemaDocument.
    val analyze: config: SchemaGeneratorConfig -> targetType: System.Type -> SchemaDocument
