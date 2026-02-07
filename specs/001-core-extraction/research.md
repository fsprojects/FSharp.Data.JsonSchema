# Research: Core Extraction and Multi-Target Architecture

## R1: Core IR Design — SchemaNode DU vs Interfaces

**Decision**: Use a discriminated union (`SchemaNode`) for the IR.

**Rationale**: A closed DU enforces exhaustive pattern matching in every
translator. When a new node type is added, every translator gets a compiler
warning until it handles the new case. This prevents silent omissions — the
primary correctness risk in a multi-target architecture.

**Alternatives considered**:
- Interface-based IR: Open for extension but no compiler enforcement. A new
  interface method would need default implementations or break all consumers.
- Record-based IR with type tag: Loses pattern matching ergonomics; requires
  manual dispatch.

## R2: Extracting Analysis from NJsonSchema Processors

**Decision**: Rewrite the type analysis as a standalone recursive function in
Core that produces `SchemaDocument` values, rather than attempting to extract
and adapt the existing `ISchemaProcessor` implementations.

**Rationale**: The existing processors are deeply coupled to NJsonSchema types
(`SchemaProcessorContext`, `JsonSchema`, `JsonSchemaProperty`,
`context.Resolver`, `context.Generator.Generate`). They also depend on
`Namotion.Reflection` via `context.ContextualType`. Extracting these into
Core would require either (a) duplicating NJsonSchema's type infrastructure
or (b) abstracting over it — both worse than a clean rewrite.

The analysis logic itself is ~200 LOC spread across 4 processors. The
non-trivial parts are:
- `MultiCaseDuSchemaProcessor`: ~100 LOC — field iteration, option detection,
  camelCase naming, resolver/generator interaction for nested types
- `OptionSchemaProcessor`: ~30 LOC — JsonObjectType flag combination
- `RecordSchemaProcessor`: ~15 LOC — nullable property detection, required marking
- `SingleCaseDuSchemaProcessor`: ~15 LOC — all-cases-empty check, string enum

A clean rewrite producing IR nodes instead of NJsonSchema objects will be
shorter and more maintainable than the current code because it won't need to
work around NJsonSchema's mutable schema model.

**Key extraction challenges**:
- **Recursive type detection**: Currently handled by NJsonSchema's resolver
  (`context.Resolver.HasSchema`). Core must implement its own visited-set
  tracking during type traversal.
- **Nested type generation**: Currently uses `context.Generator.Generate(ty,
  resolver)`. Core's analyzer will recursively call itself.
- **Type name generation**: Currently uses `SchemaNameGenerator` which depends
  on `Namotion.Reflection.ToCachedType()`. Core must implement its own type
  naming using `System.Reflection` only.
- **Property discovery**: Currently NJsonSchema discovers properties before
  processors run. Core must use `FSharpType.GetRecordFields` and
  `FSharpType.GetUnionCases` directly.

## R3: NJsonSchema Translator Strategy

**Decision**: Implement `NJsonSchemaTranslator` as a straightforward recursive
pattern match over `SchemaNode`, constructing NJsonSchema objects. The existing
`ISchemaProcessor` implementations remain in the package but are bypassed for
types handled by the Core analyzer.

**Rationale**: The translator is a pure function from IR → NJsonSchema types.
It does not need NJsonSchema's generation pipeline — it constructs objects
directly. The existing processors are preserved for potential use with
`Generator.Create` (backward compatibility) but the internal flow becomes:

```
Generator.CreateInternal →
  SchemaAnalyzer.analyze →
  NJsonSchemaTranslator.translate →
  return NJsonSchema.JsonSchema
```

**Alternative considered**: Keep using NJsonSchema's `FromType` and processors
as-is, adding Core as a parallel path. Rejected because it would not validate
that the extraction is correct — the existing path would mask bugs in the
Core analyzer.

## R4: Microsoft.OpenApi Version Differences (net9.0 vs net10.0)

**Decision**: Use conditional compilation (`#if NET10_0_OR_GREATER`) in the
OpenApi project to handle the breaking changes between Microsoft.OpenApi 1.6.x
(net9.0) and 2.0.x (net10.0).

**Rationale**: The differences are significant:

| Aspect | net9.0 (OpenApi 1.6.x) | net10.0 (OpenApi 2.0.x) |
|--------|----------------------|------------------------|
| Nullable | `schema.Nullable = true` | `schema.Type \|= JsonSchemaType.Null` |
| Enum values | `OpenApiString` / `OpenApiInteger` | `JsonNode` |
| Schema type | `OpenApiSchema` class | `IOpenApiSchema` interface |
| Discriminator mapping | `Dictionary<string, string>` | `Dictionary<string, OpenApiSchemaReference>` |
| Document access | Not available in context | `context.Document` available |
| Schema references | `OpenApiReference` | `OpenApiSchemaReference` |

**Strategy**: The translator will have a shared core function that produces
the structural schema, with version-specific adapters for nullable handling,
enum value construction, and reference creation.

## R5: Recursive Type Handling in Core Analyzer

**Decision**: Use a `HashSet<Type>` as a visited set during analysis. When a
type is encountered that is already being analyzed, emit `Ref(typeId)` and
register a placeholder in a mutable `Dictionary<string, SchemaNode>`. Once the
full analysis of the type completes, replace the placeholder with the actual
schema.

**Rationale**: This mirrors the approach in NJsonSchema's resolver but
without the resolver's coupling to NJsonSchema types. The visited-set pattern
is the standard approach for cycle detection in recursive type analysis.

**Edge case**: Mutually recursive types (A → B → A). Both appear in the
visited set. The first encounter of A starts analysis; when B references A,
it emits `Ref("A")`. When B completes, A's analysis continues and includes B
in its definitions.

## R6: Type Name Generation for $ref Identifiers

**Decision**: Default `TypeIdResolver` uses the type's `Name` property
(short name), matching the current `DefaultSchemaNameGenerator` behavior.
For generic types, appends type arguments (e.g., `PaginatedResultOfTestRecord`).
For option types, unwraps to the inner type name.

**Rationale**: The current `SchemaNameGenerator` at `JsonSchema.fs:234-245`
already produces short names via NJsonSchema's `DefaultSchemaNameGenerator`.
Matching this default preserves backward compatibility for `$ref` identifiers
in the NJsonSchema translator.

## R7: Annotation Handling Strategy

**Decision**: Annotations (`[<Required>]`, `[<MaxLength>]`, `[<Range>]`) are
NOT represented in the Core IR. They remain NJsonSchema-specific, processed
by NJsonSchema's built-in pipeline after the translator produces the base
schema.

**Rationale**: The current library relies on NJsonSchema's automatic annotation
processing, which happens before `ISchemaProcessor` runs. The Core IR captures
structural schema semantics (types, composition, nullability) — not validation
constraints. Each target can handle annotations via its own mechanism (NJsonSchema
built-in, OpenApi schema properties, etc.).

**Implication**: The NJsonSchema translator will need to post-process the
translated schema to apply annotations. This can use NJsonSchema's existing
`FromType` for annotation discovery, or annotations can be handled in the
preserved `ISchemaProcessor` pipeline.

## R8: OpenApi IOpenApiSchemaTransformer Integration

**Decision**: Implement `FSharpSchemaTransformer` as an
`IOpenApiSchemaTransformer` that:
1. Checks `context.JsonTypeInfo.Type` for F# characteristics
2. Calls `SchemaAnalyzer.analyze` to produce IR
3. Calls `OpenApiSchemaTranslator.translate` to produce OpenApiSchema
4. Replaces properties on the provided `schema` parameter in-place
5. Registers definitions in component schemas

**Rationale**: The transformer API receives a pre-built schema and expects
in-place modification. We cannot return a new schema — we must mutate the
provided one. The transformer detects F# types via:
- `FSharpType.IsUnion(type)` — discriminated unions
- `FSharpType.IsRecord(type)` — records
- Field-level option detection for records with option fields

Non-F# types pass through unchanged (transformer returns `Task.CompletedTask`
without modification).

**Registration in .NET 9**:
```fsharp
builder.Services.AddOpenApi(fun options ->
    options.AddSchemaTransformer(FSharpSchemaTransformer()))
```

**Registration in .NET 10**:
Same API, but `FSharpSchemaTransformer` can use `context.Document` to add
component schemas directly (not available in .NET 9).

## R9: Core Dependency on FSharp.SystemTextJson

**Decision**: Allow `FSharp.Data.JsonSchema.Core` to depend on
`FSharp.SystemTextJson` as a runtime dependency.

**Rationale**: The `Skippable<'T>` type is defined in `FSharp.SystemTextJson`.
Without a reference, the Core analyzer would need to detect it by type name
string matching — fragile and prone to breakage across versions. Since every
consumer of this library already depends on `FSharp.SystemTextJson` for
serialization, adding it to Core does not increase the effective dependency
footprint for real-world consumers.

**Benefits**:
- Direct type reference for `Skippable<'T>` detection (no string matching)
- Access to `JsonUnionEncoding` flags for potential internal alignment
- Simplifies analyzer implementation

**Alternatives rejected**:
- Type-name-based detection (`"Skippable`1"`): Fragile, breaks if assembly
  name or namespace changes across FSharp.SystemTextJson versions.
- Zero-dependency Core: Adds implementation complexity for no practical
  benefit — all downstream consumers already have this dependency.
