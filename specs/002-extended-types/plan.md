# Implementation Plan: Extended Type Support

**Branch**: `002-extended-types` | **Date**: 2026-02-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/002-extended-types/spec.md`

## Summary

Add support for 4 missing type/feature categories in the JSON Schema generator: anonymous records, Choice types (resolving GitHub issue #22), all 4 DU encoding styles (InternalTag, AdjacentTag, ExternalTag, Untagged — currently only InternalTag is implemented despite config/IR support for all 4), and custom format annotations via a new attribute. All changes are in SchemaAnalyzer logic with no IR modifications needed. Map, Set, and built-in format inference are already complete and out of scope.

## Technical Context

**Language/Version**: F# 8.0+ / .NET SDK 8.0+
**Primary Dependencies**: FSharp.Core, FSharp.SystemTextJson (Core); NJsonSchema (main package); Microsoft.OpenApi (OpenApi package)
**Storage**: N/A (library, no persistence)
**Testing**: Expecto + Verify (snapshot testing), 141 existing tests across 3 test projects
**Target Platform**: netstandard2.0, netstandard2.1, netcoreapp3.1, net6.0, net8.0, net9.0, net10.0
**Project Type**: Multi-package NuGet library
**Performance Goals**: N/A (compile-time schema generation)
**Constraints**: No new runtime dependencies; backwards-compatible schema output for existing types
**Scale/Scope**: ~430 lines in SchemaAnalyzer.fs; estimated ~200 new lines of analyzer logic + ~100 lines for attribute + ~300 lines of new tests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. F#-Idiomatic API | ✅ Pass | Anonymous records, Choice, DU encodings all map faithfully to JSON Schema |
| II. Minimal Dependencies | ✅ Pass | No new dependencies. FSharp.SystemTextJson already allowed for Core |
| III. Framework Compatibility | ✅ Pass | No new TFM-specific code. Anonymous record reflection via FSharp.Core |
| IV. Snapshot Testing | ✅ Pass | All new features will have Verify snapshot tests |
| V. Simplicity | ✅ Pass | Single new attribute type. Changes focused in SchemaAnalyzer |
| VI. Semantic Versioning | ✅ Pass | MINOR bump — new additive capabilities, no breaking changes |

**Post-Design Re-check**: ✅ All gates still pass. No new dependencies introduced. One new public type (`JsonSchemaFormatAttribute`) is minimal and justified.

## Project Structure

### Documentation (this feature)

```text
specs/002-extended-types/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── api-surface.md   # Public API changes
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── FSharp.Data.JsonSchema.Core/
│   ├── SchemaNode.fs                    # No changes needed (IR sufficient)
│   ├── SchemaGeneratorConfig.fs         # No changes needed (config field exists)
│   ├── SchemaAnalyzer.fs                # PRIMARY CHANGES: Choice, anon records, DU encoding, format attr
│   └── JsonSchemaFormatAttribute.fs     # NEW: Custom format annotation attribute
├── FSharp.Data.JsonSchema/
│   ├── NJsonSchemaTranslator.fs         # May need minor updates for new encoding shapes
│   └── Serializer.fs                    # No changes
└── FSharp.Data.JsonSchema.OpenApi/
    └── OpenApiSchemaTranslator.fs       # No changes expected (translates IR generically)

test/
├── FSharp.Data.JsonSchema.Core.Tests/
│   └── SchemaAnalyzerTests.fs           # New tests for Choice, anon records, DU encodings, format attr
├── FSharp.Data.JsonSchema.Tests/
│   ├── TestTypes.fs                     # New test types
│   └── GeneratorTests.fs                # New snapshot tests
└── FSharp.Data.JsonSchema.OpenApi.Tests/
    └── (may add tests if OpenApi translator needs changes)
```

**Structure Decision**: Existing multi-package structure. All changes within existing projects. One new file (`JsonSchemaFormatAttribute.fs`) in Core.

## Complexity Tracking

No constitution violations. Table intentionally empty.

## Key Implementation Decisions

### 1. Choice types are detected before general DU dispatch
Choice types (`Choice<'a,'b>` through `Choice<'a,...,'g>`) are intercepted before `FSharpType.IsUnion` check in the type dispatch chain. They produce `SchemaNode.AnyOf` of their type arguments directly — no case wrappers, no discriminators.

### 2. Anonymous records produce inline Object schemas
Anonymous records have no stable type identity, so they always produce inline `SchemaNode.Object` with `TypeId = None` and `Title = None`. They are never registered in definitions and never produce `$ref` entries.

### 3. DU encoding resolved per-type with fallback to config
Resolution order: `[<JsonFSharpConverter(UnionEncoding = ...)>]` attribute on the DU type → `config.UnionEncoding` → default (InternalTag). The encoding style is passed as a parameter to `buildCaseSchema`.

### 4. All encoding styles assume NamedFields
Fields within DU cases are always represented as named object properties (matching the existing serializer's `WithUnionNamedFields()` configuration). Positional array representation is out of scope.

### 5. Custom format attribute defined in Core
`JsonSchemaFormatAttribute` lives in Core (no NJsonSchema dependency). It targets `Property | Field` and carries a single `Format: string` value. The SchemaAnalyzer checks for it during field analysis and uses it to override built-in format inference.

## Artifacts

- [research.md](research.md) — Phase 0: all unknowns resolved
- [data-model.md](data-model.md) — Phase 1: schema shapes per encoding style
- [contracts/api-surface.md](contracts/api-surface.md) — Phase 1: public API changes
- [quickstart.md](quickstart.md) — Phase 1: implementation order and commands
