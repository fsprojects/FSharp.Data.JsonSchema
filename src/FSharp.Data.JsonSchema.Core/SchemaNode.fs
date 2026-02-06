namespace FSharp.Data.JsonSchema.Core

/// Primitive JSON Schema types.
[<RequireQualifiedAccess>]
type PrimitiveType =
    | String
    | Integer
    | Number
    | Boolean

/// Discriminated union encoding styles for JSON serialization.
[<RequireQualifiedAccess>]
type UnionEncodingStyle =
    | InternalTag
    | AdjacentTag
    | ExternalTag
    | Untagged

/// How option fields are represented in the schema.
[<RequireQualifiedAccess>]
type OptionSchemaStyle =
    | Nullable
    | OmitWhenNone

/// A single property within an object schema.
type PropertySchema = {
    /// Property name (after naming policy applied).
    Name: string
    /// Schema for the property value.
    Schema: SchemaNode
    /// Optional description.
    Description: string option
}

/// Schema for a JSON object type.
and ObjectSchema = {
    /// Ordered list of properties.
    Properties: PropertySchema list
    /// Names of required properties.
    Required: string list
    /// Whether additional properties are allowed.
    AdditionalProperties: bool
    /// Type identifier for $ref generation.
    TypeId: string option
    /// Optional description.
    Description: string option
    /// Optional title.
    Title: string option
}

/// Discriminator for oneOf/anyOf schemas.
and Discriminator = {
    /// Name of the discriminator property.
    PropertyName: string
    /// Mapping from discriminator value to type identifier.
    Mapping: Map<string, string>
}

/// A node in the JSON Schema intermediate representation.
and [<RequireQualifiedAccess>] SchemaNode =
    /// An object with named properties.
    | Object of ObjectSchema
    /// An array of items with a single item schema.
    | Array of items: SchemaNode
    /// Any of the given schemas (untagged union).
    | AnyOf of schemas: SchemaNode list
    /// One of the given schemas with optional discriminator.
    | OneOf of schemas: SchemaNode list * discriminator: Discriminator option
    /// Nullable wrapper around an inner schema.
    | Nullable of inner: SchemaNode
    /// A primitive type with optional format.
    | Primitive of primitiveType: PrimitiveType * format: string option
    /// An enumeration of allowed values.
    | Enum of values: string list * underlyingType: PrimitiveType
    /// A reference to a named definition.
    | Ref of typeId: string
    /// A map/dictionary with string keys and typed values.
    | Map of valueSchema: SchemaNode
    /// A constant value (used for discriminator tags).
    | Const of value: string * primitiveType: PrimitiveType
    /// Permissive schema with no type constraint.
    | Any

/// The result of analyzing a type: a root schema plus named definitions.
type SchemaDocument = {
    /// The root schema node.
    Root: SchemaNode
    /// Named schema definitions in insertion order (typeId * schema).
    Definitions: (string * SchemaNode) list
}
