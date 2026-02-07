# API Surface Contracts: Extended Type Support

**Feature**: 002-extended-types | **Date**: 2026-02-06

This feature is a library (not a service), so contracts describe the public API surface changes rather than HTTP endpoints.

## New Public Types

### FSharp.Data.JsonSchema.Core

```fsharp
/// New attribute for custom format annotations.
[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field, AllowMultiple = false)>]
type JsonSchemaFormatAttribute(format: string) =
    inherit Attribute()
    member _.Format : string
```

No other new public types are introduced.

## Modified Behavior (Internal, No API Surface Change)

### SchemaAnalyzer.analyze

**Before**: Ignores `config.UnionEncoding`, hardcodes InternalTag for all DUs. Does not recognize Choice or anonymous record types.

**After**:
1. Reads `config.UnionEncoding` as global default
2. Detects `[<JsonFSharpConverter(UnionEncoding = ...)>]` per-type attributes (overrides config)
3. Recognizes `Choice<'a,'b>` through `Choice<'a,...,'g>` → produces `SchemaNode.AnyOf`
4. Recognizes anonymous records → produces inline `SchemaNode.Object` (no `$ref`)
5. Reads `[<JsonSchemaFormat>]` attributes on properties → overrides built-in format inference

### SchemaGeneratorConfig

No changes to the type definition. The existing `UnionEncoding` field will now be read and respected.

## Backwards Compatibility Contract

| Scenario | Guarantee |
|----------|-----------|
| Existing InternalTag DUs (default config) | Byte-identical schema output |
| Existing records | No change |
| Existing Map/Set/Dictionary | No change |
| Existing built-in format inference | No change (attribute overrides only when present) |
| Existing snapshot tests (141) | All pass unchanged |

## Version Impact

- **Package version**: MINOR bump (new additive capabilities, no breaking changes)
- **FSharp.Data.JsonSchema.Core**: 1.0.0 → 1.1.0
- **FSharp.Data.JsonSchema**: 3.0.0 → 3.1.0
- **FSharp.Data.JsonSchema.OpenApi**: 1.0.0 → 1.1.0
