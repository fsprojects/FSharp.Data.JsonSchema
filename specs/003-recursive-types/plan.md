# Implementation Plan: Recursive Type Schema Generation

**Branch**: `003-recursive-types` | **Date**: 2026-02-06 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-recursive-types/spec.md`

## Summary

Resolve GitHub issue #15 by adding comprehensive test coverage for self-recursive F# types in JSON Schema generation. Research confirms the existing `SchemaAnalyzer` recursion detection mechanism (`visiting` HashSet + `analyzed` cache) already handles self-recursive types correctly. This feature is primarily test-first validation: define recursive test types, write tests across Core IR, NJsonSchema, and OpenApi, and fix any bugs discovered. No new public API surface is expected.

## Technical Context

**Language/Version**: F# 8.0+ / .NET SDK 8.0+
**Primary Dependencies**: FSharp.Core, FSharp.SystemTextJson (Core); NJsonSchema (main package); Microsoft.OpenApi (OpenApi package)
**Storage**: N/A (library, no persistence)
**Testing**: Expecto + Verify (snapshot testing), multi-target (net8.0, net9.0, net10.0)
**Target Platform**: netstandard2.0 through net10.0
**Project Type**: Multi-package NuGet library
**Performance Goals**: Schema generation must complete without infinite loops for all recursive type patterns
**Constraints**: No new runtime dependencies; all existing 141 tests must continue passing
**Scale/Scope**: ~4 new test types, ~15-20 new tests across 3 test projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. F#-Idiomatic API Design | PASS | Self-recursive DUs and records are core F# patterns; schema must handle them correctly |
| II. Minimal Dependency Surface | PASS | No new dependencies; changes are test-only |
| III. Broad Framework Compatibility | PASS | Tests run on net8.0, net9.0, net10.0; no target changes |
| IV. Schema Stability via Snapshot Testing | PASS | New snapshot tests will be added for all recursive type patterns |
| V. Simplicity and Focus | PASS | Test coverage for existing functionality; no scope creep |
| VI. Semantic Versioning Discipline | PASS | PATCH bump if bug fixes needed; otherwise test-only (no version change needed) |

**Post-Design Re-check**: All principles remain satisfied. No new abstractions, dependencies, or API surface introduced.

## Project Structure

### Documentation (this feature)

```text
specs/003-recursive-types/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: Research findings
├── data-model.md        # Phase 1: Expected schema structures
├── quickstart.md        # Phase 1: Implementation guide
├── contracts/           # Phase 1: Expected schema contracts
│   └── expected-schemas.md
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── FSharp.Data.JsonSchema.Core/
│   └── SchemaAnalyzer.fs          # May need fixes if tests reveal bugs
├── FSharp.Data.JsonSchema/
│   └── NJsonSchemaTranslator.fs   # May need fixes if tests reveal bugs
└── FSharp.Data.JsonSchema.OpenApi/
    └── OpenApiSchemaTranslator.fs # May need fixes if tests reveal bugs

test/
├── FSharp.Data.JsonSchema.Core.Tests/
│   ├── TestTypes.fs               # Add recursive test types
│   └── AnalyzerTests.fs           # Add Core IR recursion tests
├── FSharp.Data.JsonSchema.Tests/
│   ├── TestTypes.fs               # Add recursive test types
│   ├── GeneratorTests.fs          # Add NJsonSchema snapshot tests
│   ├── ValidationTests.fs         # Add recursive validation tests
│   └── generator-verified/        # New snapshot files (auto-generated)
└── FSharp.Data.JsonSchema.OpenApi.Tests/
    ├── TransformerIntegrationTests.fs  # Add OpenApi recursion tests
    └── (TestTypes.fs if needed)
```

**Structure Decision**: Existing multi-package library structure is used unchanged. All changes are in test projects, with potential minor fixes in source projects if bugs are discovered.

## Complexity Tracking

No constitution violations. No complexity tracking needed.
