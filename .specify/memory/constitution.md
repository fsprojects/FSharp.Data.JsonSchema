<!--
Sync Impact Report
===================
Version change: 1.0.0 → 1.1.0 (principle expanded)
Modified principles:
  - II. Minimal Dependency Surface — expanded to support multi-package
    architecture. Per-package dependency rules replace single-package
    constraint. FSharp.SystemTextJson added as allowed Core dependency.
Added sections: None
Removed sections: None
Templates requiring updates:
  - .specify/templates/plan-template.md — ✅ no changes needed
  - .specify/templates/spec-template.md — ✅ no changes needed
  - .specify/templates/tasks-template.md — ✅ no changes needed
  - .specify/templates/checklist-template.md — ✅ no changes needed
  - .specify/templates/agent-file-template.md — ✅ no changes needed
Follow-up TODOs: None
-->

# FSharp.Data.JsonSchema Constitution

## Core Principles

### I. F#-Idiomatic API Design

Every public API MUST produce output faithful to F# type semantics.
Discriminated unions MUST map to JSON Schema `anyOf` constructs.
`Option<'T>` and `ValueOption<'T>` MUST be handled as nullable
schema properties, not wrapped objects. Records MUST surface all
fields as `required` in generated schemas. Enum types MUST map to
their underlying representation.

**Rationale**: The library exists specifically because generic .NET
schema generators fail to represent F# types correctly. Idiomatic
mapping is the core value proposition.

### II. Minimal Dependency Surface

Each published package MUST minimize its runtime dependencies.
Allowed per-package dependencies:

- **FSharp.Data.JsonSchema.Core**: `FSharp.Core`,
  `FSharp.SystemTextJson` (for `Skippable<'T>` support and union
  encoding alignment).
- **FSharp.Data.JsonSchema**: Core, `NJsonSchema`,
  `FSharp.SystemTextJson`.
- **FSharp.Data.JsonSchema.OpenApi**: Core, `Microsoft.OpenApi`,
  `Microsoft.AspNetCore.OpenApi`.

New runtime dependencies beyond those listed MUST NOT be added
without explicit justification and approval. Test-only
dependencies are exempt from this constraint but MUST remain in
test projects.

**Rationale**: As NuGet-distributed libraries consumed by other
projects, every added dependency increases version conflict risk
and transitive bloat for consumers. Enumerating allowed
dependencies per package makes the constraint auditable.

### III. Broad Framework Compatibility

The library MUST target `netstandard2.0`, `netstandard2.1`,
`netcoreapp3.1`, `net6.0`, and `net8.0` (or their successors as
.NET evolves). New target frameworks MAY be added. Existing
targets MUST NOT be removed without a major version bump and
documented migration guidance.

**Rationale**: Consumers span legacy and modern .NET runtimes.
Dropping a target framework is a breaking change that prevents
adoption.

### IV. Schema Stability via Snapshot Testing

All schema generation output MUST be covered by Verify-based
snapshot tests stored in the `generator-verified/` directory.
Any change to generated schema output MUST result in an updated
snapshot that is explicitly reviewed and approved. New type
mappings MUST include corresponding snapshot tests before merge.

**Rationale**: Schema output is the library's contract with
consumers. Undetected changes in generated schemas can break
downstream systems that depend on specific schema structures.

### V. Simplicity and Focus

The library MUST remain focused on JSON Schema generation and
validation for F# types. Features outside this scope (e.g., code
generation, UI rendering, HTTP client integration) MUST NOT be
added. Internal implementation MUST favor straightforward code
over abstraction—new helper modules or indirection layers MUST
be justified by concrete reuse across at least two call sites.

**Rationale**: A focused library is easier to maintain, test,
and reason about. Scope creep dilutes quality and increases the
maintenance burden on a small project.

### VI. Semantic Versioning Discipline

The package version MUST follow `MAJOR.MINOR.PATCH` semantics.
Any change to generated schema output for existing types MUST be
treated as a breaking change (MAJOR bump). New type support or
additive API surface is a MINOR bump. Bug fixes and documentation
are PATCH bumps. `RELEASE_NOTES.md` MUST be updated for every
published version.

**Rationale**: Consumers rely on version semantics to assess
upgrade risk. Schema output changes can silently break downstream
validation, so they warrant major version treatment.

## Additional Constraints

- **Build reproducibility**: `dotnet build -c Release` and
  `dotnet test` MUST succeed on a clean checkout with no external
  state beyond the .NET SDK.
- **CI parity**: All tests MUST pass on all targeted frameworks
  in GitHub Actions before merge.
- **Symbol packages**: Every NuGet release MUST include `.snupkg`
  symbol packages with Source Link enabled.
- **XML documentation**: Public API members MUST have XML doc
  comments. The build MUST generate documentation files.
- **No secret material**: Repository MUST NOT contain API keys,
  credentials, or other secrets. NuGet API keys MUST remain in
  GitHub Actions secrets only.

## Development Workflow

- **Branch strategy**: Feature work occurs on branches; merges to
  `master` via pull request.
- **Test-first encouraged**: New type mappings SHOULD have failing
  snapshot tests committed before the implementation commit.
- **CI gate**: The GitHub Actions CI workflow MUST pass (build,
  test on all targeted frameworks, pack) before a PR is merged.
- **Release process**: Tag `master` with `vMAJOR.MINOR.PATCH` to
  trigger NuGet publication via CI. The tag version MUST match the
  version in `Directory.Build.props`.
- **Commit discipline**: Each commit SHOULD represent a single
  logical change. Commit messages SHOULD follow conventional
  format (e.g., `fix:`, `feat:`, `docs:`, `test:`).

## Governance

This constitution is the authoritative source of project standards
for FSharp.Data.JsonSchema. All pull requests and code reviews
MUST verify compliance with these principles.

**Amendment procedure**: Amendments MUST be proposed as a pull
request modifying this file. The version MUST be incremented per
the semantic versioning rules below. The change MUST include an
updated Sync Impact Report (HTML comment at top of file).

**Versioning policy**:
- MAJOR: Principle removed, redefined, or made materially less
  restrictive.
- MINOR: New principle added, existing principle materially
  expanded.
- PATCH: Wording clarifications, typo fixes, non-semantic
  refinements.

**Compliance review**: At the start of each feature plan
(`/speckit.plan`), the Constitution Check section MUST verify
that the proposed work does not violate any principle. Violations
MUST be documented in the Complexity Tracking table with explicit
justification.

**Version**: 1.1.0 | **Ratified**: 2026-02-05 | **Last Amended**: 2026-02-05
