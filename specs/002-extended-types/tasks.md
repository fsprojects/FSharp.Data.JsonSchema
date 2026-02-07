# Tasks: Extended Type Support

**Input**: Design documents from `/specs/002-extended-types/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Snapshot tests are included per constitution principle IV (Schema Stability via Snapshot Testing). All new type mappings MUST include corresponding snapshot tests.

**Organization**: Tasks are grouped by user story. The quickstart.md implementation order (Choice → Anonymous Records → DU Encoding → Format Annotations) is reflected in phase ordering to minimize risk.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup

**Purpose**: Verify baseline and create shared test infrastructure

- [ ] T001 Run full test suite to verify all 141 existing tests pass as baseline: `dotnet test`
- [ ] T002 [P] Add Choice type test types (RecWithChoice2, RecWithChoice3, RecWithChoiceComplex, RecWithNestedChoice) in `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs`
- [ ] T003 [P] Add anonymous record test types (RecWithAnonRecord, RecWithNestedAnonRecord, RecWithOptionalAnonField, RecWithAnonInCollection) in `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs`
- [ ] T004 [P] Add DU encoding test types (TestDUForEncoding with fieldless, single-field, and multi-field cases) in `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs`
- [ ] T005 [P] Add format annotation test types (RecWithCustomFormat, RecWithFormatOverride) in `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs` — requires T029 (attribute definition) first, so defer to Phase 5

**Checkpoint**: Baseline verified, test types ready for US1 and US2

---

## Phase 2: User Story 2 - Choice Type Support (Priority: P1) 🎯 MVP

**Goal**: Recognize `Choice<'a,'b>` through `Choice<'a,...,'g>` types and produce `SchemaNode.AnyOf` of constituent types, resolving GitHub issue #22

**Independent Test**: Define `Choice<string, int>` property, verify schema uses `anyOf` listing string and integer type alternatives

### Implementation for User Story 2

- [ ] T006 [US2] Add `isChoiceType` helper function in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` that checks if a type's generic type definition matches `typedefof<Choice<_,_>>` through `typedefof<Choice<_,_,_,_,_,_,_>>`
- [ ] T007 [US2] Add `analyzeChoiceType` function in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` that extracts generic type arguments via `ty.GetGenericArguments()`, analyzes each, and produces `SchemaNode.AnyOf` of the results
- [ ] T008 [US2] Insert Choice type check before `FSharpType.IsUnion` in the `analyzeType` dispatch chain (~line 189) in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`
- [ ] T009 [US2] Add Core snapshot tests for Choice types in `test/FSharp.Data.JsonSchema.Core.Tests/SchemaAnalyzerTests.fs`: Choice<string,int>, Choice<string,int,bool>, Choice<string,ComplexRecord>, nested Choice<int, Choice<string,bool>>
- [ ] T010 [US2] Add NJsonSchema snapshot tests for Choice types in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` with test types in `test/FSharp.Data.JsonSchema.Tests/TestTypes.fs`
- [ ] T011 [US2] Run full test suite to verify all 141 existing tests still pass plus new Choice tests: `dotnet test`

**Checkpoint**: Choice types produce `anyOf` schemas. GitHub issue #22 resolved. All existing tests pass.

---

## Phase 3: User Story 1 - Anonymous Record Support (Priority: P1)

**Goal**: Recognize F# anonymous record types and produce inline `SchemaNode.Object` with `TypeId = None` and `Title = None`

**Independent Test**: Define a record with `{| field1: string; field2: int |}` property, verify schema includes object with all fields properly typed

### Implementation for User Story 1

- [ ] T012 [US1] Add anonymous record detection in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`: add `isAnonymousRecord` helper using `FSharpType.IsRecord` with binding flags or compiler-generated name detection, inserted before the existing `FSharpType.IsRecord` check in `analyzeType`
- [ ] T013 [US1] Add `analyzeAnonymousRecord` function in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` that reuses record field analysis logic but produces `SchemaNode.Object` with `TypeId = None`, `Title = None`, respecting config settings (PropertyNamingPolicy, RecordFieldsRequired, AdditionalPropertiesDefault)
- [ ] T014 [US1] Add Core snapshot tests for anonymous records in `test/FSharp.Data.JsonSchema.Core.Tests/SchemaAnalyzerTests.fs`: simple anon record, nested anon record, anon record with optional field, anon record in collection, anon record in complex type
- [ ] T015 [US1] Add NJsonSchema snapshot tests for anonymous records in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` with test types in `test/FSharp.Data.JsonSchema.Tests/TestTypes.fs`
- [ ] T016 [US1] Verify OpenApi translator handles anonymous record IR shapes by running OpenApi tests: `dotnet test test/FSharp.Data.JsonSchema.OpenApi.Tests/` (FR-003: all translators must generate valid schemas)
- [ ] T017 [US1] Run full test suite to verify all existing tests still pass plus new anonymous record tests: `dotnet test`

**Checkpoint**: Anonymous records produce inline object schemas. All translators verified. All existing tests pass.

---

## Phase 4: User Story 3 - DU Encoding Styles (Priority: P1)

**Goal**: Respect `config.UnionEncoding` and per-type `[<JsonFSharpConverter>]` attributes to produce correct schema shapes for InternalTag, AdjacentTag, ExternalTag, and Untagged encoding styles

**Independent Test**: Configure `UnionEncoding = ExternalTag` and verify DU schema uses case-name-as-key wrapping structure

### Implementation for User Story 3

- [ ] T018 [US3] Add `resolveUnionEncoding` helper in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` that checks for `[<JsonFSharpConverter(UnionEncoding = ...)>]` attribute on the DU type, falls back to `config.UnionEncoding`, and maps FSharp.SystemTextJson's `JsonUnionEncoding` flags to Core's `UnionEncodingStyle`
- [ ] T019 [US3] Modify `analyzeDU` in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` to call `resolveUnionEncoding` and pass the result to `buildCaseSchema` and `analyzeMultiCaseDU`
- [ ] T020 [US3] Add `encodingStyle: UnionEncodingStyle` parameter to `buildCaseSchema` in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` and implement InternalTag path (must produce identical output to current behavior — no functional change for this path)
- [ ] T021 [US3] Run full test suite after InternalTag refactor to verify all 141 existing tests still produce byte-identical output: `dotnet test`
- [ ] T022 [US3] Implement AdjacentTag encoding in `buildCaseSchema` in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`: produce Object with "Case" (Const tag) and "Fields" (nested Object with case fields) adjacent properties; fieldless cases omit "Fields" property
- [ ] T023 [US3] Implement ExternalTag encoding in `buildCaseSchema` in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`: produce Object with single property named after the case containing the fields object; fieldless cases use empty object
- [ ] T024 [US3] Implement Untagged encoding in `buildCaseSchema` in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`: produce Object with only field properties (no discriminator); modify `analyzeMultiCaseDU` to use `SchemaNode.AnyOf` without discriminator for Untagged style
- [ ] T025 [US3] Add Core snapshot tests for all 4 encoding styles in `test/FSharp.Data.JsonSchema.Core.Tests/SchemaAnalyzerTests.fs`: for each style test fieldless case, single-field case, and multi-field case (12 tests minimum)
- [ ] T026 [US3] Add Core snapshot test for per-type attribute override in `test/FSharp.Data.JsonSchema.Core.Tests/SchemaAnalyzerTests.fs`: DU with `[<JsonFSharpConverter(UnionEncoding = ExternalTag)>]` attribute with global config set to InternalTag
- [ ] T027 [US3] Add NJsonSchema snapshot tests for DU encoding styles in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` with test types in `test/FSharp.Data.JsonSchema.Tests/TestTypes.fs`
- [ ] T028 [US3] Run full test suite to verify all existing tests still pass plus new DU encoding tests: `dotnet test`

**Checkpoint**: All 4 DU encoding styles produce correct schemas. Per-type attribute overrides work. All existing tests pass with byte-identical InternalTag output.

---

## Phase 5: User Story 4 - Custom Format Annotations (Priority: P2)

**Goal**: Define `JsonSchemaFormatAttribute` and read it during field analysis to override built-in format inference

**Independent Test**: Annotate a string property with `[<JsonSchemaFormat("email")>]`, verify schema includes `"format": "email"`

### Implementation for User Story 4

- [ ] T029 [US4] Create `JsonSchemaFormatAttribute` in new file `src/FSharp.Data.JsonSchema.Core/JsonSchemaFormatAttribute.fs` with `[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Field)>]`, single `Format: string` property, XML doc comments
- [ ] T030 [US4] Add `JsonSchemaFormatAttribute.fs` to the `<Compile>` list in `src/FSharp.Data.JsonSchema.Core/FSharp.Data.JsonSchema.Core.fsproj` (before SchemaAnalyzer.fs so it's available)
- [ ] T031 [US4] Modify field analysis in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs` to check `PropertyInfo.GetCustomAttribute<JsonSchemaFormatAttribute>()` and when present, use its `Format` value to override the built-in format inference in `SchemaNode.Primitive`
- [ ] T032 [US4] Add format annotation test types in `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs`: RecWithCustomFormat (string property with `[<JsonSchemaFormat("email")>]`), RecWithFormatOverride (DateTime property with `[<JsonSchemaFormat("date")>]` overriding default "date-time")
- [ ] T033 [US4] Add Core snapshot tests for format annotations in `test/FSharp.Data.JsonSchema.Core.Tests/SchemaAnalyzerTests.fs`: custom format on string, format override on DateTime, format with Nullable wrapper
- [ ] T034 [US4] Add NJsonSchema snapshot tests for format annotations in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs`
- [ ] T035 [US4] Run full test suite to verify all existing tests still pass plus new format annotation tests: `dotnet test`

**Checkpoint**: Custom format annotations work via attributes. Built-in format inference is overridden when attribute present. All existing tests pass.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, versioning, and final validation

- [ ] T036 Update README.md with documentation for all 4 new capabilities: Choice type support, anonymous records, DU encoding styles, custom format annotations
- [ ] T037 Update RELEASE_NOTES.md with version bump entries for Core (1.0.0→1.1.0), main (3.0.0→3.1.0), OpenApi (1.0.0→1.1.0)
- [ ] T038 [P] Update version numbers in `Directory.Build.props` or project files for MINOR bump
- [ ] T039 [P] Add XML doc comments to any new public functions or helpers in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`
- [ ] T040 Run full test suite one final time across all TFMs to verify everything passes: `dotnet test`
- [ ] T041 Run quickstart.md validation: verify build commands and test commands work as documented

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (US2 - Choice)**: Depends on T001 baseline verification. Lowest risk, highest confidence.
- **Phase 3 (US1 - Anon Records)**: Depends on T001. Independent of Phase 2. Can run in parallel.
- **Phase 4 (US3 - DU Encoding)**: Depends on T001. Highest risk (modifies existing DU code). Must verify InternalTag regression at T021 before continuing.
- **Phase 5 (US4 - Format Annotations)**: Depends on T001. Independent of Phases 2-4. Can run in parallel.
- **Phase 6 (Polish)**: Depends on all user story phases complete.

### User Story Dependencies

- **US1 (Anonymous Records)**: Independent — no dependency on other stories
- **US2 (Choice Types)**: Independent — no dependency on other stories
- **US3 (DU Encoding)**: Independent — no dependency on other stories. **Highest risk**: modifies existing `buildCaseSchema` function
- **US4 (Format Annotations)**: Independent — no dependency on other stories

### Within Each User Story

- Implementation before tests (tests validate snapshot output)
- Run full test suite after each story to catch regressions
- Commit after each story checkpoint

### Parallel Opportunities

- T002, T003, T004 can all run in parallel (different test type sections)
- US1 and US2 can run in parallel (different code paths in SchemaAnalyzer)
- US4 can run in parallel with US1/US2/US3 (separate file + isolated change)
- T036, T037, T038, T039 can all run in parallel

---

## Parallel Example: Phase 2 + Phase 3

```text
# These can run concurrently since they modify different code paths:
Phase 2 (US2): Choice type detection (new code path before DU dispatch)
Phase 3 (US1): Anonymous record detection (new code path before record dispatch)

# But Phase 4 (US3) should run after Phases 2+3 complete, since it modifies
# the existing DU code path that Phase 2 must not break.
```

---

## Implementation Strategy

### MVP First (User Story 2 - Choice Types)

1. Complete Phase 1: Setup + baseline verification
2. Complete Phase 2: Choice type support (resolves GitHub issue #22)
3. **STOP and VALIDATE**: Run tests, verify issue #22 is resolved
4. Commit + potentially create PR for just Choice types

### Incremental Delivery

1. Phase 1 → Baseline verified
2. Phase 2 (US2 - Choice) → Test → Commit (MVP, resolves issue #22)
3. Phase 3 (US1 - Anon Records) → Test → Commit
4. Phase 4 (US3 - DU Encoding) → Test → Commit (**carefully** — highest risk)
5. Phase 5 (US4 - Format Annotations) → Test → Commit
6. Phase 6 → Polish, version bump, documentation → Final PR

---

## Notes

- Constitution requires snapshot tests for ALL new type mappings (Principle IV)
- InternalTag backwards compatibility is CRITICAL — verify at T021 before any other encoding work
- Anonymous record detection may vary across TFMs — test early on netstandard2.0
- FSharp.SystemTextJson `JsonFSharpConverter` attribute API must match pinned dependency version
- Total estimated new code: ~200 lines analyzer + ~100 lines attribute + ~300 lines tests
