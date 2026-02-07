# Implementation Plan: Core Extraction and Multi-Target Architecture

**Branch**: `001-core-extraction` | **Date**: 2026-02-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-core-extraction/spec.md`

## Summary

Refactor FSharp.Data.JsonSchema into a three-package architecture: a Core
library defining a target-agnostic SchemaNode IR and type analyzer, the existing
NJsonSchema package rebuilt as a thin translator over Core, and a new OpenApi
package providing an ASP.NET Core `IOpenApiSchemaTransformer`. The extraction
must preserve 100% backward compatibility with existing consumers. New type
support and configuration options are separated as deferrable features.

## Technical Context

**Language/Version**: F# 8.0+ / .NET SDK 8.0+
**Primary Dependencies**:
- Core: FSharp.Core (>= 8.0.0), FSharp.SystemTextJson (1.1.23)
- NJsonSchema package: FSharp.Data.JsonSchema.Core, NJsonSchema (10.*), FSharp.SystemTextJson (1.1.23)
- OpenApi package: FSharp.Data.JsonSchema.Core, Microsoft.OpenApi (1.6.x for net9.0 / 2.0.x for net10.0), Microsoft.AspNetCore.OpenApi (9.0.x / 10.0.x)
**Storage**: N/A (library, no persistence)
**Testing**: Expecto (9.0.2), Verify.Expecto (20.3.2) for snapshot tests, dotnet test
**Target Platform**: .NET multi-target (netstandard2.0 through net10.0)
**Project Type**: Multi-project NuGet library
**Performance Goals**: Schema generation performance must not regress; analyzer
adds one IR allocation pass but avoids reflection re-traversal in translator
**Constraints**: Core depends only on FSharp.Core and FSharp.SystemTextJson; existing
snapshot tests must pass byte-identical; OpenApi net9.0 uses Microsoft.OpenApi
1.6.x (OpenAPI 3.0), net10.0 uses Microsoft.OpenApi 2.0.x (OpenAPI 3.1) with
breaking API differences (Nullable property removed, JsonNode replaces OpenApiAny)
**Scale/Scope**: ~420 LOC source (2 files), ~250 LOC tests (6 files), 22 snapshot
files; extraction creates ~3 new source projects + 2 test projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. F#-Idiomatic API Design | PASS | IR uses F# DU for schema nodes; analyzer preserves all current F# type semantics |
| II. Minimal Dependency Surface | PASS | Core adds FSharp.SystemTextJson (required for `Skippable<'T>` detection). Existing package gains Core as transitive dep (unavoidable). OpenApi adds Microsoft.OpenApi + Microsoft.AspNetCore.OpenApi (both required for its purpose). Constitution Principle II amended (v1.1.0) to support multi-package architecture with per-package dependency rules. |
| III. Broad Framework Compatibility | PASS | Core and existing package add net9.0/net10.0 to existing TFMs. OpenApi targets net9.0/net10.0 (constrained by ASP.NET Core). No TFMs removed. |
| IV. Schema Stability via Snapshot Testing | PASS | All existing snapshot tests run unchanged against refactored NJsonSchema translator. Core gets its own IR-level test suite. OpenApi gets parallel snapshot tests. |
| V. Simplicity and Focus | PASS with justification | Adding two new projects is justified by the core value proposition (multi-target schema output). The IR is a single DU — no abstraction layers, interfaces, or dependency injection. See Complexity Tracking. |
| VI. Semantic Versioning Discipline | PASS | Existing package bumps to 3.0.0 (major: internal restructuring). Core starts at 1.0.0. OpenApi starts at 1.0.0. RELEASE_NOTES.md updated. |

## Project Structure

### Documentation (this feature)

```text
specs/001-core-extraction/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (F# module signatures)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── Directory.Build.props                        # Shared NuGet metadata (existing)
├── FSharp.Data.JsonSchema.Core/
│   ├── FSharp.Data.JsonSchema.Core.fsproj       # NEW: netstandard2.0;...;net10.0
│   ├── SchemaNode.fs                            # IR types (SchemaNode DU, ObjectSchema, etc.)
│   ├── SchemaGeneratorConfig.fs                 # Config types + defaults
│   └── SchemaAnalyzer.fs                        # F# type → SchemaDocument
├── FSharp.Data.JsonSchema/
│   ├── FSharp.Data.JsonSchema.fsproj            # MODIFIED: adds Core dependency
│   ├── Serializer.fs                            # UNCHANGED: FSharp.Data.Json class
│   ├── NJsonSchemaTranslator.fs                 # NEW: SchemaDocument → NJsonSchema
│   └── JsonSchema.fs                            # MODIFIED: Generator delegates to
│                                                #   analyzer + translator; processors
│                                                #   and Validation preserved
└── FSharp.Data.JsonSchema.OpenApi/
    ├── FSharp.Data.JsonSchema.OpenApi.fsproj     # NEW: net9.0;net10.0
    ├── OpenApiSchemaTranslator.fs                # NEW: SchemaDocument → OpenApiSchema
    └── FSharpSchemaTransformer.fs                # NEW: IOpenApiSchemaTransformer impl

test/
├── FSharp.Data.JsonSchema.Core.Tests/
│   ├── FSharp.Data.JsonSchema.Core.Tests.fsproj # NEW: Expecto + Verify
│   ├── TestTypes.fs                             # Shared test type definitions
│   ├── AnalyzerTests.fs                         # IR output verification
│   └── Main.fs
├── FSharp.Data.JsonSchema.Tests/
│   ├── FSharp.Data.JsonSchema.Tests.fsproj      # EXISTING: unchanged test suite
│   ├── TestTypes.fs                             # EXISTING
│   ├── GeneratorTests.fs                        # EXISTING (snapshot tests)
│   ├── JsonSerializationTests.fs                # EXISTING
│   ├── ValidationTests.fs                       # EXISTING
│   ├── Bug10.fs                                 # EXISTING
│   ├── Main.fs                                  # EXISTING
│   └── generator-verified/                      # EXISTING snapshot files
└── FSharp.Data.JsonSchema.OpenApi.Tests/
    ├── FSharp.Data.JsonSchema.OpenApi.Tests.fsproj # NEW
    ├── TranslatorTests.fs                        # OpenApiSchema output verification
    ├── TransformerIntegrationTests.fs            # ASP.NET Core test host
    └── Main.fs
```

**Structure Decision**: Multi-project library under existing `src/` and `test/`
solution folders. Follows the established pattern of the current repo. All three
source projects share `src/Directory.Build.props` for NuGet metadata.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Two new source projects (Core, OpenApi) | Core extraction is the feature's purpose; OpenApi requires ASP.NET Core deps incompatible with Core/existing | A single project cannot target both netstandard2.0 (Core consumers) and net9.0-only (ASP.NET Core OpenApi) without conditional compilation that obscures the API |
| Microsoft.OpenApi + Microsoft.AspNetCore.OpenApi deps in OpenApi package | Required to implement IOpenApiSchemaTransformer and produce OpenApiSchema objects | No alternative exists — these are the only APIs for ASP.NET Core OpenAPI integration |
| Conditional compilation in OpenApi project (net9.0 vs net10.0) | Microsoft.OpenApi 2.0 (net10.0) has breaking changes: Nullable removed, JsonNode replaces OpenApiAny, IOpenApiSchema interface | Cannot target only one version — users on both .NET 9 and .NET 10 need support |
