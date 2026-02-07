# Research: Extended Type Support

**Feature**: 002-extended-types | **Date**: 2026-02-06

## R1: Anonymous Record Detection via Reflection

**Decision**: Use `FSharpType.IsRecord(ty, BindingFlags.Public ||| BindingFlags.NonPublic)` combined with checking the type's `CustomAttributes` for `CompilationMappingAttribute` with `SourceConstructFlags.RecordType`. Anonymous records are compiler-generated record types whose names start with `<>f__AnonymousType` or carry the anonymous record flags.

**Rationale**: F# anonymous records are compiled as generic classes with compiler-generated names. `FSharpType.IsRecord` with `allowAccessToPrivateRepresentation = true` (the existing `true` argument) returns `true` for anonymous records. The key differentiation is that anonymous records have no TypeId (they're ephemeral, inline types) and should be represented as inline `SchemaNode.Object` without generating `$ref` definitions.

**Alternatives considered**:
- Checking type name pattern (`<>f__AnonymousType`): Fragile, depends on compiler internals
- Using `FSharp.Reflection.FSharpType.IsAnonymousRecord` (F# 6+): This is the ideal approach if available on all target frameworks
- Treating as classes: Would miss the record semantics (all fields required, etc.)

**Resolution**: Check `FSharpType.IsRecord(ty, true)` — this already catches anonymous records. The existing `analyzeRecord` code at line 193 should already work. The issue is likely that anonymous records are NOT detected by `FSharpType.IsRecord` on older TFMs or that they need `FSharp.Reflection` helpers not currently imported. **Verify at implementation time** whether `FSharpType.IsRecord(ty, true)` returns `true` for anonymous records on all targeted frameworks, and fall back to name-pattern detection if needed.

## R2: Choice Type Detection

**Decision**: Detect Choice types by checking if the type is a generic type whose generic type definition is one of `Choice<_,_>` through `Choice<_,_,_,_,_,_,_>` (7 variants in FSharp.Core). Extract type arguments via `ty.GetGenericArguments()` and analyze each as a constituent schema for `SchemaNode.AnyOf`.

**Rationale**: Choice types are regular F# discriminated unions in FSharp.Core with well-known generic type definitions. They must be intercepted *before* the general DU handling (line 189 of SchemaAnalyzer.fs) to prevent InternalTag encoding from being applied. Each `Choice<N>Of<M>` case wraps exactly one value of the corresponding type parameter, so the schema should be `AnyOf` of the type parameter schemas directly (not of the case wrapper objects).

**Alternatives considered**:
- Treating Choice as a normal DU with encoding style override: Would still generate case wrapper objects with discriminator, which is unnecessarily complex for what is semantically a simple type union
- Only handling `Choice<'a,'b>`: Would miss 3+ parameter variants that users may use

**Resolution**: Add a `isChoiceType` helper that checks `ty.IsGenericType` and matches the generic type definition against `typedefof<Choice<_,_>>` etc. Insert this check before line 189 in the type dispatch chain.

## R3: DU Encoding Style Implementation — Schema Shapes

**Decision**: Modify `buildCaseSchema` to accept a `UnionEncodingStyle` parameter and produce different `SchemaNode` structures per style. The encoding style is resolved by: per-type `[<JsonFSharpConverter>]` attribute → global `config.UnionEncoding` → default InternalTag.

**Rationale**: Each encoding style produces a fundamentally different JSON structure:

### InternalTag (existing, no change)
```json
{ "kind": "CaseName", "field1": "value", "field2": 42 }
```
Schema: `Object` with discriminator property + field properties.

### AdjacentTag (new)
```json
{ "Case": "CaseName", "Fields": { "field1": "value", "field2": 42 } }
```
Schema: `Object` with two properties — a `Const` tag property and a nested `Object` for fields. The tag property name defaults to `"Case"` and fields property name defaults to `"Fields"` (matching FSharp.SystemTextJson defaults).

### ExternalTag (new)
```json
{ "CaseName": { "field1": "value", "field2": 42 } }
```
Schema: `Object` with a single property named after the case, containing the fields object. For fieldless cases: `"CaseName": {}` or `"CaseName": true`.

### Untagged (new)
```json
{ "field1": "value", "field2": 42 }
```
Schema: `AnyOf` of case schemas without any discriminator properties. Structurally identical cases are valid (deserialization ambiguity is the user's concern).

**Alternatives considered**:
- Using `OneOf` for all tagged styles with discriminator mapping: Could work but `AnyOf` is already the pattern used for InternalTag
- Separate builder functions per encoding: More duplication but clearer — rejected in favor of parameterized `buildCaseSchema`

## R4: Per-Type Attribute Detection for JsonFSharpConverter

**Decision**: Add a helper function `getUnionEncodingForType` that checks for `[<JsonFSharpConverter(UnionEncoding = ...)>]` on the DU type and extracts the encoding flags. Map the FSharp.SystemTextJson flags to the Core `UnionEncodingStyle` enum.

**Rationale**: FSharp.SystemTextJson's `JsonFSharpConverter` attribute accepts a `JsonUnionEncoding` flags enum. The relevant flag combinations for tag placement are:
- `InternalTag` (flag value `0x02_00`)
- `AdjacentTag` (flag value `0x01_00`)
- `ExternalTag` (flag value `0x00_00`, default)
- `Untagged` (flag value `0x04_00`)

Since FSharp.SystemTextJson is already a dependency of Core, we can directly reference its types.

**Alternatives considered**:
- String-based attribute matching to avoid tight coupling: Rejected, FSharp.SystemTextJson is an allowed Core dependency per constitution
- Only reading from config (no attribute detection): User requested attribute support in clarification

## R5: Custom Format Annotation Attribute

**Decision**: Define a `[<JsonSchemaFormat("format-string")>]` attribute in FSharp.Data.JsonSchema.Core that can be applied to properties and types. The SchemaAnalyzer reads this attribute during field/property analysis and uses its value as the `format` parameter in `SchemaNode.Primitive`.

**Rationale**: The attribute approach is consistent with how .NET ecosystem libraries handle schema metadata (e.g., `[<StringLength>]`, `[<Range>]` in System.ComponentModel.DataAnnotations). Placing it in Core means it has no dependency on NJsonSchema or OpenApi.

**Alternatives considered**:
- Using System.ComponentModel.DataAnnotations attributes: Would add an unwanted dependency and doesn't have a format concept
- Configuration-based format mapping (type → format): Less granular, can't target specific properties
- Using `[<JsonPropertyName>]` or similar existing attributes: Wrong semantics

**Resolution**: New attribute `JsonSchemaFormatAttribute` in Core with a single `Format: string` property.

## R6: Constitution Compliance Verification

**Decision**: All changes comply with the constitution. No violations needed.

- **I. F#-Idiomatic API**: All new types (anonymous records, Choice, DUs) will map faithfully to JSON Schema. ✅
- **II. Minimal Dependencies**: No new dependencies. FSharp.SystemTextJson already allowed for Core. ✅
- **III. Framework Compatibility**: No new TFM-specific code expected (anonymous record reflection works on all targets with FSharp.Core 6+). ✅
- **IV. Snapshot Testing**: All new features will have Verify snapshot tests. ✅
- **V. Simplicity**: Changes are focused on schema generation. New attribute is minimal (single property). ✅
- **VI. Semantic Versioning**: New type support = MINOR bump. No existing schema output changes. ✅
