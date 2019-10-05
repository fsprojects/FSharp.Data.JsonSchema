module FSharp.JsonSchema.Tests.GeneratorTests

open FSharp.Data.JsonSchema
open Expecto

[<Tests>]
let tests =
    let generator = Generator.CreateMemoized("tag")

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
  ]
  "enum": [
    "First",
    "Second",
    "Third"
  ]
}"""
            let actual = generator(typeof<TestEnum>).ToJson()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Class generates proper schema" {
            let expected = """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestRecord",
  "type": "object",
  "additionalProperties": false
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
            let actual = generator(typeof<TestClass>).ToJson()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
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
            let actual = generator(typeof<TestRecord>).ToJson()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
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
            let schema = generator(ty)
            let actual = schema.ToJson()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
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
            let schema = generator(ty)
            let actual = schema.ToJson()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
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
            let schema = generator(ty)
            let actual = schema.ToJson()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Multi-case DU generates proper schema" {
            let expected = """{
  "type": "object",
  "anyOf": [
    {
      "type": "object",
      "properties": {
        "tag": {
          "type": "string"
        }
      }
    },
    {
      "type": "object",
      "properties": {
        "tag": {
          "type": "string"
        },
        "Item": {
          "type": "integer"
        }
      }
    },
    {
      "type": "object",
      "properties": {
        "tag": {
          "type": "string"
        },
        "name": {
          "type": "string"
        },
        "value": {
          "type": "number"
        }
      }
    }
  ]
}"""
            let ty = typeof<TestDU>
            let schema = generator(ty)
            let actual = schema.ToJson()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }
    ]
