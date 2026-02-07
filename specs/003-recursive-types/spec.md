# Feature Specification: Recursive Type Schema Generation

**Feature Branch**: `003-recursive-types`
**Created**: 2026-02-06
**Status**: Draft
**Input**: User description: "Resolve GitHub issue #15: how to add a schema generator for a recursive schema like `type Node = | Leaf of int | Node of Node * Node`"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Self-Recursive Discriminated Union (Priority: P1)

A library consumer defines a self-recursive discriminated union type (e.g., a tree node type where one case contains the same type) and generates a JSON Schema from it. The schema generation completes without infinite loops and produces a valid JSON Schema that uses `$ref` to represent the recursive reference.

**Why this priority**: This is the exact scenario described in GitHub issue #15. Self-recursive DU types are common in F# domain modeling (expression trees, ASTs, nested data structures). Generating schemas for these types without hanging is the core ask.

**Independent Test**: Can be fully tested by defining a self-recursive DU type, invoking the schema generator, and verifying that the output is a valid JSON Schema with correct `$ref` references and no infinite recursion.

**Acceptance Scenarios**:

1. **Given** a self-recursive DU type `type Node = | Leaf of int | Node of Node * Node`, **When** the schema generator analyzes this type, **Then** the generation completes without hanging and produces a valid JSON Schema document.
2. **Given** a self-recursive DU type, **When** the generated schema is examined, **Then** the recursive case uses `$ref` to reference the root type rather than embedding an infinite expansion.
3. **Given** a self-recursive DU type, **When** the generated schema is used to validate a valid JSON instance (e.g., `{"Node": {"Item1": {"Leaf": {"Item": 1}}, "Item2": {"Leaf": {"Item": 2}}}}`), **Then** the instance validates successfully.

---

### User Story 2 - Self-Recursive Record Type (Priority: P2)

A library consumer defines a record type that references itself (e.g., a linked list node with a field of its own type wrapped in option) and generates a JSON Schema. The schema generation produces correct `$ref` references for the recursive field.

**Why this priority**: Self-recursive records are another common F# pattern alongside DUs. Supporting this ensures recursive type handling works across F# type constructs, not just DUs.

**Independent Test**: Can be fully tested by defining a self-recursive record type, generating a schema, and verifying `$ref` references appear for the recursive fields.

**Acceptance Scenarios**:

1. **Given** a self-recursive record type `type LinkedNode = { Value: int; Next: LinkedNode option }`, **When** the schema generator analyzes this type, **Then** the generation completes and produces a valid JSON Schema with `$ref` for the `Next` field.
2. **Given** the generated schema, **When** validating a nested JSON instance, **Then** the schema correctly validates instances at arbitrary depth.

---

### User Story 3 - Deeply Nested Recursive Structures (Priority: P3)

A library consumer defines types with deep or multi-level recursion (e.g., mutually recursive types, or a type that references itself through an intermediate type) and generates schemas. The schema generation handles all recursion patterns consistently, producing `$ref` references wherever recursion occurs.

**Why this priority**: While basic self-recursion is the most common case, real-world domain models sometimes involve indirect recursion or recursion through intermediate types (e.g., `Tree` -> `Branch` -> `Tree`). Ensuring all recursion patterns are handled increases library robustness.

**Independent Test**: Can be fully tested by defining mutually recursive types and types with indirect recursion, generating schemas, and verifying all recursive references resolve correctly.

**Acceptance Scenarios**:

1. **Given** mutually recursive types (e.g., `type A = | HasB of B and type B = | HasA of A`), **When** the schema generator analyzes either type, **Then** the schema contains definitions for both types with `$ref` cross-references.
2. **Given** a type with recursion through a collection (e.g., `type Tree = { Children: Tree list }`), **When** the schema generator analyzes this type, **Then** the array items schema uses `$ref` to reference the parent type.

---

### Edge Cases

- What happens when a recursive type references itself through multiple case fields (e.g., `Node of Node * Node` with two self-references in the same case)?
- How does the system handle recursion through option types (e.g., `type T = { Child: T option }`)?
- How does the system handle recursion through collections (e.g., `type T = { Children: T list }`)?
- What happens with deeply nested instantiation during validation (e.g., 100 levels deep)?
- How does the system handle a DU case where the recursive reference is one of several fields of different types?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The schema generator MUST produce a valid JSON Schema document for any self-recursive discriminated union type without entering an infinite loop.
- **FR-002**: The schema generator MUST represent recursive type references using JSON Schema `$ref` notation pointing to the appropriate definition.
- **FR-003**: The schema generator MUST produce a valid JSON Schema document for self-recursive record types without entering an infinite loop.
- **FR-004**: The schema generator MUST handle recursion that occurs through collection types (e.g., a type containing a list of itself).
- **FR-005**: The schema generator MUST handle recursion that occurs through option-wrapped fields.
- **FR-006**: The generated schema MUST validate correct JSON instances of recursive types at arbitrary nesting depth.
- **FR-007**: The schema generation MUST work consistently across all output targets (Core IR, NJsonSchema, OpenApi).

### Key Entities

- **Self-Recursive Type**: A type whose definition references itself directly in one or more of its fields or cases. Examples include tree structures, expression ASTs, and linked lists.
- **Recursive Reference**: A `$ref` entry in the generated JSON Schema that points back to the type's own definition or to a mutually referenced definition, preventing infinite schema expansion.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Schema generation for all recursive type patterns (self-recursive DU, self-recursive record, recursion through collections, recursion through options) completes without hanging.
- **SC-002**: 100% of generated schemas for recursive types pass JSON Schema validation (the schema itself is a valid JSON Schema document).
- **SC-003**: Valid JSON instances of recursive types at various nesting depths (1, 3, 10 levels) validate successfully against the generated schema.
- **SC-004**: All recursive type test cases produce deterministic, snapshot-verified output across Core IR, NJsonSchema, and OpenApi translators.

## Assumptions

- The F# `System.Text.Json` serialization conventions (via `FSharp.SystemTextJson`) determine the JSON structure of recursive types, and the schema should match those conventions.
- The existing `visiting` set / `analyzed` cache mechanism in the SchemaAnalyzer is the correct foundation for recursion detection; this feature validates and extends test coverage for that mechanism rather than replacing it.
- Mutual recursion between two distinct types (e.g., Chicken/Egg) is already tested and working; this feature focuses primarily on self-referencing types as reported in issue #15.
