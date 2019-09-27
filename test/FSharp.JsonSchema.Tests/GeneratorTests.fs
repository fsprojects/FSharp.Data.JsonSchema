module GeneratorTests

open Newtonsoft.Json.Schema.Generation
open Expecto

type TestClass() =
    member val FirstName = "" with get, set
    member val LastName = "" with get, set

type TestRecord =
    { FirstName : string
      LastName : string }

type TestDU =
    | Case
    | WithOneField of int
    | WithNamedFields of name:string * value:float

[<Tests>]
let tests =
    testList "generator" [
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
            let actual = generator.Generate(typeof<option<_>>).ToString()
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
            let actual = generator.Generate(typeof<option<int>>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }

        test "Multi-case DU generates proper schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let expected = """{ }"""
            let actual = generator.Generate(typeof<TestDU>).ToString()
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual expected
        }
    ]
