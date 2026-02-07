# Quickstart: Extended Type Support

**Feature**: 002-extended-types | **Date**: 2026-02-06

## Implementation Order

Work on these features in this order. Each is independently testable and can be merged incrementally.

### Step 1: Choice Type Support (Smallest, Highest Confidence)

**Why first**: Simplest change — add a type check before the DU dispatch, produce `AnyOf` of type arguments. No config changes needed. Resolves GitHub issue #22.

**Key file**: `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`

**What to do**:
1. Add `isChoiceType` helper that checks generic type definition against `typedefof<Choice<_,_>>` etc.
2. Insert check before `FSharpType.IsUnion` in the type dispatch chain (~line 189)
3. Extract generic type arguments, analyze each, produce `SchemaNode.AnyOf`
4. Add test types: `Choice<string, int>`, `Choice<string, int, bool>`, `Choice<string, ComplexRecord>`, nested `Choice<int, Choice<string, bool>>`
5. Write snapshot tests

### Step 2: Anonymous Record Support (Small, Medium Confidence)

**Why second**: Isolated change in type dispatch — add detection before the record check. No config changes needed.

**Key file**: `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`

**What to do**:
1. Add anonymous record detection (try `FSharpType.IsRecord` with binding flags, verify on all TFMs)
2. Reuse existing record analysis logic but produce `SchemaNode.Object` with `TypeId = None` and `Title = None`
3. Add test types: simple anonymous record, nested, with optional fields, in collections
4. Write snapshot tests

### Step 3: DU Encoding Styles (Largest, Core Change)

**Why third**: Requires modifying the existing DU analysis functions which have the most risk of breaking existing tests.

**Key files**: `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`

**What to do**:
1. Add `resolveUnionEncoding` helper that reads per-type `[<JsonFSharpConverter>]` attribute, falls back to `config.UnionEncoding`
2. Modify `analyzeDU` to call `resolveUnionEncoding` and pass result to `buildCaseSchema`
3. Add `encodingStyle` parameter to `buildCaseSchema`
4. Implement 4 code paths in `buildCaseSchema`:
   - `InternalTag`: Existing behavior (no change)
   - `AdjacentTag`: Tag + Fields adjacent properties
   - `ExternalTag`: Case name wraps fields
   - `Untagged`: Fields only, no discriminator
5. Modify `analyzeMultiCaseDU` to not use discriminator for Untagged
6. Add test types for each style (fieldless, single-field, multi-field)
7. Write snapshot tests — **run existing tests first to ensure InternalTag output is identical**

### Step 4: Custom Format Annotations (Smallest, Isolated)

**Why last**: Depends on nothing else, purely additive, lowest risk.

**Key files**:
- New: `src/FSharp.Data.JsonSchema.Core/JsonSchemaFormatAttribute.fs`
- Modified: `src/FSharp.Data.JsonSchema.Core/SchemaAnalyzer.fs`

**What to do**:
1. Define `JsonSchemaFormatAttribute` in Core
2. In `SchemaAnalyzer`, check for the attribute on `PropertyInfo` during field analysis
3. When present, use attribute's format value instead of built-in inference
4. Add test types with `[<JsonSchemaFormat("email")>]` etc.
5. Write snapshot tests

## Build & Test Commands

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run only Core tests
dotnet test test/FSharp.Data.JsonSchema.Core.Tests/

# Run only main tests (NJsonSchema snapshots)
dotnet test test/FSharp.Data.JsonSchema.Tests/

# Run only OpenApi tests
dotnet test test/FSharp.Data.JsonSchema.OpenApi.Tests/

# Accept updated snapshots (after verifying changes are correct)
# Delete .received. files that differ and rename them to .verified.
```

## Risk Areas

1. **Anonymous record detection on netstandard2.0**: `FSharpType.IsRecord` may not detect anonymous records on older TFMs. Test early.
2. **InternalTag regression**: Modifying `buildCaseSchema` could break existing 89 snapshot tests. Run tests after every DU encoding change.
3. **FSharp.SystemTextJson attribute API**: The `JsonFSharpConverter` attribute's property names and types may vary across versions. Pin to the version in the project's dependencies.
