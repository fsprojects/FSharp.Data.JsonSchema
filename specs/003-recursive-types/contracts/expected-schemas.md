# Expected Schema Contracts: Recursive Types

**Feature**: 003-recursive-types
**Date**: 2026-02-06

## Contract 1: Self-Recursive DU (TreeNode)

**Input Type**:
```fsharp
type TreeNode =
    | Leaf of int
    | Branch of TreeNode * TreeNode
```

**Expected JSON Schema** (NJsonSchema output, InternalTag encoding):
```json
{
  "title": "TreeNode",
  "definitions": {
    "Leaf": {
      "type": "object",
      "additionalProperties": false,
      "required": ["kind", "Item"],
      "properties": {
        "kind": {
          "type": "string",
          "default": "Leaf",
          "enum": ["Leaf"],
          "x-enumNames": ["Leaf"]
        },
        "Item": {
          "type": "integer",
          "format": "int32"
        }
      }
    },
    "Branch": {
      "type": "object",
      "additionalProperties": false,
      "required": ["kind", "Item1", "Item2"],
      "properties": {
        "kind": {
          "type": "string",
          "default": "Branch",
          "enum": ["Branch"],
          "x-enumNames": ["Branch"]
        },
        "Item1": {
          "$ref": "#"
        },
        "Item2": {
          "$ref": "#"
        }
      }
    }
  },
  "anyOf": [
    { "$ref": "#/definitions/Leaf" },
    { "$ref": "#/definitions/Branch" }
  ]
}
```

**Key assertions**:
- `Item1` and `Item2` in Branch case both use `$ref: "#"` (root self-reference)
- No infinite nesting or expansion

## Contract 2: Self-Recursive Record (LinkedNode)

**Input Type**:
```fsharp
type LinkedNode = { Value: int; Next: LinkedNode option }
```

**Expected JSON Schema**:
```json
{
  "title": "LinkedNode",
  "type": "object",
  "additionalProperties": false,
  "required": ["value"],
  "properties": {
    "value": {
      "type": "integer",
      "format": "int32"
    },
    "next": {
      "anyOf": [
        { "$ref": "#" },
        { "type": "null" }
      ]
    }
  }
}
```

**Key assertions**:
- `next` field uses `$ref: "#"` wrapped in anyOf with null (nullable self-reference)
- No definitions needed (record is the root, self-reference uses "#")

## Contract 3: Recursion Through Collection (TreeRecord)

**Input Type**:
```fsharp
type TreeRecord = { Value: string; Children: TreeRecord list }
```

**Expected JSON Schema**:
```json
{
  "title": "TreeRecord",
  "type": "object",
  "additionalProperties": false,
  "required": ["value", "children"],
  "properties": {
    "value": {
      "type": "string"
    },
    "children": {
      "type": "array",
      "items": {
        "$ref": "#"
      }
    }
  }
}
```

**Key assertions**:
- `children` array items use `$ref: "#"` (root self-reference)
- No definitions needed

## Contract 4: Multi-Case Self-Recursive DU (Expression)

**Input Type**:
```fsharp
type Expression =
    | Literal of int
    | Add of Expression * Expression
    | Negate of Expression
```

**Expected JSON Schema**:
```json
{
  "title": "Expression",
  "definitions": {
    "Literal": {
      "type": "object",
      "additionalProperties": false,
      "required": ["kind", "Item"],
      "properties": {
        "kind": {
          "type": "string",
          "default": "Literal",
          "enum": ["Literal"],
          "x-enumNames": ["Literal"]
        },
        "Item": {
          "type": "integer",
          "format": "int32"
        }
      }
    },
    "Add": {
      "type": "object",
      "additionalProperties": false,
      "required": ["kind", "Item1", "Item2"],
      "properties": {
        "kind": {
          "type": "string",
          "default": "Add",
          "enum": ["Add"],
          "x-enumNames": ["Add"]
        },
        "Item1": {
          "$ref": "#"
        },
        "Item2": {
          "$ref": "#"
        }
      }
    },
    "Negate": {
      "type": "object",
      "additionalProperties": false,
      "required": ["kind", "Item"],
      "properties": {
        "kind": {
          "type": "string",
          "default": "Negate",
          "enum": ["Negate"],
          "x-enumNames": ["Negate"]
        },
        "Item": {
          "$ref": "#"
        }
      }
    }
  },
  "anyOf": [
    { "$ref": "#/definitions/Literal" },
    { "$ref": "#/definitions/Add" },
    { "$ref": "#/definitions/Negate" }
  ]
}
```

**Key assertions**:
- All recursive case fields (Add.Item1, Add.Item2, Negate.Item) use `$ref: "#"`
- Non-recursive case (Literal) has normal integer field

---

**Note**: These schemas are approximate. The actual output will be determined by running the tests and capturing verified snapshots. Minor formatting differences (property ordering, whitespace) may vary.
