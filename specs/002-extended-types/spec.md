# Feature Specification: Extended Type Support for JSON Schema Generation

**Feature Branch**: `002-extended-types`
**Created**: 2026-02-06
**Status**: Draft
**Input**: User description: "Complete Phase 7 from the last spec - Extended type support (anonymous records, Map, Set, format annotations, encoding styles)"

## Clarifications

### Session 2026-02-06

- Q: Map, Set, and built-in format annotations are already implemented with tests. Should scope be narrowed to actual gaps (anonymous records, Choice, DU encoding styles, custom format annotations)? → A: Yes, narrow to 4 actual gaps. Existing Map/Set/format support is complete and out of scope.
- Q: Should the analyzer detect per-type `[<JsonFSharpConverter>]` attributes on DU types, or only respect the global config? → A: Both. Per-type attributes override the global config setting.
- Q: Should all DU encoding styles assume NamedFields, or also support positional array field representation? → A: NamedFields only. Matches current serializer config and common JSON API usage.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate schemas for anonymous records (Priority: P1)

As a developer with a record type containing an anonymous record property, I need the schema generator to recognize and properly serialize the anonymous record structure so my API documentation accurately reflects all nested fields.

**Why this priority**: Anonymous records are commonly used in F# for lightweight data structures. Currently they fall through to `SchemaNode.Any`, losing all type information.

**Independent Test**: Can be fully tested by defining a record with an `{| field1: string; field2: int |}` property and verifying the generated schema contains the correct nested structure with all fields properly typed.

**Acceptance Scenarios**:

1. **Given** a record type with an anonymous record property, **When** analyzing the type, **Then** the schema includes an object definition with all anonymous record fields
2. **Given** nested anonymous records, **When** analyzing the type, **Then** the schema properly nests object definitions matching the structure

---

### User Story 2 - Support Choice types with anyOf encoding (Priority: P1)

As a developer using `Choice<'a, 'b>` types, I need the schema generator to produce concise, standards-compliant `anyOf` schemas that accurately represent the possible union types, resolving GitHub issue #22.

**Why this priority**: Choice types are common in F# for modeling sum types. Currently they are not recognized at all and would fall through to generic DU handling, producing overly complex schemas with unnecessary discriminators.

**Independent Test**: Can be fully tested by defining a `Choice<string, int>` property and verifying the generated schema uses `anyOf` to list the constituent types directly.

**Acceptance Scenarios**:

1. **Given** a `Choice<string, int>` property, **When** analyzing the type, **Then** the schema uses `anyOf` listing string and integer type alternatives
2. **Given** a `Choice<string, int, bool>` property (3+ cases), **When** analyzing the type, **Then** the schema uses `anyOf` with all constituent types
3. **Given** a `Choice<string, ComplexRecord>` property, **When** analyzing the type, **Then** the schema uses `anyOf` with a primitive type and a `$ref` to the complex type definition

---

### User Story 3 - Support all DU encoding styles (Priority: P1)

As a developer with discriminated unions using different FSharp.SystemTextJson encoding styles, I need the schema generator to respect the configured encoding (InternalTag, AdjacentTag, ExternalTag, Untagged) so the generated schema matches actual JSON serialization behavior.

**Why this priority**: The `UnionEncodingStyle` IR and config field already exist but the analyzer hardcodes InternalTag, ignoring the configuration. This is the most impactful gap for users whose serialization uses a non-default encoding style.

**Independent Test**: Can be fully tested by configuring different `UnionEncoding` styles and verifying each produces the correct schema structure.

**Acceptance Scenarios**:

1. **Given** a DU with InternalTag encoding (via config), **When** analyzing the type, **Then** the schema includes a discriminator property inside each case object (existing behavior, must remain unchanged)
2. **Given** a DU with AdjacentTag encoding (via config), **When** analyzing the type, **Then** the schema represents each case as an object with adjacent tag and fields properties
3. **Given** a DU with ExternalTag encoding (via config), **When** analyzing the type, **Then** the schema represents each case wrapped by its case name as a key
4. **Given** a DU with Untagged encoding (via config), **When** analyzing the type, **Then** the schema uses `anyOf` without any discriminator
5. **Given** a DU with a `[<JsonFSharpConverter(UnionEncoding = ExternalTag)>]` attribute and a global config of InternalTag, **When** analyzing the type, **Then** the per-type attribute takes precedence and the schema uses ExternalTag encoding

---

### User Story 4 - Support custom format annotations via attributes (Priority: P2)

As a developer wanting to extend built-in format support (DateTime→"date-time", Guid→"guid", etc.) with custom format hints, I need a way to annotate properties so generated schemas include user-specified format values.

**Why this priority**: Built-in format inference already works for standard types. This story adds extensibility for custom formats (e.g., annotating a string property as `"uuid"` or `"email"`).

**Independent Test**: Can be fully tested by annotating a string property with a custom format attribute and verifying the generated schema includes the specified format value.

**Acceptance Scenarios**:

1. **Given** a string property annotated with a custom format attribute specifying "email", **When** analyzing the type, **Then** the schema includes `"format": "email"`
2. **Given** a property with both a built-in format inference (e.g., DateTime) and an explicit attribute, **When** analyzing the type, **Then** the explicit attribute takes precedence

---

### Edge Cases

- What happens when an anonymous record contains an optional field?
- How does the system handle deeply nested anonymous records (3+ levels)?
- What occurs when a Choice type contains complex types that require `$ref` definitions?
- What happens when a Choice type is nested recursively (e.g., `Choice<int, Choice<string, bool>>`)?
- How should Untagged DU encoding behave when cases are structurally identical (ambiguous deserialization)?
- What happens when a DU case has no fields under each encoding style?
- How should a custom format attribute interact with Nullable wrapping?

## Requirements *(mandatory)*

### Functional Requirements

#### Anonymous Record Support
- **FR-001**: SchemaAnalyzer MUST recognize F# anonymous record types (currently fall through to `SchemaNode.Any`) and analyze their field structure
- **FR-002**: SchemaNode IR MUST represent anonymous records as Object variants with field information (no new IR variants needed)
- **FR-003**: All translators (NJsonSchema, OpenApi) MUST generate valid schemas for anonymous record types
- **FR-004**: Generator MUST handle nested anonymous records by creating appropriate nested object definitions
- **FR-005**: Anonymous records MUST respect existing config settings (PropertyNamingPolicy, RecordFieldsRequired, AdditionalPropertiesDefault)

#### Choice Type Support (Issue #22)
- **FR-006**: SchemaAnalyzer MUST recognize `Choice<'a, 'b>` through `Choice<'a, ..., 'g>` types (all F# Choice variants up to 7 type parameters)
- **FR-007**: Generator MUST represent Choice types as `SchemaNode.AnyOf` with each case's type as a constituent schema
- **FR-008**: Generator MUST handle Choice types containing complex types with proper `$ref` generation within anyOf arrays
- **FR-009**: Generator MUST correctly handle nested/recursive Choice types (e.g., `Choice<int, Choice<string, bool>>`)
- **FR-010**: Choice types MUST NOT produce discriminator properties (they are inherently untagged)

#### DU Encoding Styles (all 4 styles)
- **FR-011**: SchemaAnalyzer MUST read and respect the `UnionEncoding` field from `SchemaGeneratorConfig` as the global default (currently ignored, hardcoded to InternalTag)
- **FR-012**: SchemaAnalyzer MUST detect `[<JsonFSharpConverter(UnionEncoding = ...)>]` attributes on individual DU types and use the attribute's encoding style in preference to the global config
- **FR-013**: InternalTag encoding MUST continue to produce identical output (backwards compatibility)
- **FR-014**: AdjacentTag encoding MUST generate schemas representing each case as an object with adjacent tag and fields properties
- **FR-015**: ExternalTag encoding MUST generate schemas representing each case wrapped by its case name as a key
- **FR-016**: Untagged encoding MUST generate `anyOf` schemas without discriminator information
- **FR-017**: All 4 encoding styles MUST correctly handle fieldless DU cases
- **FR-018**: All 4 encoding styles MUST correctly handle single-field and multi-field DU cases

#### Custom Format Annotations
- **FR-019**: SchemaAnalyzer MUST support an attribute-based mechanism for specifying custom format strings on properties
- **FR-020**: Explicit format annotations MUST take precedence over built-in format inference (e.g., attribute overrides DateTime's default "date-time")
- **FR-021**: SchemaNode.Primitive already carries `format: string option`; no IR changes needed
- **FR-022**: All translators MUST propagate custom format values into generated schemas

### Key Entities

- **AnonymousRecordType**: A record-like F# type without a formal type declaration, with named fields and typed values; detected at runtime via reflection
- **ChoiceType**: An F# `Choice<'a, 'b, ...>` type representing one of N possible types; always encoded as `anyOf` (no discriminator)
- **UnionEncodingStyle**: One of InternalTag, AdjacentTag, ExternalTag, or Untagged - controls how DU case names and fields appear in JSON
- **FormatAnnotation**: An attribute providing a custom JSON Schema format string for a property, overriding built-in inference

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 4 user stories (anonymous records, Choice types, DU encoding styles, custom format annotations) have passing snapshot tests
- **SC-002**: Anonymous record test suite validates at least 5 scenarios: simple, nested, optional fields, in complex types, and in collections
- **SC-003**: Choice type support resolves GitHub issue #22 with `anyOf` schemas; tests cover `Choice<'a,'b>` through at least `Choice<'a,'b,'c>`, primitive and complex type arguments, and nested Choice
- **SC-004**: DU encoding test suite validates all 4 styles (InternalTag, AdjacentTag, ExternalTag, Untagged) with at least 3 scenarios each (fieldless case, single-field case, multi-field case)
- **SC-005**: Custom format annotation test suite validates attribute-based format specification and precedence over built-in inference
- **SC-006**: All 141 existing tests continue to pass (backwards compatibility maintained)
- **SC-007**: Documentation (README, examples) updated to demonstrate the 4 new capabilities

## Assumptions

1. **Anonymous Records**: Anonymous records in F# are detectable at runtime via reflection (e.g., checking `FSharpType.IsRecord` with appropriate binding flags or compiler-generated naming patterns) and can be analyzed similarly to regular records
2. **Choice Type Encoding**: Choice types are always represented as `anyOf` (they are inherently untagged unions); the generated JSON Schema must support bidirectional mapping to F# types
3. **DU Encoding Priority**: Per-type `[<JsonFSharpConverter(UnionEncoding = ...)>]` attributes take precedence over the global `config.UnionEncoding` setting; this matches FSharp.SystemTextJson's own resolution order
4. **No IR Changes Needed**: The existing SchemaNode IR (Object, AnyOf, OneOf, Map, Array, etc.) is expressive enough for all 4 encoding styles and anonymous records; no new variants required
5. **Built-in Formats Already Complete**: Map, Set, Dictionary, DateTime/Guid/Uri/TimeSpan format inference are already implemented and tested; this spec does not modify them
6. **Backwards Compatibility**: All 141 existing tests and snapshot files remain valid; InternalTag encoding output is byte-identical

## Dependencies & Constraints

### Technical Dependencies
- F# 8.0+ for reflection on anonymous records
- No new external dependencies required (builds on existing FSharp.Core, FSharp.SystemTextJson, NJsonSchema)

### Internal Dependencies
- Builds on Phase 1-6 (001-core-extraction): Core IR, SchemaAnalyzer, NJsonSchema translator, OpenApi translator
- SchemaNode IR is sufficient as-is; changes are in SchemaAnalyzer logic and translator handling
- Config already has `UnionEncoding: UnionEncodingStyle` field that just needs to be read

### Known Constraints
- Choice types are always `anyOf` (no discriminator); alternative encoding strategies deferred to future phases
- DU encoding style resolved by: per-type `[<JsonFSharpConverter>]` attribute > global `config.UnionEncoding` > default (InternalTag)
- All DU encoding styles assume NamedFields (fields as named object properties); positional array field representation is out of scope
- Custom format annotation mechanism requires defining a new attribute type

## Already Implemented (out of scope)

The following were originally planned for Phase 7 but are already complete with tests:
- `Map<string, 'v>` / `Dictionary<string, 'v>` → `SchemaNode.Map` with typed additionalProperties
- `Set<'a>` → `SchemaNode.Array` with typed items
- Built-in format inference: DateTime→"date-time", DateTimeOffset→"date-time", DateOnly→"date", TimeOnly→"time", Guid→"guid", Uri→"uri", TimeSpan→"duration", byte[]→"byte"

## Related Issues

- **GitHub Issue #22**: `Choice<'a, 'b>` schema generation complexity - addressed in User Story 2
- Reference: FSharp.Data.JsonSchema phases 1-6 implementation (001-core-extraction branch)
