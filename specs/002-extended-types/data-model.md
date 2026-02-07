# Data Model: Extended Type Support

**Feature**: 002-extended-types | **Date**: 2026-02-06

## Existing IR (No Changes Required)

The current `SchemaNode` discriminated union is expressive enough for all 4 encoding styles, anonymous records, and Choice types. No new variants are needed.

```fsharp
type SchemaNode =
    | Object of ObjectSchema      // Used for: records, anonymous records, DU cases (InternalTag, AdjacentTag, ExternalTag)
    | Array of items: SchemaNode  // (no changes)
    | AnyOf of schemas: SchemaNode list  // Used for: Choice types, Untagged DUs, multi-case DUs
    | OneOf of schemas: SchemaNode list * discriminator: Discriminator option  // (no changes)
    | Nullable of inner: SchemaNode  // (no changes)
    | Primitive of primitiveType: PrimitiveType * format: string option  // format carries custom annotations
    | Enum of values: string list * underlyingType: PrimitiveType  // (no changes)
    | Ref of typeId: string  // (no changes)
    | Map of valueSchema: SchemaNode  // (no changes)
    | Const of value: string * primitiveType: PrimitiveType  // Used for: DU tag values
    | Any  // (no changes)
```

## Existing Config (No Changes Required)

```fsharp
type SchemaGeneratorConfig = {
    UnionEncoding: UnionEncodingStyle       // EXISTING but currently unused — will be read by analyzer
    DiscriminatorPropertyName: string       // Used for InternalTag (default: "kind")
    PropertyNamingPolicy: string -> string  // Applied to all field names
    AdditionalPropertiesDefault: bool       // Applied to anonymous records too
    TypeIdResolver: Type -> string          // (no changes)
    OptionStyle: OptionSchemaStyle          // (no changes)
    UnwrapSingleCaseDU: bool               // (no changes)
    RecordFieldsRequired: bool             // Applied to anonymous records too
    UnwrapFieldlessTags: bool              // (no changes)
}
```

## New Types

### JsonSchemaFormatAttribute (new, in Core)

```fsharp
namespace FSharp.Data.JsonSchema.Core

open System

/// Specifies a custom JSON Schema format string for the annotated property or type.
/// When present, overrides built-in format inference (e.g., DateTime → "date-time").
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field, AllowMultiple = false)>]
type JsonSchemaFormatAttribute(format: string) =
    inherit Attribute()
    member _.Format = format
```

**Relationships**:
- Read by `SchemaAnalyzer.analyzeType` during field/property analysis
- Produces `SchemaNode.Primitive(type, Some format)` overriding built-in inference

## Schema Shape by Encoding Style

### InternalTag (existing — no change)

For a DU case `Case1 of name: string * value: int`:

```
SchemaNode.Object {
    Properties = [
        { Name = "kind"; Schema = Const("Case1", String) }  // discriminator inside
        { Name = "name"; Schema = Primitive(String, None) }
        { Name = "value"; Schema = Primitive(Integer, None) }
    ]
    Required = ["kind"; "name"; "value"]
    TypeId = Some "Case1"
}
```

### AdjacentTag (new)

Same case produces:

```
SchemaNode.Object {
    Properties = [
        { Name = "Case"; Schema = Const("Case1", String) }  // tag property
        { Name = "Fields"; Schema = Object {                 // fields property
            Properties = [
                { Name = "name"; Schema = Primitive(String, None) }
                { Name = "value"; Schema = Primitive(Integer, None) }
            ]
            Required = ["name"; "value"]
            TypeId = None
        }}
    ]
    Required = ["Case"; "Fields"]
    TypeId = Some "Case1"
}
```

For a fieldless case `Case2`:

```
SchemaNode.Object {
    Properties = [
        { Name = "Case"; Schema = Const("Case2", String) }
    ]
    Required = ["Case"]
    TypeId = Some "Case2"
}
```

### ExternalTag (new)

Same case produces:

```
SchemaNode.Object {
    Properties = [
        { Name = "Case1"; Schema = Object {                  // case name is the key
            Properties = [
                { Name = "name"; Schema = Primitive(String, None) }
                { Name = "value"; Schema = Primitive(Integer, None) }
            ]
            Required = ["name"; "value"]
            TypeId = None
        }}
    ]
    Required = ["Case1"]
    TypeId = Some "Case1"
}
```

For a fieldless case `Case2`:

```
SchemaNode.Object {
    Properties = [
        { Name = "Case2"; Schema = Object {
            Properties = []
            Required = []
            TypeId = None
        }}
    ]
    Required = ["Case2"]
    TypeId = Some "Case2"
}
```

### Untagged (new)

Same case produces (no discriminator, just fields):

```
SchemaNode.Object {
    Properties = [
        { Name = "name"; Schema = Primitive(String, None) }
        { Name = "value"; Schema = Primitive(Integer, None) }
    ]
    Required = ["name"; "value"]
    TypeId = Some "Case1"
}
```

For a fieldless case: `SchemaNode.Const("Case2", PrimitiveType.String)` (same as current behavior when unwrapping fieldless tags).

### Multi-case DU assembly

All encoding styles combine cases the same way:
- Root DU: register each case in definitions, produce `SchemaNode.AnyOf` of `SchemaNode.Ref` entries
- Non-root DU: inline case schemas in `SchemaNode.AnyOf`

Exception: Untagged DUs do not use discriminator in `OneOf` — they use plain `AnyOf`.

## Choice Type Schema Shape

For `Choice<string, int>`:

```
SchemaNode.AnyOf [
    SchemaNode.Primitive(String, None)
    SchemaNode.Primitive(Integer, None)
]
```

For `Choice<string, ComplexRecord>`:

```
SchemaNode.AnyOf [
    SchemaNode.Primitive(String, None)
    SchemaNode.Ref "ComplexRecord"
]
```

(ComplexRecord added to definitions as usual.)

## Anonymous Record Schema Shape

For `{| Name: string; Age: int |}`:

```
SchemaNode.Object {
    Properties = [
        { Name = "name"; Schema = Primitive(String, None) }  // naming policy applied
        { Name = "age"; Schema = Primitive(Integer, None) }
    ]
    Required = ["name"; "age"]
    AdditionalProperties = false       // from config
    TypeId = None                      // anonymous records have no stable type identity
    Description = None
    Title = None                       // no meaningful name
}
```

Key difference from named records: `TypeId = None` and `Title = None`, so anonymous records are always inlined (never produce `$ref` definitions).
