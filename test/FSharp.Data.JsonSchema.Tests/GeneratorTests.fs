module FSharp.Data.JsonSchema.Tests.GeneratorTests

open FSharp.Data.JsonSchema
open Expecto

[<Tests>]
let tests =
    let generator = Generator.CreateMemoized("tag")
    let equal (actual:NJsonSchema.JsonSchema) expected message =
        let actual = NJsonSchema.JsonSchemaReferenceUtilities.ConvertPropertyReferences(actual.ToJson())
        Expect.equal (Util.stripWhitespace actual) (Util.stripWhitespace expected) message

    testList "schema generation" [
        test "Enum generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestEnum",
  "type": "string",
  "description": "",
  "x-enumNames": [
    "First",
    "Second",
    "Third"
  ],
  "enum": [
    "First",
    "Second",
    "Third"
  ]
}"""
            let actual = generator(typeof<TestEnum>)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
        }

        test "Class generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestClass",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "firstName": {
      "type": [
        "null",
        "string"
      ]
    },
    "lastName": {
      "type": [
        "null",
        "string"
      ]
    }
  }
}"""
            let actual = generator(typeof<TestClass>)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
        }

        test "Record generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestRecord",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "firstName": {
      "type": [
        "null",
        "string"
      ]
    },
    "lastName": {
      "type": [
        "null",
        "string"
      ]
    }
  }
}"""
            let actual = generator(typeof<TestRecord>)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
        }

        test "option<'a> generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "FSharpOptionOfObject",
  "type": [
    "array",
    "boolean",
    "integer",
    "null",
    "number",
    "object",
    "string"
  ],
  "additionalProperties": false
}"""
            let ty = typeof<option<_>>
            let actual = generator(ty)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
        }

        test "option<int> generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "FSharpOptionOfInteger",
  "type": [
    "integer",
    "null"
  ],
  "additionalProperties": false
}"""
            let ty = typeof<option<int>>
            let actual = generator(ty)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
        }

        test "TestSingleDU generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestSingleDU",
  "type": "string",
  "additionalProperties": false,
  "x-enumNames": [
    "Single",
    "Double",
    "Triple"
  ],
  "enum": [
    "Single",
    "Double",
    "Triple"
  ]
}"""
            let ty = typeof<TestSingleDU>
            let actual = generator(ty)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
        }

        test "Multi-case DU generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestDU",
  "definitions": {
    "Case": {
      "type": "string",
      "default": "Case",
      "x-enumNames": ["Case"],
      "enum": ["Case"]
    },
    "WithOneField": {
      "type": "object",
      "required": [
        "tag",
        "item"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "WithOneField",
          "x-enumNames": ["WithOneField"],
          "enum": ["WithOneField"]
        },
        "item": {
          "type": "integer"
        }
      }
    },
    "WithNamedFields": {
      "type": "object",
      "required": [
        "tag",
        "name",
        "value"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "WithNamedFields",
          "x-enumNames": ["WithNamedFields"],
          "enum": ["WithNamedFields"]
        },
        "name": {
          "type": "string"
        },
        "value": {
          "type": "number"
        }
      }
    }
  },
  "anyOf": [
    {
      "$ref": "#/definitions/Case"
    },
    {
      "$ref": "#/definitions/WithOneField"
    },
    {
      "$ref": "#/definitions/WithNamedFields"
    }
  ]
}"""
            let ty = typeof<TestDU>
            let actual = generator(ty)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
        }
    ]
