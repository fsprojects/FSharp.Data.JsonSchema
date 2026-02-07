# Tasks: Recursive Type Schema Generation

**Input**: Design documents from `/specs/003-recursive-types/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: This feature is test-first by design (research Decision 5). All tasks are tests or test support, with potential bug fixes if tests reveal issues.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Verify existing tests pass and establish baseline

- [ ] T001 Run full test suite to confirm all 141 existing tests pass as baseline via `dotnet test`

---

## Phase 2: Foundational (Test Types)

**Purpose**: Define all recursive test types needed across user stories. Types must be added to TestTypes.fs files before any tests can be written.

- [ ] T002 [P] Add `TreeNode`, `LinkedNode`, `TreeRecord`, and `Expression` recursive type definitions to `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs` (see data-model.md for exact type definitions)
- [ ] T003 [P] Add `TreeNode`, `LinkedNode`, `TreeRecord`, and `Expression` recursive type definitions to `test/FSharp.Data.JsonSchema.Tests/TestTypes.fs` (same types as Core)

**Checkpoint**: Test types defined, all existing tests still pass

---

## Phase 3: User Story 1 - Self-Recursive Discriminated Union (Priority: P1) 🎯 MVP

**Goal**: Prove that self-recursive DU types like `type TreeNode = | Leaf of int | Branch of TreeNode * TreeNode` (the exact issue #15 pattern) generate correct JSON Schemas with `$ref` self-references.

**Independent Test**: Run Core analyzer tests for TreeNode and verify `Ref "#"` appears in Branch case fields. Run NJsonSchema snapshot test and verify `$ref: "#"` in output. Run validation test with a nested tree instance.

### Tests for User Story 1

- [ ] T004 [P] [US1] Add Core IR tests for `TreeNode` in `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs`: (1) self-recursive DU produces definitions for Leaf and Branch cases, (2) Branch case fields produce `Ref "#"` for both Item1 and Item2, (3) root is `AnyOf` with refs to case definitions
- [ ] T005 [P] [US1] Add NJsonSchema snapshot test for `TreeNode` in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` using `verify "Self-recursive DU generates proper schema" { return generator typeof<TreeNode> |> json }` pattern. Run once to generate snapshot, review and approve the `.verified.txt` file in `test/FSharp.Data.JsonSchema.Tests/generator-verified/`
- [ ] T006 [P] [US1] Add NJsonSchema validation test for `TreeNode` in `test/FSharp.Data.JsonSchema.Tests/ValidationTests.fs`: serialize a nested TreeNode instance (e.g., `Branch(Leaf 1, Branch(Leaf 2, Leaf 3))`) using `Json.Serialize` and validate against generated schema
- [ ] T007 [US1] Add OpenApi integration test for `TreeNode` in `test/FSharp.Data.JsonSchema.OpenApi.Tests/TransformerIntegrationTests.fs`: analyze `TreeNode` with `SchemaAnalyzer.analyze`, translate with `OpenApiSchemaTranslator.translate`, verify root schema has `AnyOf` entries and component schemas are produced. Use conditional compilation for NET9/NET10 differences.

### Bug Fix (if needed)

- [ ] T008 [US1] If any US1 tests fail: diagnose and fix the specific issue in `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`, `src/FSharp.Data.JsonSchema/NJsonSchemaTranslator.fs`, or `src/FSharp.Data.JsonSchema.OpenApi/OpenApiSchemaTranslator.fs`. Re-run tests until all pass.

**Checkpoint**: Self-recursive DU (issue #15 pattern) validated across Core IR, NJsonSchema, and OpenApi. All tests green.

---

## Phase 4: User Story 2 - Self-Recursive Record Type (Priority: P2)

**Goal**: Prove that self-recursive record types like `type LinkedNode = { Value: int; Next: LinkedNode option }` generate correct JSON Schemas with `$ref` self-references through option-wrapped fields.

**Independent Test**: Run Core analyzer tests for LinkedNode and verify `Nullable(Ref "#")` for the Next field. Run NJsonSchema snapshot test and verify `$ref: "#"` in nullable context.

### Tests for User Story 2

- [ ] T009 [P] [US2] Add Core IR tests for `LinkedNode` in `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs`: (1) self-recursive record root is `Object`, (2) `next` property schema is `Nullable(Ref "#")`, (3) `value` property is `Primitive Int`, (4) no definitions needed (self-ref to root)
- [ ] T010 [P] [US2] Add NJsonSchema snapshot test for `LinkedNode` in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` using the `verify` pattern. Run to generate snapshot, review and approve.
- [ ] T011 [P] [US2] Add NJsonSchema validation test for `LinkedNode` in `test/FSharp.Data.JsonSchema.Tests/ValidationTests.fs`: serialize a linked list instance (e.g., `{ Value = 1; Next = Some { Value = 2; Next = None } }`) and validate against generated schema
- [ ] T012 [US2] Add OpenApi integration test for `LinkedNode` in `test/FSharp.Data.JsonSchema.OpenApi.Tests/TransformerIntegrationTests.fs`: verify root schema is object type with properties including nullable self-reference

### Bug Fix (if needed)

- [ ] T013 [US2] If any US2 tests fail: diagnose and fix the specific issue in source files. Re-run tests until all pass.

**Checkpoint**: Self-recursive records validated across all targets. All tests green.

---

## Phase 5: User Story 3 - Deeply Nested Recursive Structures (Priority: P3)

**Goal**: Prove that recursion through collections and multi-case self-recursive DUs work correctly. This covers `TreeRecord` (recursion through list) and `Expression` (multi-case self-recursive DU with varying arity).

**Independent Test**: Run Core analyzer tests for TreeRecord and Expression. Verify `Array(Ref "#")` for collection recursion and multiple `Ref "#"` patterns in Expression cases.

### Tests for User Story 3

- [ ] T014 [P] [US3] Add Core IR tests for `TreeRecord` in `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs`: (1) root is `Object`, (2) `children` property schema is `Array(Ref "#")` (recursion through list), (3) `value` property is `Primitive String`
- [ ] T015 [P] [US3] Add Core IR tests for `Expression` in `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs`: (1) produces definitions for Literal, Add, and Negate cases, (2) Add case has two `Ref "#"` fields, (3) Negate case has one `Ref "#"` field, (4) Literal case has `Primitive Int` field
- [ ] T016 [P] [US3] Add NJsonSchema snapshot test for `TreeRecord` in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` using the `verify` pattern. Run to generate snapshot, review and approve.
- [ ] T017 [P] [US3] Add NJsonSchema snapshot test for `Expression` in `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` using the `verify` pattern. Run to generate snapshot, review and approve.
- [ ] T018 [P] [US3] Add NJsonSchema validation tests in `test/FSharp.Data.JsonSchema.Tests/ValidationTests.fs`: (1) validate a TreeRecord instance with nested children, (2) validate an Expression instance like `Add(Literal 1, Negate(Literal 2))`
- [ ] T019 [US3] Add OpenApi integration tests for `TreeRecord` and `Expression` in `test/FSharp.Data.JsonSchema.OpenApi.Tests/TransformerIntegrationTests.fs`: verify correct schema structure and component references for both types

### Bug Fix (if needed)

- [ ] T020 [US3] If any US3 tests fail: diagnose and fix the specific issue in source files. Re-run tests until all pass.

**Checkpoint**: All recursive type patterns validated. All tests green.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, cleanup, and issue closure

- [ ] T021 Run full test suite via `dotnet test` to confirm all existing tests still pass alongside new tests
- [ ] T022 If any bug fixes were made (T008, T013, T020): update RELEASE_NOTES.md and version in `Directory.Build.props` per constitution principle VI (PATCH bump)
- [ ] T023 Close GitHub issue #15 with comment referencing the new test types and test results as evidence that recursive types are supported

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - baseline validation
- **Foundational (Phase 2)**: Depends on Phase 1 - adds test types needed by all stories
- **User Stories (Phase 3-5)**: All depend on Phase 2 (test types must exist)
  - US1, US2, US3 can proceed in parallel after Phase 2
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 3 (P3)**: Can start after Phase 2 - No dependencies on other stories

### Within Each User Story

- Core IR tests, NJsonSchema snapshot tests, NJsonSchema validation tests, and OpenApi tests marked [P] can run in parallel
- OpenApi integration test depends on Core analysis working (sequential within story if bugs found)
- Bug fix task depends on identifying which tests fail

### Parallel Opportunities

- T002 and T003 (test type definitions in different files) can run in parallel
- Within each user story: T004/T005/T006, T009/T010/T011, T014/T015/T016/T017/T018 can all run in parallel
- User stories 1, 2, and 3 can run in parallel after Phase 2

---

## Parallel Example: User Story 1

```text
# After Phase 2 (test types defined), launch all US1 tests in parallel:
T004: "Core IR tests for TreeNode in AnalyzerTests.fs"
T005: "NJsonSchema snapshot test for TreeNode in GeneratorTests.fs"
T006: "NJsonSchema validation test for TreeNode in ValidationTests.fs"

# Then sequentially:
T007: "OpenApi integration test for TreeNode in TransformerIntegrationTests.fs"
T008: "Bug fix if any tests fail" (only if needed)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Baseline validation
2. Complete Phase 2: Add test types to TestTypes.fs files
3. Complete Phase 3: US1 - Self-recursive DU (TreeNode) tests
4. **STOP and VALIDATE**: If all US1 tests pass, issue #15 is resolved for the core case
5. This alone is sufficient to close GitHub issue #15

### Incremental Delivery

1. Setup + Foundational → Test types defined
2. Add US1 (self-recursive DU) → Resolves issue #15 core case (MVP!)
3. Add US2 (self-recursive record) → Extends coverage to records
4. Add US3 (collections + multi-case DU) → Full recursive type coverage
5. Each story adds confidence without breaking previous stories

### Parallel Strategy

With multiple agents/developers:

1. Complete Setup + Foundational together
2. Once Phase 2 is done:
   - Agent A: User Story 1 (self-recursive DU)
   - Agent B: User Story 2 (self-recursive record)
   - Agent C: User Story 3 (collections + multi-case DU)
3. All stories complete independently; merge and run full suite

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- This feature is validation-first: existing code likely already works; tests prove it
- If no bugs are found, no source code changes are needed (test-only feature)
- Snapshot files (`.verified.txt`) must be reviewed and approved after first test run
- Commit after each user story phase completes with all tests green
