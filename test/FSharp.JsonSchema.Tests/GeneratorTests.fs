module FSharp.JsonSchema.Tests.GeneratorTests

open Newtonsoft.Json.Schema.Generation
open Expecto

[<Tests>]
let tests =
    testList "generator" [
        test "Enum generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let expected = """{
  "type": "string",
  "enum": [
    "First",
    "Second",
    "Third"
  ]
}"""
            let actual = generator.Generate(typeof<TestEnum>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Class generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
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
            let actual = generator.Generate(typeof<TestClass>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Record generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
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
            let actual = generator.Generate(typeof<TestRecord>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "option<'a> generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let expected = """{
  "type": "object",
  "anyOf": [
    {
      "type": "null"
    },
    {
      "type": [
        "string",
        "number",
        "integer",
        "boolean",
        "object",
        "array"
      ]
    }
  ]
}"""
            let ty = typeof<option<_>>
            let schema = generator.Generate(ty)
            let actual = schema.ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "option<int> generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let expected = """{
  "type": "object",
  "anyOf": [
    {
      "type": "null"
    },
    {
      "type": "integer"
    }
  ]
}"""
            let ty = typeof<option<int>>
            let schema = generator.Generate(ty)
            let actual = schema.ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "TestSingleDU is a union type" {
            let expected = true
            let actual = FSharp.Reflection.FSharpType.IsUnion(typeof<TestSingleDU>)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "TestSingleDU generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let expected = """{
  "type": "string",
  "enum": [
    "Single",
    "Double",
    "Triple"
  ]
}"""
            let ty = typeof<TestSingleDU>
            let schema = generator.Generate(ty)
            let actual = schema.ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "TestDU is a union type" {
            let expected = true
            let actual = FSharp.Reflection.FSharpType.IsUnion(typeof<TestDU>)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Multi-case DU generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
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
            let schema = generator.Generate(ty)
            let actual = schema.ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }
    ]
