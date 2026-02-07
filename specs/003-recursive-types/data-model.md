# Data Model: Recursive Type Schema Generation

**Feature**: 003-recursive-types
**Date**: 2026-02-06

## Entities

### SchemaNode (Existing - No Changes)

The core intermediate representation for JSON Schema nodes. Already includes the `Ref` variant that handles recursive references.

| Variant | Description | Recursive Relevance |
|---------|-------------|-------------------|
| `Ref of typeId: string` | Reference to another schema definition | `"#"` = self-reference to root; other strings = definition reference |
| `AnyOf of SchemaNode list` | Union of schemas (DU representation) | Root DU produces AnyOf of Refs to case definitions |
| `Object` | Object with properties | DU cases and records produce Object nodes |
| `Array of SchemaNode` | Array with item schema | Items can be `Ref` for recursive collections |
| `Nullable of SchemaNode` | Nullable wrapper | Inner schema can be `Ref` for recursive option fields |
| Other variants | Primitive, Enum, Map, Const, OneOf, Any | Not directly involved in recursion |

### SchemaDocument (Existing - No Changes)

```
SchemaDocument = {
    Root: SchemaNode              -- Top-level schema (AnyOf for DUs, Object for records)
    Definitions: (string * SchemaNode) list  -- Named definitions in insertion order
}
```

### Recursion Detection State (Internal - No Changes)

| State | Type | Purpose |
|-------|------|---------|
| `visiting` | `HashSet<Type>` | Tracks types currently being analyzed (cycle detection) |
| `analyzed` | `Dictionary<Type, string>` | Caches completed type-to-typeId mappings |
| `definitions` | `Dictionary<string, SchemaNode>` | Accumulates definitions in insertion order |

## Recursive Type Patterns

### Pattern 1: Self-Recursive DU (Issue #15)

**F# Type**:
```fsharp
type TreeNode =
    | Leaf of int
    | Branch of TreeNode * TreeNode
```

**Expected SchemaDocument**:
```
Root = AnyOf [Ref "Leaf"; Ref "Branch"]
Definitions = [
    ("Leaf", Object { Properties = [kind="Leaf" (Const); Item (Primitive Int)] })
    ("Branch", Object { Properties = [kind="Branch" (Const); Item1 (Ref "#"); Item2 (Ref "#")] })
]
```

### Pattern 2: Self-Recursive Record

**F# Type**:
```fsharp
type LinkedNode = { Value: int; Next: LinkedNode option }
```

**Expected SchemaDocument**:
```
Root = Object { Properties = [value (Primitive Int); next (Nullable (Ref "#"))] }
Definitions = []
```

### Pattern 3: Recursion Through Collection

**F# Type**:
```fsharp
type TreeRecord = { Value: string; Children: TreeRecord list }
```

**Expected SchemaDocument**:
```
Root = Object { Properties = [value (Primitive String); children (Array (Ref "#"))] }
Definitions = []
```

### Pattern 4: Multi-Case Self-Recursive DU

**F# Type**:
```fsharp
type Expression =
    | Literal of int
    | Add of Expression * Expression
    | Negate of Expression
```

**Expected SchemaDocument**:
```
Root = AnyOf [Ref "Literal"; Ref "Add"; Ref "Negate"]
Definitions = [
    ("Literal", Object { Properties = [kind="Literal" (Const); Item (Primitive Int)] })
    ("Add", Object { Properties = [kind="Add" (Const); Item1 (Ref "#"); Item2 (Ref "#")] })
    ("Negate", Object { Properties = [kind="Negate" (Const); Item (Ref "#")] })
]
```

## Relationships

```
SchemaAnalyzer.analyze
    ├── analyzeType → analyzeMultiCaseDU (for DU types)
    │   ├── buildCaseSchema → analyzeDuCaseFieldSchema
    │   │   └── getOrAnalyzeRef → detects visiting set → Ref "#" or Ref typeId
    │   └── definitions accumulate case schemas
    ├── analyzeType → analyzeRecord (for record types)
    │   └── analyzeFieldSchema → getOrAnalyzeRef → Ref "#" or Ref typeId
    └── Returns SchemaDocument { Root; Definitions }
```

## State Transitions

No state transitions apply - schema generation is a pure analysis pass that reads F# type metadata and produces an immutable SchemaDocument.
