# Research: Recursive Type Schema Generation

**Feature**: 003-recursive-types
**Date**: 2026-02-06

## Decision 1: Existing Recursion Detection Is Already Functional

**Decision**: The existing `visiting` HashSet / `analyzed` Dictionary mechanism in SchemaAnalyzer correctly handles self-recursive types. This feature is primarily about adding test coverage and documenting existing behavior, not implementing new recursion detection.

**Rationale**: Code analysis of `SchemaAnalyzer.fs` shows:
- `getOrAnalyzeRef` (lines 142-155) checks `visiting.Contains ty` before recursing
- When a type is currently being visited, it returns `"#"` for root self-reference or `typeId` for non-root back-reference
- The `analyzeMultiCaseDU` function (lines 695-714) correctly handles root DU case expansion with definitions
- `analyzeDuCaseFieldSchema` (lines 312-337) routes non-primitive field types through `getOrAnalyzeRef`

**Trace for `type Node = | Leaf of int | Node of Node * Node`**:
1. `visiting.Add(Node)` at entry
2. `analyzeMultiCaseDU` detects `isRoot = true`
3. For `Leaf of int` case: `int` is primitive, inlined directly
4. For `Node of Node * Node` case: each `Node` field hits `analyzeDuCaseFieldSchema` → `getOrAnalyzeRef(Node)` → `visiting.Contains(Node)=true` → returns `"#"` → produces `Ref "#"`
5. Result: Root is `AnyOf [Ref "Leaf"; Ref "Node"]` with definitions for both cases

**Alternatives considered**:
- Reimplementing recursion detection: Unnecessary, existing mechanism is correct
- Adding depth limits: Not needed for schema generation (only produces references, not infinite expansion)

## Decision 2: Test Coverage Strategy

**Decision**: Add comprehensive test coverage across all three test projects for self-recursive types, recursion through collections, and recursion through options.

**Rationale**: Current test coverage only includes:
- Mutually recursive DUs (Chicken/Egg) in Core and NJsonSchema tests
- Mutually recursive DUs with options (Even/Odd) in Core and NJsonSchema tests
- No self-recursive DU tests (the exact pattern from issue #15)
- No recursive record tests
- No recursion-through-collection tests
- No OpenApi tests for any recursive patterns
- No validation tests for recursive types

**Test types to add**:
1. `type TreeNode = | Leaf of int | Branch of TreeNode * TreeNode` (self-recursive DU, issue #15 pattern)
2. `type LinkedNode = { Value: int; Next: LinkedNode option }` (self-recursive record)
3. `type TreeRecord = { Value: string; Children: TreeRecord list }` (recursion through collection)
4. `type Expression = | Literal of int | Add of Expression * Expression | Negate of Expression` (multi-case self-recursive DU)

## Decision 3: Reference Format Across Translators

**Decision**: Self-references use `Ref "#"` in the Core IR, which translates to:
- NJsonSchema: `Reference = rootSchema` (object identity)
- OpenApi NET9: `schema.Reference = OpenApiReference(Id = rootTitle)`
- OpenApi NET10: `s.AnyOf.Add(OpenApiSchemaReference(rootTitle, null))`

**Rationale**: All three translators already handle `Ref "#"` correctly. The NJsonSchema translator produces `$ref: "#"` in JSON output. OpenApi translators resolve "#" to the root schema's title for component reference naming.

## Decision 4: Versioning Impact

**Decision**: This is a PATCH bump (bug fix / test coverage addition), not a MINOR or MAJOR bump.

**Rationale**: Per constitution principle VI (Semantic Versioning Discipline):
- No changes to generated schema output for existing types (no MAJOR bump needed)
- No new public API surface (no MINOR bump needed)
- If any bugs are found and fixed during testing, those are PATCH-level fixes
- Adding test coverage and documentation is a PATCH activity

## Decision 5: No Code Changes Expected (Validation-First Approach)

**Decision**: Write tests first. If all tests pass without code changes, the feature is "verify existing behavior works for issue #15 patterns." If any tests fail, fix the bugs as part of this feature.

**Rationale**: The code analysis strongly suggests the recursion mechanism already works for self-recursive types. The test-first approach:
1. Proves the behavior works (or identifies specific failures)
2. Prevents regression in future changes
3. Satisfies constitution principle IV (Schema Stability via Snapshot Testing)
4. Closes GitHub issue #15 with concrete evidence
