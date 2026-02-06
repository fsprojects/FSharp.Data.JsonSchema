# Feature Specification: Core Extraction and Multi-Target Architecture

**Feature Branch**: `001-core-extraction`
**Created**: 2026-02-05
**Status**: Draft
**Input**: Refactor the library to extract a Core library with a generic IR abstraction, preserving the existing NJsonSchema target and enabling a new Microsoft.OpenApi target.

## Scope Classification

This feature contains two distinct workstreams with different risk profiles:

**Extraction (refactor existing behavior):** Extract the F# type analysis logic
currently embedded in NJsonSchema-specific code (`JsonSchema.fs`) into a
standalone Core library with an intermediate representation. Rebuild the
existing NJsonSchema output as a thin translator over the IR. All existing
tests MUST pass unchanged.

**New Features (additive capability):** Extend the analyzer with type support
and configuration options that do not exist in the current library. These are
explicitly called out below and MAY be deferred to a follow-up release without
blocking the extraction or the OpenApi target.

### What exists today (extraction scope)

The current library handles these types and exactly one encoding style:

- **Records**: Fields become properties, non-nullable fields marked required
- **Struct records**: Same as records
- **Multi-case DUs** (internal tag + named fields only): `anyOf` with per-case
  objects, discriminator property (`casePropertyName`, default `"kind"`),
  fieldless cases become string enums
- **Fieldless DUs** (all cases empty): String enum schema
- **Enums**: Integer-backed enums with string enumeration values
- **Option/ValueOption**: Nullable type handling (None adds `JsonObjectType.Null`)
- **Skippable fields**: Via `FSharp.SystemTextJson.Skippable<'T>`
- **Lists**: `'T list`, `ResizeArray<'T>` as array schemas
- **Arrays**: `'T array` as array schemas
- **Sequences**: `'T seq` as array schemas
- **Nullable<'T>**: Via NJsonSchema's built-in handling
- **Recursive types**: Mutually recursive DUs (e.g., `Chicken`/`Egg`,
  `Even`/`Odd`) via NJsonSchema's reference resolution
- **Records with annotations**: `[<Required>]`, `[<MaxLength>]`, `[<Range>]`
  via NJsonSchema's built-in annotation processing
- **Nested types**: DUs containing records, other DUs, enums, classes, options
- **Generic types**: `PaginatedResult<'T>`, `RecWithGenericOption<'T>` etc.
- **Primitives**: string, int, float, decimal, bool (via NJsonSchema defaults)
- **Classes**: .NET classes with get/set properties (via NJsonSchema defaults)

**Encoding**: The library is hardcoded to `InternalTag` + `NamedFields` +
`UnwrapFieldlessTags` + `UnwrapOption` + `SkippableOptionFields` via
`FSharp.SystemTextJson` configuration in `Serializer.fs`. The
`MultiCaseDuSchemaProcessor` produces schemas matching only this encoding.

**Dependencies used in type analysis** (relevant to extraction boundary):
- `Microsoft.FSharp.Reflection` — union case/record field introspection
- `Namotion.Reflection` — contextual type metadata (transitive via NJsonSchema)
- `NJsonSchema` — schema object construction, reference resolution, annotation
  processing, and the `ISchemaProcessor` / `SchemaProcessorContext` pipeline
- `NJsonSchema.Generation` — `JsonSchemaGeneratorSettings`, `DefaultReflectionService`,
  `DefaultSchemaNameGenerator`

### What does NOT exist today (new feature scope)

- **AdjacentTag encoding** (`{"Case": "Name", "Fields": [...]}`)
- **ExternalTag encoding** (`{"Name": [...]}`)
- **Untagged encoding** (infer case from shape)
- **Configurable `RecordFieldsRequired`** (currently hardcoded: always required)
- **Configurable `UnwrapSingleCaseDU`** (no unwrapping logic exists; single-case
  DUs with fields are processed by `MultiCaseDuSchemaProcessor` like any other DU)
- **Configurable `OptionSchemaStyle`** (currently hardcoded: nullable only)
- **Anonymous record handling** (not implemented, no tests)
- **`Map<string, 'T>` as additionalProperties** (no Map handling exists)
- **`Map<'K, 'T>` as array of key-value pairs** (no Map handling exists)
- **`Set<'T>` as array** (no Set handling exists)
- **DateOnly, TimeOnly, TimeSpan, Guid, Uri, byte[] format mappings**
  (currently delegated to NJsonSchema defaults; explicit format annotations
  would be new in the IR)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Existing NJsonSchema Consumer Upgrades (Priority: P1)

A developer currently using `FSharp.Data.JsonSchema` to generate NJsonSchema
objects from F# types upgrades to the new version. Their existing code
continues to compile and produce identical schema output without any source
changes.

**Why this priority**: Backward compatibility is the highest priority. Breaking
existing consumers would undermine trust and adoption. The refactoring MUST be
transparent to current users.

**Independent Test**: Install the updated `FSharp.Data.JsonSchema` package in a
project that uses the existing public API (`Generator.Create`,
`Generator.CreateMemoized`, `Validation.validate`, `FSharp.Data.Json`
serialization helpers). All existing snapshot tests pass with unchanged expected
output.

**Acceptance Scenarios**:

1. **Given** a project referencing `FSharp.Data.JsonSchema` v2.x, **When** the
   developer upgrades to v3.x, **Then** all code compiles without modification
   and all generated schemas are byte-identical to the previous version's output.
2. **Given** a developer calling `Generator.Create` for a discriminated union
   type, **When** the schema is generated, **Then** the output contains `anyOf`
   entries matching the current library's behavior exactly.
3. **Given** a developer using `Validation.validate` with a schema and JSON
   string, **When** they upgrade, **Then** validation results are identical to
   the previous version.

---

### User Story 2 - Core Library for Custom Targets (Priority: P2)

A library author wants to build a schema target for a format other than
NJsonSchema or Microsoft.OpenApi. They reference `FSharp.Data.JsonSchema.Core`
directly, analyze F# types into the intermediate representation, and translate
the IR to their target format using exhaustive pattern matching.

**Why this priority**: The Core library is the foundation that enables all
downstream targets. It MUST be independently usable and well-specified so that
third-party consumers can build on it.

**Independent Test**: Reference only `FSharp.Data.JsonSchema.Core` (no
NJsonSchema or OpenApi dependencies). Call `SchemaAnalyzer.analyze` on all type
categories the current library supports. Pattern match over the resulting
`SchemaDocument` and verify the IR captures all type semantics correctly.

**Acceptance Scenarios**:

1. **Given** a project referencing only `FSharp.Data.JsonSchema.Core`, **When**
   a developer calls `SchemaAnalyzer.analyze` on a record type, **Then** they
   receive a `SchemaDocument` with an `Object` root containing correctly named
   properties, required fields, and type identifiers.
2. **Given** a multi-case discriminated union, **When** analyzed with
   `InternalTag` encoding (the current library's only supported encoding),
   **Then** the IR contains an `AnyOf` node with one `Object` per case, each
   with a `Const` discriminator field and appropriate case fields.
3. **Given** a recursive type (e.g., `Chicken`/`Egg` mutual recursion), **When**
   analyzed, **Then** the IR contains `Ref` nodes at recursion points and
   corresponding entries in `SchemaDocument.Definitions`.
4. **Given** a developer pattern-matching over `SchemaNode`, **When** a new node
   variant is added to the IR in a future release, **Then** the compiler emits
   an incomplete-match warning, preventing silent omissions.

---

### User Story 3 - ASP.NET Core OpenAPI with F# Types (Priority: P3)

An F# developer building an ASP.NET Core Minimal API application registers
`FSharpSchemaTransformer` in their OpenAPI configuration. When the application
generates its OpenAPI document, F# discriminated unions, records, and option
types produce correct schemas instead of the opaque or incorrect output from
the default `JsonSchemaExporter`.

**Why this priority**: This is the primary motivating use case for the
refactoring, but it depends on both Core (US2) and the OpenApi translator being
complete. It delivers the end-user-visible value.

**Independent Test**: Create a minimal ASP.NET Core application with endpoints
using F# record and DU types. Register `FSharpSchemaTransformer`. Request the
`/openapi/v1.json` endpoint and verify that schemas for F# types are correct.

**Acceptance Scenarios**:

1. **Given** an ASP.NET Core Minimal API with an endpoint returning an F# record
   type, **When** `FSharpSchemaTransformer` is registered and the OpenAPI
   document is generated, **Then** the schema for that record type contains
   correct properties, required fields, and types.
2. **Given** an endpoint accepting a discriminated union as input, **When** the
   OpenAPI document is generated, **Then** the schema contains `anyOf` entries
   matching the union's InternalTag encoding with the configured discriminator
   property.
3. **Given** a type not recognized as an F# type (e.g., a plain C# class),
   **When** the schema transformer processes it, **Then** the default schema
   generation passes through unchanged.
4. **Given** two endpoints that share an F# type, **When** the OpenAPI document
   is generated, **Then** the shared type appears once in `components/schemas`
   and both endpoints reference it via `$ref`.

---

### User Story 4 - Schema Generation Configuration (Priority: P2)

A developer needs to configure schema generation to match their application's
JSON serialization settings. They create a `SchemaGeneratorConfig` and pass it
to the analyzer or transformer. The generated schemas match the actual wire
format their application produces.

**Why this priority**: Configuration alignment between serialization and schema
generation is essential for correct schemas. Mismatched configuration produces
schemas that don't match actual JSON output.

**Independent Test**: Analyze the same discriminated union type with different
configuration values and verify that each produces the expected IR.

**Acceptance Scenarios** *(extraction scope — InternalTag encoding)*:

1. **Given** a discriminated union type and `InternalTag` encoding with a custom
   discriminator property name, **When** the type is analyzed, **Then** the IR
   uses `AnyOf` with per-case objects containing a `Const` field matching the
   custom property name.
2. **Given** a record with `Option<string>` fields, **When** analyzed, **Then**
   the option field schema is wrapped in a `Nullable` node (matching current
   behavior).
3. **Given** a custom `PropertyNamingPolicy`, **When** a record is analyzed,
   **Then** all property names in the IR reflect the naming policy.

**Acceptance Scenarios** *(new feature scope — additional encodings)*:

4. **Given** the same DU type with `ExternalTag` encoding, **When** analyzed,
   **Then** the IR uses `AnyOf` with each case wrapped in a single-property
   object. *(NEW FEATURE)*
5. **Given** `InternalTag` encoding with `OneOf` semantics, **When** analyzed,
   **Then** the IR uses `OneOf` with a `Discriminator`. *(NEW FEATURE)*
6. **Given** a record with `Option<string>` and `OptionSchemaStyle.OmitWhenNone`,
   **When** analyzed, **Then** the option field is not in the required list and
   its schema is the unwrapped inner type. *(NEW FEATURE)*

---

### User Story 5 - Extended Type Support (Priority: P4)

A developer analyzes types that the current library does not handle (anonymous
records, Maps, Sets, single-case DU unwrapping). The analyzer produces correct
IR for these types.

**Why this priority**: These are additive features that expand the library's
coverage. They are valuable but not required for the core extraction to succeed
or for the OpenApi target to be useful.

**Independent Test**: Analyze each new type category and verify IR output.

**Acceptance Scenarios**:

1. **Given** an anonymous record type, **When** analyzed, **Then** the IR
   produces an `Object` node with correct property schemas. *(NEW FEATURE)*
2. **Given** `Map<string, 'T>`, **When** analyzed, **Then** the IR produces a
   `Map` node with the value schema. *(NEW FEATURE)*
3. **Given** `Set<'T>`, **When** analyzed, **Then** the IR produces an `Array`
   node. *(NEW FEATURE)*
4. **Given** a single-case DU with `UnwrapSingleCaseDU = true`, **When**
   analyzed, **Then** the IR produces the inner field's schema directly.
   *(NEW FEATURE)*

---

### Edge Cases

- What happens when a type contains `obj` or `System.Object` fields? The
  analyzer MUST produce a permissive schema (no type constraint) rather than
  failing.
- How does the system handle generic types with concrete type parameters (e.g.,
  `Result<User, Error>`)? Each concrete instantiation MUST produce a distinct
  schema.
- What happens with deeply nested generic types (e.g.,
  `Map<string, Option<User list>>`)? The analyzer MUST correctly compose nested
  IR nodes. *(Note: Map handling is new feature scope.)*
- How does the system handle mutually recursive types (A references B, B
  references A)? Both MUST appear in `Definitions` with cross-references via
  `Ref`.
- What happens when a type has no public properties or fields (e.g., an empty
  record)? The analyzer MUST produce a valid `Object` schema with empty
  properties.
- How does the OpenApi transformer handle types it does not recognize as F#
  types? It MUST pass them through to the default schema generation unchanged.

## Clarifications

### Session 2026-02-05

- Q: Should Core depend on FSharp.SystemTextJson for union encoding config, or define its own enum? → A: Core defines its own `UnionEncodingStyle` enum for its public API but MAY depend on `FSharp.SystemTextJson` as a runtime dependency (for `Skippable<'T>` detection and internal alignment). Callers configure via `SchemaGeneratorConfig`. See research.md R9 for rationale.
- Q: What target frameworks for the new packages? → A: Core and existing package target all 5 existing TFMs (netstandard2.0, netstandard2.1, netcoreapp3.1, net6.0, net8.0) plus net9.0 and net10.0. OpenApi targets net9.0 and net10.0 (constrained by Microsoft.AspNetCore.OpenApi dependency).
- Q: What should the `additionalProperties` default be in the IR? → A: Default `true` (JSON Schema standard). DU case schemas explicitly set `false`. The setting is configurable via `SchemaGeneratorConfig` so consumers can override the default. The goal is valid JSON Schema, which may require differences from F# defaults.
- Q: What `$ref` type identifier naming convention should the default `TypeIdResolver` use? → A: Match the current library's naming behavior as default. Fall back to short type name (e.g., `Person`) where there is ambiguity. Custom `TypeIdResolver` via `SchemaGeneratorConfig` remains available for full control.

## Requirements *(mandatory)*

### Functional Requirements — Extraction Scope

These requirements extract and refactor existing behavior. They MUST be
completed before new features are added.

- **FR-001**: The system MUST provide a Core library (`FSharp.Data.JsonSchema.Core`) with no dependencies on NJsonSchema, Microsoft.OpenApi, or Namotion.Reflection. Core MAY depend on `FSharp.SystemTextJson` (for `Skippable<'T>` detection and union encoding alignment).
- **FR-002**: The Core library MUST define a discriminated union IR (`SchemaNode`) that captures JSON Schema semantics for all F# types the library currently supports (see "What exists today" in Scope Classification).
- **FR-003**: The Core library MUST provide a `SchemaAnalyzer` that reflects over F# types and produces `SchemaDocument` values.
- **FR-004**: The IR MUST support: objects, arrays, anyOf, oneOf with discriminator, nullable, primitives with format, enums, references (for recursion), maps, and constant values. The IR is designed to be forward-compatible — node types MAY exist in the IR before the analyzer produces them, to support future features.
- **FR-005**: The `SchemaAnalyzer` MUST handle all type categories listed under "What exists today": records, struct records, multi-case DUs (InternalTag encoding), fieldless DUs, enums, `list`, `array`, `seq`, `ResizeArray`, option/value-option types, `Nullable<'T>`, `Skippable<'T>`, recursive types, generic types, nested types, annotated records, .NET classes, and primitives. *Clarification*: "Annotated records" means the analyzer handles records structurally (properties, required fields); annotation-specific constraints (`[<Required>]`, `[<MaxLength>]`, `[<Range>]`) remain in the NJsonSchema post-processing pipeline and are not represented in the Core IR. ".NET classes" are handled via `System.Reflection` property discovery (public instance properties with getters), producing `Object` schemas.
- **FR-006**: The Core library MUST provide a `SchemaGeneratorConfig` type. For extraction scope, it MUST support: discriminator property name (default: `"kind"`), property naming policy (default: camelCase), and the default `additionalProperties` behavior (default: `true` per JSON Schema standard).
- **FR-007**: The existing `FSharp.Data.JsonSchema` package MUST be refactored to depend on Core and translate IR to NJsonSchema types via an internal translator.
- **FR-008**: The existing public API surface of `FSharp.Data.JsonSchema` MUST remain unchanged — `Generator` (with `Create` and `CreateMemoized`), `Validation` module (`validate`), `FSharp.Data.Json` class (all `Serialize`/`Deserialize`/`DeserializeWithValidation` overloads), and all `ISchemaProcessor` implementations.
- **FR-009**: The schema output of the refactored `FSharp.Data.JsonSchema` MUST be string-identical (as verified by Verify snapshot comparison) to the pre-refactoring output for all existing snapshot test types.
- **FR-010**: The `Validation` module and `FSharp.Data.Json.DeserializeWithValidation` MUST remain in the `FSharp.Data.JsonSchema` package (they depend on NJsonSchema's `JsonSchemaValidator`).
- **FR-011**: `FSharp.Data.JsonSchema.Core` and `FSharp.Data.JsonSchema` MUST target `netstandard2.0`, `netstandard2.1`, `netcoreapp3.1`, `net6.0`, `net8.0`, `net9.0`, and `net10.0`.

### Functional Requirements — OpenApi Target

- **FR-012**: A new `FSharp.Data.JsonSchema.OpenApi` package MUST translate Core IR to `Microsoft.OpenApi.Models.OpenApiSchema` objects.
- **FR-013**: The OpenApi package MUST provide an `IOpenApiSchemaTransformer` implementation (`FSharpSchemaTransformer`) that integrates with ASP.NET Core's OpenAPI pipeline.
- **FR-014**: The `FSharpSchemaTransformer` MUST detect F# types (unions, records, types with option fields) and replace their default schemas with correct schemas from the analyzer.
- **FR-015**: The `FSharpSchemaTransformer` MUST pass through non-F# types unchanged.
- **FR-016**: Shared types referenced by multiple endpoints MUST appear once in OpenAPI `components/schemas` and be referenced via `$ref`.
- **FR-017**: `FSharp.Data.JsonSchema.OpenApi` MUST target `net9.0` and `net10.0`.

### Functional Requirements — New Features

These extend the analyzer beyond current library behavior. They MAY be
deferred to a follow-up release.

- **FR-018**: The `SchemaAnalyzer` MUST support `AdjacentTag`, `ExternalTag`, and `Untagged` union encoding styles in addition to the existing `InternalTag` encoding. *(NEW FEATURE)*
- **FR-019**: `SchemaGeneratorConfig` MUST support configurable `RecordFieldsRequired` (default: `true`, matching current hardcoded behavior). *(NEW FEATURE)*
- **FR-020**: `SchemaGeneratorConfig` MUST support configurable `UnwrapSingleCaseDU` for single-case DUs with fields. *(NEW FEATURE)*
- **FR-021**: `SchemaGeneratorConfig` MUST support configurable `OptionSchemaStyle` (`Nullable` vs `OmitWhenNone`; default: `Nullable`, matching current behavior). *(NEW FEATURE)*
- **FR-022**: The `SchemaAnalyzer` MUST handle anonymous records. *(NEW FEATURE)*
- **FR-023**: The `SchemaAnalyzer` MUST handle `Map<string, 'T>` as `additionalProperties` schemas and `Map<'K, 'T>` (non-string key) as array of key-value pairs. *(NEW FEATURE)*
- **FR-024**: The `SchemaAnalyzer` MUST handle `Set<'T>` as array schemas. *(NEW FEATURE)*
- **FR-025**: The `SchemaAnalyzer` MUST produce explicit format annotations for `DateOnly`, `TimeOnly`, `TimeSpan`, `Guid`, `Uri`, and `byte[]` instead of delegating to target-specific defaults. *(NEW FEATURE)*

### Key Entities

- **SchemaNode**: A discriminated union representing a single node in the schema IR — the core abstraction. Variants include Object, Array, AnyOf, OneOf, Nullable, Primitive, Enum, Ref, Map, Const, and Any.
- **SchemaDocument**: The complete result of analyzing a type — contains a root `SchemaNode` and a definitions map for types referenced via `Ref`.
- **SchemaGeneratorConfig**: Configuration controlling how F# types map to schema constructs. Extraction scope: discriminator property name, property naming policy, additionalProperties default. New feature scope adds: union encoding style, option style, single-case DU unwrapping, record field requiredness, type ID resolution.
- **ObjectSchema**: Properties, required fields, additional-properties flag, type identifier, and metadata for an object-type schema node.
- **PropertySchema**: A named property with its schema and optional description.
- **Discriminator**: Property name and value-to-reference mapping for tagged union schemas.

### Assumptions

- The existing public API consists of: `Generator` class (`Create`, `CreateMemoized` — both public), `Validation` module (`validate` — public), `FSharp.Data.Json` class (`Serialize`, `Deserialize`, `DefaultOptions` — public; `DefaultCasePropertyName` — internal), `DeserializeWithValidation` extension methods, and `ISchemaProcessor` implementations (`OptionSchemaProcessor`, `SingleCaseDuSchemaProcessor`, `MultiCaseDuSchemaProcessor`, `RecordSchemaProcessor` — all public).
- The `ISchemaProcessor` extension point and all four processor implementations are NJsonSchema-specific and MUST remain in the `FSharp.Data.JsonSchema` package (not extracted to Core).
- The `Validation` module wraps `NJsonSchema.Validation.JsonSchemaValidator` and returns `NJsonSchema.Validation.ValidationError` arrays. It MUST remain in the NJsonSchema package.
- The current library depends on `Namotion.Reflection` (transitive via NJsonSchema) for contextual type metadata in `ReflectionService` and `SchemaNameGenerator`. Core MUST NOT depend on `Namotion.Reflection`; the analyzer MUST use `Microsoft.FSharp.Reflection` and `System.Reflection` directly.
- Core MUST define its own `UnionEncodingStyle` enum for its public API, but MAY depend on `FSharp.SystemTextJson` as a runtime dependency for `Skippable<'T>` handling and internal alignment. Callers configure schema generation via `SchemaGeneratorConfig` rather than `FSharp.SystemTextJson` flags directly.
- The OpenApi target initially targets OpenAPI 3.0 semantics (using `nullable: true` rather than `type: ["string", "null"]`), matching ASP.NET Core .NET 9's output format.
- Schema caching will be the caller's responsibility initially. The existing `Generator.CreateMemoized` pattern continues to work via its `ConcurrentDictionary` cache in the NJsonSchema package.
- NJsonSchema's built-in handling of annotations (`[<Required>]`, `[<MaxLength>]`, `[<Range>]`) and class properties occurs outside the `ISchemaProcessor` pipeline. The Core IR does not need to represent annotations; annotation processing remains target-specific.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing snapshot tests pass without modification after the refactoring, confirming identical NJsonSchema output.
- **SC-002**: The Core library can be referenced and used independently — a project depending only on `FSharp.Data.JsonSchema.Core` (which transitively brings in `FSharp.SystemTextJson`) can analyze types and inspect the IR without pulling in NJsonSchema, Microsoft.OpenApi, or Namotion.Reflection.
- **SC-003**: All type categories listed under "What exists today" produce correct IR when analyzed through `SchemaAnalyzer`, verified by a new Core-level test suite.
- **SC-004**: An ASP.NET Core application using `FSharpSchemaTransformer` produces an OpenAPI document where F# discriminated unions show correct `anyOf` schemas instead of opaque empty schemas.
- **SC-005**: The `FSharpSchemaTransformer` does not alter schemas for non-F# types — existing C#/plain-.NET type schemas remain unchanged.
- **SC-006**: Pattern matching over `SchemaNode` in consumer code produces compiler incomplete-match warnings when new variants are added, ensuring exhaustive handling.
- **SC-007**: New feature requirements (FR-018 through FR-025) each have dedicated tests that pass when implemented, independent of the extraction scope tests.
