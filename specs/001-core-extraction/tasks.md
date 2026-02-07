# Tasks: Core Extraction and Multi-Target Architecture

**Input**: Design documents from `/specs/001-core-extraction/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story. Tests are included where specified by the feature specification (snapshot tests for US1, Core IR tests for US2, integration tests for US3).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create project structure, add solution references, configure target frameworks.

- [X] T001 Create Core project: `mkdir -p src/FSharp.Data.JsonSchema.Core && dotnet new classlib -lang F# -o src/FSharp.Data.JsonSchema.Core && dotnet sln add` with solution-folder src
- [X] T002 Configure `src/FSharp.Data.JsonSchema.Core/FSharp.Data.JsonSchema.Core.fsproj`: set TargetFrameworks to `netstandard2.0;netstandard2.1;netcoreapp3.1;net6.0;net8.0;net9.0;net10.0`, add PackageReference for FSharp.SystemTextJson (1.1.23), remove other PackageReferences except FSharp.Core
- [X] T003 Create OpenApi project: `mkdir -p src/FSharp.Data.JsonSchema.OpenApi && dotnet new classlib -lang F# -o src/FSharp.Data.JsonSchema.OpenApi && dotnet sln add` with solution-folder src
- [X] T004 Configure `src/FSharp.Data.JsonSchema.OpenApi/FSharp.Data.JsonSchema.OpenApi.fsproj`: set TargetFrameworks to `net9.0;net10.0`, add Core ProjectReference, add version-conditional PackageReferences for Microsoft.OpenApi (1.6.x on net9.0, 2.0.x on net10.0) and Microsoft.AspNetCore.OpenApi (9.0.x on net9.0, 10.0.x on net10.0)
- [X] T005 Add Core ProjectReference to `src/FSharp.Data.JsonSchema/FSharp.Data.JsonSchema.fsproj`
- [X] T006 Create Core test project: `mkdir -p test/FSharp.Data.JsonSchema.Core.Tests && dotnet new classlib -lang F# -o test/FSharp.Data.JsonSchema.Core.Tests && dotnet sln add` with solution-folder test. Add Expecto, Verify.Expecto, and Core ProjectReference.
- [X] T007 Create OpenApi test project: `mkdir -p test/FSharp.Data.JsonSchema.OpenApi.Tests && dotnet new classlib -lang F# -o test/FSharp.Data.JsonSchema.OpenApi.Tests && dotnet sln add` with solution-folder test. TargetFrameworks `net9.0;net10.0`. Add Expecto, Core and OpenApi ProjectReferences, Microsoft.AspNetCore.TestHost.
- [X] T008 Verify `dotnet build -c Release` succeeds for all projects (empty libs compile)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core IR types and config that MUST be complete before ANY user story can be implemented.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T009 [P] Implement IR types in `src/FSharp.Data.JsonSchema.Core/SchemaNode.fs`: `PrimitiveType` DU, `UnionEncodingStyle` DU, `OptionSchemaStyle` DU, `PropertySchema` record, `ObjectSchema` record, `Discriminator` record, `SchemaNode` DU (all 11 variants: Object, Array, AnyOf, OneOf, Nullable, Primitive, Enum, Ref, Map, Const, Any), `SchemaDocument` record. All per `contracts/core-api.fsi` and `data-model.md`.
- [X] T010 [P] Implement config types in `src/FSharp.Data.JsonSchema.Core/SchemaGeneratorConfig.fs`: `SchemaGeneratorConfig` record with all fields (UnionEncoding, DiscriminatorPropertyName, PropertyNamingPolicy, AdditionalPropertiesDefault, TypeIdResolver, OptionStyle, UnwrapSingleCaseDU, RecordFieldsRequired, UnwrapFieldlessTags). Add `SchemaGeneratorConfig.defaults` value with extraction-scope defaults matching current library behavior (InternalTag, "kind", camelCase, true, short-type-name, Nullable, false, true, true).
- [X] T011 Update `src/FSharp.Data.JsonSchema.Core/FSharp.Data.JsonSchema.Core.fsproj` compile order: SchemaNode.fs, SchemaGeneratorConfig.fs, SchemaAnalyzer.fs (add SchemaAnalyzer.fs entry even though file doesn't exist yet — or create empty placeholder)
- [X] T012 Verify `dotnet build src/FSharp.Data.JsonSchema.Core -c Release` succeeds with no warnings

**Checkpoint**: IR types and config compile. User story implementation can now begin.

---

## Phase 3: User Story 1 — Existing NJsonSchema Consumer Upgrades (Priority: P1)

**Goal**: Refactor existing package internals so `Generator.Create` calls `SchemaAnalyzer.analyze >> NJsonSchemaTranslator.translate`. All 22 existing snapshot tests MUST pass byte-identical.

**Independent Test**: `dotnet test test/FSharp.Data.JsonSchema.Tests -c Release --framework net8.0` — all existing tests pass unchanged.

### Implementation for User Story 1

- [X] T013 [US1] Implement `SchemaAnalyzer.analyze` in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`: recursive type traversal with `HashSet<Type>` visited-set for cycle detection. Must handle all "extraction scope" types: records (via `FSharpType.GetRecordFields`), struct records, multi-case DUs with InternalTag encoding (via `FSharpType.GetUnionCases`), fieldless DUs as string enums, F# enums as integer enums, option/voption as Nullable, Skippable as inner type, list/array/seq/ResizeArray as Array, Nullable<T>, recursive types via Ref + Definitions, generic types, nested types, .NET classes (via System.Reflection public instance property discovery), primitives. Annotated records are handled structurally; annotation constraints (`[<Required>]`, `[<MaxLength>]`, `[<Range>]`) remain NJsonSchema-specific and are not represented in the IR. Use `SchemaGeneratorConfig` for naming policy, discriminator name, additionalProperties default, and TypeIdResolver.
- [X] T014 [US1] Implement `NJsonSchemaTranslator.translate` in `src/FSharp.Data.JsonSchema/NJsonSchemaTranslator.fs`: exhaustive pattern match over `SchemaNode` → `NJsonSchema.JsonSchema` per the NJsonSchema Translation Mapping in `data-model.md`. Handle: Object → JsonSchema(Type=Object) with properties + required; Array → Item; AnyOf → AnyOf collection with schema references; OneOf → OneOf collection + discriminator; Nullable → Type ||| JsonObjectType.Null; Primitive → mapped type + format; Enum(String) → String + enumeration; Enum(Integer) → Integer + enumeration; Ref → Reference to definitions; Map → AdditionalPropertiesSchema; Const → single-enum + default; Any → empty schema. Build definitions dict for SchemaDocument.Definitions.
- [X] T015 [US1] Modify `src/FSharp.Data.JsonSchema/JsonSchema.fs`: change `Generator.CreateInternal` to call `SchemaAnalyzer.analyze config >> NJsonSchemaTranslator.translate` instead of using NJsonSchema's `FromType` + processor pipeline. Preserve the existing `ISchemaProcessor` types (OptionSchemaProcessor, SingleCaseDuSchemaProcessor, MultiCaseDuSchemaProcessor, RecordSchemaProcessor) as fully functional public classes (they remain usable by consumers who build custom NJsonSchema generation pipelines, though `Generator.Create`/`CreateMemoized` no longer routes through them). Preserve `Validation` module and all `Generator` public members unchanged. Wire `casePropertyName` parameter through to `SchemaGeneratorConfig.DiscriminatorPropertyName`.
- [X] T016 [US1] Update `src/FSharp.Data.JsonSchema/FSharp.Data.JsonSchema.fsproj` compile order: add NJsonSchemaTranslator.fs before JsonSchema.fs

**CRITICAL CHECKPOINT**: Run existing snapshot tests:
```bash
dotnet test test/FSharp.Data.JsonSchema.Tests -c Release --framework net8.0
```
All 22 snapshot tests MUST pass byte-identical. Serialization and validation tests MUST also pass.

- [X] T017 [US1] Fix any snapshot test failures by adjusting `SchemaAnalyzer` or `NJsonSchemaTranslator` to match exact current output (property ordering, nullable flag placement, enum value formatting, $ref naming, additionalProperties flags)

**Checkpoint**: Existing consumers can upgrade to the refactored package with zero source changes. US1 acceptance scenarios 1-3 satisfied.

---

## Phase 4: User Story 2 — Core Library for Custom Targets (Priority: P2)

**Goal**: Verify that `FSharp.Data.JsonSchema.Core` is independently usable — a consumer can reference only Core, analyze types, and pattern-match over the IR.

**Independent Test**: `dotnet test test/FSharp.Data.JsonSchema.Core.Tests -c Release --framework net8.0` — all Core tests pass.

### Tests for User Story 2

- [X] T018 [P] [US2] Create test types in `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs`: define representative types for all extraction-scope categories (record, struct record, multi-case DU, fieldless DU, enum, option types, list/array/seq, Nullable<T>, recursive types like Chicken/Egg, generic types like PaginatedResult<T>, nested types, empty record, obj fields). Can share or mirror types from existing test project.
- [X] T019 [P] [US2] Create `test/FSharp.Data.JsonSchema.Core.Tests/Main.fs` with Expecto test runner entry point.

### Implementation for User Story 2

- [X] T020 [US2] Implement analyzer tests in `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs`: verify IR output for each extraction-scope type category. Tests should call `SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<T>` and assert the resulting `SchemaDocument` structure. Cover: simple record → Object with correct properties/required; multi-case DU → AnyOf with per-case Objects + Const discriminator; fieldless DU → Enum with string values; enum → Enum with integer type; option → Nullable wrapping; list/array/seq → Array; recursive types → Ref + Definitions entries; generic types → distinct schemas per instantiation; obj field → Any; empty record → Object with empty properties.
- [X] T021 [US2] Verify Core has no transitive NJsonSchema/OpenApi/Namotion dependencies: `dotnet publish src/FSharp.Data.JsonSchema.Core -c Release --framework net8.0` and inspect output directory. Core SHOULD only contain FSharp.Core and FSharp.SystemTextJson (plus their transitive deps). No NJsonSchema, Microsoft.OpenApi, or Namotion.Reflection DLLs.
- [X] T022 [US2] Verify pattern matching exhaustiveness: write a test that exhaustively pattern-matches all 11 `SchemaNode` variants without a wildcard catch-all. This confirms the DU has the expected shape and that `[<RequireQualifiedAccess>]` ensures future variant additions produce compiler incomplete-match warnings in consumer code.

**Checkpoint**: Core library is independently usable. US2 acceptance scenarios 1-4 satisfied.

---

## Phase 5: User Story 4 — Schema Generation Configuration (Priority: P2)

**Goal**: Verify that `SchemaGeneratorConfig` controls analyzer behavior for extraction-scope settings.

**Independent Test**: Run Core tests with non-default config values and verify IR changes.

### Implementation for User Story 4

- [X] T023 [US4] Add config tests in `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs`: test custom discriminator property name (e.g., `"type"` instead of `"kind"`) produces Const fields with the custom name in DU case objects.
- [X] T024 [US4] Add config tests: test custom `PropertyNamingPolicy` (e.g., PascalCase or snake_case) produces correctly-named properties in the IR for records and DU case fields.
- [X] T025 [US4] Add config tests: test `AdditionalPropertiesDefault = false` produces Object schemas with `AdditionalProperties = false`.
- [X] T026 [US4] Add config test in `test/FSharp.Data.JsonSchema.Tests/`: verify that `Generator.Create(casePropertyName = "type")` still works correctly end-to-end with the refactored pipeline and produces the expected NJsonSchema output.

**Checkpoint**: Configuration extraction-scope settings verified. US4 acceptance scenarios 1-3 satisfied.

---

## Phase 6: User Story 3 — ASP.NET Core OpenAPI with F# Types (Priority: P3)

**Goal**: Implement OpenApi translator and `FSharpSchemaTransformer` so F# types produce correct OpenAPI schemas in ASP.NET Core apps.

**Independent Test**: Integration test using ASP.NET Core test host.

### Implementation for User Story 3

- [X] T027 [US3] Implement `OpenApiSchemaTranslator.translate` in `src/FSharp.Data.JsonSchema.OpenApi/OpenApiSchemaTranslator.fs`: exhaustive pattern match over `SchemaNode` → `OpenApiSchema` per the OpenApi Translation Mapping in `data-model.md`. Use `#if NET10_0_OR_GREATER` conditional compilation for: Nullable handling (net9.0: `Nullable = true` / net10.0: Type |= JsonSchemaType.Null), enum values (net9.0: OpenApiString/OpenApiInteger / net10.0: JsonNode), schema references (net9.0: OpenApiReference / net10.0: OpenApiSchemaReference), discriminator mapping value types. Return tuple of (root schema, component schemas map).
- [X] T028 [US3] Implement `FSharpSchemaTransformer` in `src/FSharp.Data.JsonSchema.OpenApi/FSharpSchemaTransformer.fs`: implement `IOpenApiSchemaTransformer`. Detect F# types via `FSharpType.IsUnion` / `FSharpType.IsRecord`. Call `SchemaAnalyzer.analyze >> OpenApiSchemaTranslator.translate`. Mutate provided schema in-place. Register definitions in component schemas. Pass through non-F# types unchanged (return `Task.CompletedTask`). Two constructors: default (uses `SchemaGeneratorConfig.defaults`) and explicit config.
- [X] T029 [US3] Update `src/FSharp.Data.JsonSchema.OpenApi/FSharp.Data.JsonSchema.OpenApi.fsproj` compile order: OpenApiSchemaTranslator.fs, FSharpSchemaTransformer.fs
- [X] T030 [US3] Implement translator unit tests in `test/FSharp.Data.JsonSchema.OpenApi.Tests/TranslatorTests.fs`: verify OpenApiSchema output for each IR node type (Object, Array, AnyOf, OneOf, Nullable, Primitive, Enum, Ref, Map, Const, Any). Test both net9.0 and net10.0 code paths where applicable.
- [X] T031 [US3] Implement integration test in `test/FSharp.Data.JsonSchema.OpenApi.Tests/TransformerIntegrationTests.fs`: create a minimal ASP.NET Core app with test host, register `FSharpSchemaTransformer`, define endpoints using F# record and DU types, request `/openapi/v1.json`, verify: record type has correct properties and required fields; DU type has anyOf with discriminator; shared types appear once in components/schemas with $ref; non-F# types are unchanged.
- [X] T032 [US3] Create `test/FSharp.Data.JsonSchema.OpenApi.Tests/Main.fs` with Expecto test runner entry point.

**Checkpoint**: F# types produce correct OpenAPI schemas. US3 acceptance scenarios 1-4 satisfied.

---

## Phase 7: User Story 5 — Extended Type Support (Priority: P4) [NEW FEATURES — DEFERRABLE]

**Goal**: Extend the analyzer beyond current library behavior. Each task is independently implementable and testable.

**Note**: These tasks MAY be deferred to a follow-up release without blocking the extraction or OpenApi target. Each requires corresponding tests.

- [ ] T033 [P] [US5] FR-022: Add anonymous record handling to `SchemaAnalyzer.analyze` in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`. Detect anonymous records and produce Object nodes. Add test in `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs`.
- [ ] T034 [P] [US5] FR-023: Add `Map<string, 'T>` handling (→ Map node) and `Map<'K, 'T>` non-string key handling (→ Array of key-value Objects) to `SchemaAnalyzer.analyze`. Add tests.
- [ ] T035 [P] [US5] FR-024: Add `Set<'T>` handling (→ Array node) to `SchemaAnalyzer.analyze`. Add test.
- [ ] T036 [P] [US5] FR-025: Add explicit format annotations for DateOnly (`"date"`), TimeOnly/TimeSpan (`"time"`), Guid (`"uuid"`), Uri (`"uri"`), byte[] (`"byte"`) to `SchemaAnalyzer.analyze`. Add tests.
- [ ] T037 [P] [US5] FR-018: Add `AdjacentTag`, `ExternalTag`, and `Untagged` union encoding styles to `SchemaAnalyzer.analyze`. Add tests for each encoding producing the expected IR shape.
- [ ] T038 [P] [US5] FR-019: Implement configurable `RecordFieldsRequired` in `SchemaAnalyzer.analyze` — when `false`, no fields in required list. Add test.
- [ ] T039 [P] [US5] FR-020: Implement configurable `UnwrapSingleCaseDU` in `SchemaAnalyzer.analyze` — when `true`, single-case DUs with fields produce the inner field's schema directly. Add test.
- [ ] T040 [P] [US5] FR-021: Implement configurable `OptionSchemaStyle.OmitWhenNone` in `SchemaAnalyzer.analyze` — option fields not in required list, schema is unwrapped inner type. Add test.

**Checkpoint**: Extended type support verified. US5 acceptance scenarios 1-4 satisfied. FR-018 through FR-025 each have dedicated passing tests.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Packaging, documentation, and final validation.

- [X] T041 Verify `dotnet pack -c Release` produces 3 NuGet packages: FSharp.Data.JsonSchema.Core, FSharp.Data.JsonSchema, FSharp.Data.JsonSchema.OpenApi
- [X] T042 Verify Core .nupkg declares only FSharp.Core and FSharp.SystemTextJson as dependencies — no NJsonSchema, Microsoft.OpenApi, or Namotion.Reflection
- [X] T043 Update RELEASE_NOTES.md: Core 1.0.0, FSharp.Data.JsonSchema 3.0.0, FSharp.Data.JsonSchema.OpenApi 1.0.0
- [X] T044 Run full validation checklist from quickstart.md: `dotnet build -c Release` all projects, existing snapshot tests byte-identical, Core tests pass, OpenApi tests pass, pack produces 3 packages
- [X] T045 Update README.md with new package structure and OpenApi usage example
- [X] T046 [P] Update GitHub Actions CI workflow: add build/test for new Core and OpenApi projects, add net9.0 and net10.0 test targets, update pack step to produce 3 NuGet packages
- [X] T047 [P] Add XML doc comments to all public API members in Core (`SchemaNode`, `ObjectSchema`, `PropertySchema`, `Discriminator`, `PrimitiveType`, `SchemaDocument`, `SchemaGeneratorConfig`, `SchemaAnalyzer`) and OpenApi (`OpenApiSchemaTranslator`, `FSharpSchemaTransformer`). Configure `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in new .fsproj files.
- [X] T048 [P] Configure `.snupkg` symbol packages with Source Link in `src/FSharp.Data.JsonSchema.Core/FSharp.Data.JsonSchema.Core.fsproj` and `src/FSharp.Data.JsonSchema.OpenApi/FSharp.Data.JsonSchema.OpenApi.fsproj` (match existing package's Source Link configuration)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational (Phase 2) — CRITICAL PATH (must pass before US2-US5)
- **US2 (Phase 4)**: Depends on Phase 3 (analyzer implemented in US1, tests verify it)
- **US4 (Phase 5)**: Depends on Phase 4 (config tests build on analyzer tests)
- **US3 (Phase 6)**: Depends on Phase 2 (Core IR types) + Phase 3 (analyzer)
- **US5 (Phase 7)**: Depends on Phase 4 (extends analyzer with new capabilities)
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### Critical Path

```
Phase 1 → Phase 2 → Phase 3 (US1) → Phase 4 (US2) → Phase 5 (US4)
                                   ↘ Phase 6 (US3) → Phase 8
                                     Phase 7 (US5) ↗
```

US3 (Phase 6) can start in parallel with US2/US4 once US1 completes, since it only needs the analyzer (built in Phase 3) and Core IR types (Phase 2).

US5 (Phase 7) tasks are all independent and can run in parallel once the analyzer exists.

### Within Each User Story

- Tests and implementation can be interleaved (write test, implement, verify)
- Core implementation before integration/translator
- Story complete before moving to next priority (except US3 which can parallel US2/US4)

### Parallel Opportunities

- T009 and T010 (IR types and config) can run in parallel
- T018 and T019 (US2 test types and Main.fs) can run in parallel
- All US5 tasks (T033-T040) can run in parallel
- US3 (Phase 6) can run in parallel with US2/US4 (Phases 4-5) after Phase 3 completes

---

## Implementation Strategy

### Recommended: Incremental Delivery

1. Complete Phase 1 + Phase 2 → Foundation ready
2. Complete Phase 3 (US1) → **STOP AND VALIDATE** all existing tests pass
3. Complete Phase 4 (US2) → Core independently usable
4. Complete Phase 5 (US4) → Configuration verified
5. Complete Phase 6 (US3) → OpenApi target functional
6. Phase 7 (US5) → New features (defer if needed for initial release)
7. Complete Phase 8 → Ready for release

### MVP Release Gate

After Phase 5 (US4): Core + NJsonSchema packages ready for release (Core 1.0.0, FSharp.Data.JsonSchema 3.0.0). OpenApi package can ship separately.

---

## Implementation Complete (2026-02-06)

### Completed Phases

- **✅ Phase 1**: Setup (T001-T008)
- **✅ Phase 2**: Foundational IR types (T009-T012)
- **✅ Phase 3**: User Story 1 — NJsonSchema refactoring (T013-T017)
- **✅ Phase 4**: User Story 2 — Core library tests (T018-T022)
- **✅ Phase 5**: User Story 4 — Configuration tests (T023-T026)
- **✅ Phase 6**: User Story 3 — OpenAPI support (T027-T032)
- **⏸️ Phase 7**: User Story 5 — Extended types (T033-T040) — DEFERRED as designed
- **✅ Phase 8**: Polish & packaging (T041-T048)

### Critical Regression Fixes (Beyond Original Scope)

During implementation, discovered and fixed 11 type regressions where SchemaAnalyzer would fall back to class reflection instead of matching NJsonSchema's format annotations:

1. **DateTime** → `string` + `"date-time"` format
2. **DateTimeOffset** → `string` + `"date-time"` format
3. **DateOnly** → `string` + `"date"` format (NET6+)
4. **TimeOnly** → `string` + `"time"` format (NET6+)
5. **Guid** → `string` + `"guid"` format
6. **Uri** → `string` + `"uri"` format
7. **TimeSpan** → `string` + `"duration"` format
8. **byte[]** → `string` + `"byte"` format (base64, not array of integers)
9. **Map<string,T>** → `object` + `additionalProperties` schema
10. **Dictionary<string,T>** → `object` + `additionalProperties` schema
11. **Set<T>** → `array` schema (via enhanced isArrayLike)

Added 10 new snapshot tests for regression coverage.

### Test Results

**Total: 151 tests passing** across net8.0, net9.0, net10.0
- 99 main tests (89 existing + 10 new format tests)
- 35 Core tests
- 17 OpenApi tests

All existing snapshot tests pass byte-identical.

### Deliverables

1. **FSharp.Data.JsonSchema.Core 1.0.0**
   - Target-agnostic SchemaNode IR
   - Only depends on FSharp.Core + FSharp.SystemTextJson
   - Targets netstandard2.0 through net10.0

2. **FSharp.Data.JsonSchema 3.0.0**
   - Refactored to use Core IR + NJsonSchema translator
   - Full backwards compatibility
   - Public API unchanged

3. **FSharp.Data.JsonSchema.OpenApi 1.0.0**
   - IOpenApiSchemaTransformer for ASP.NET Core
   - Conditional compilation for Microsoft.OpenApi 1.6.x (net9.0) / 2.0.x (net10.0)
   - Targets net9.0 and net10.0

4. **Documentation & Infrastructure**
   - Updated README with new package structure
   - Updated RELEASE_NOTES with version entries
   - Updated CI/CD for 3 packages on 3 TFMs
   - XML doc comments on all public APIs
   - Symbol packages with Source Link

### Git Commit

Committed as: `7a9731b` — "feat: implement Core IR extraction and OpenAPI support with regression fixes"
