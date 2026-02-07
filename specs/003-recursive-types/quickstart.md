# Quickstart: Recursive Type Schema Generation

**Feature**: 003-recursive-types
**Date**: 2026-02-06

## Overview

This feature adds comprehensive test coverage for recursive type patterns in JSON Schema generation. The underlying recursion detection mechanism already exists in the SchemaAnalyzer; this work validates and documents that behavior with snapshot-verified tests across all three output targets.

## What's Changing

### Test Types to Add

Add these types to the test type files (Core, NJsonSchema, OpenApi):

```fsharp
// Self-recursive DU (Issue #15 pattern)
type TreeNode =
    | Leaf of int
    | Branch of TreeNode * TreeNode

// Self-recursive record
type LinkedNode = { Value: int; Next: LinkedNode option }

// Recursion through collection
type TreeRecord = { Value: string; Children: TreeRecord list }

// Multi-case self-recursive DU
type Expression =
    | Literal of int
    | Add of Expression * Expression
    | Negate of Expression
```

### Tests to Add

**Core Tests (AnalyzerTests.fs)**:
- Self-recursive DU produces definitions with `Ref "#"`
- Self-recursive record produces `Ref "#"` for recursive field
- Recursion through list produces `Array(Ref "#")`
- Multi-case self-recursive DU produces correct definitions

**NJsonSchema Tests (GeneratorTests.fs)**:
- Snapshot tests for each recursive type pattern
- Validation tests for recursive type instances

**OpenApi Tests (TranslatorTests.fs)**:
- Recursive type translation produces correct component references
- Self-reference resolves to root schema title

## Implementation Approach

1. **Test-first**: Write all test types and tests before any code changes
2. **Run tests**: If all pass, the feature is complete (existing mechanism works)
3. **Fix if needed**: If any tests fail, fix the specific bug in SchemaAnalyzer
4. **Snapshot approval**: Review and approve all new snapshot files
5. **Close issue #15**: Reference the test evidence in the issue closure

## Key Files

| File | Action |
|------|--------|
| `test/FSharp.Data.JsonSchema.Core.Tests/TestTypes.fs` | Add recursive test types |
| `test/FSharp.Data.JsonSchema.Core.Tests/AnalyzerTests.fs` | Add Core IR tests |
| `test/FSharp.Data.JsonSchema.Tests/TestTypes.fs` | Add recursive test types |
| `test/FSharp.Data.JsonSchema.Tests/GeneratorTests.fs` | Add snapshot tests |
| `test/FSharp.Data.JsonSchema.Tests/ValidationTests.fs` | Add validation tests |
| `test/FSharp.Data.JsonSchema.OpenApi.Tests/TransformerIntegrationTests.fs` | Add OpenApi tests |

## Verification

```bash
# Run all tests
dotnet test

# Run only Core tests
dotnet test test/FSharp.Data.JsonSchema.Core.Tests/

# Run only NJsonSchema tests
dotnet test test/FSharp.Data.JsonSchema.Tests/

# Run only OpenApi tests
dotnet test test/FSharp.Data.JsonSchema.OpenApi.Tests/
```
