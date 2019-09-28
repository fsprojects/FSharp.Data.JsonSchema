module FSharp.JsonSchema.Tests.GeneratorTests

open FSharp.Data.JsonSchema
open Expecto

[<Tests>]
let tests =
    let generator = Generator.CreateMemoized("tag")

    testList "schema generation" [
        test "Enum generates proper schema" {
            let expected = """{
  "type": "string",
  "enum": [
    "First",
    "Second",
    "Third"
  ]
}"""
            let actual = generator(typeof<TestEnum>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Class generates proper schema" {
            let expected = """{
  "type": "object",
  "properties": {
    "firstName": {
      "type": [
        "string",
        "null"
      ]
    },
    "lastName": {
      "type": [
        "string",
        "null"
      ]
    }
  },
  "required": [
    "firstName",
    "lastName"
  ]
}"""
            let actual = generator(typeof<TestClass>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Record generates proper schema" {
            let expected = """{
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "firstName": {
      "type": [
        "string",
        "null"
      ]
    },
    "lastName": {
      "type": [
        "string",
        "null"
      ]
    }
  },
  "required": [
    "firstName",
    "lastName"
  ]
}"""
            let actual = generator(typeof<TestRecord>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "option<'a> generates proper schema" {
            let expected = """{
  "type": [
    "string",
    "number",
    "integer",
    "boolean",
    "object",
    "array",
    "null"
  ]
}"""
            let ty = typeof<option<_>>
            let schema = generator(ty)
            let actual = schema.ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "option<int> generates proper schema" {
            let expected = """{
  "type": [
    "integer",
    "null"
  ]
}"""
            let ty = typeof<option<int>>
            let schema = generator(ty)
            let actual = schema.ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "TestSingleDU generates proper schema" {
            let expected = """{
  "type": "string",
  "enum": [
    "Single",
    "Double",
    "Triple"
  ]
}"""
            let ty = typeof<TestSingleDU>
            let schema = generator(ty)
            let actual = schema.ToString()
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
            let actual = schema.ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }
    ]
