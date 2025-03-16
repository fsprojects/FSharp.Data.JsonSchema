module FSharp.Data.JsonSchema.Tests.Bug10

open FSharp.Data
open FSharp.Data.JsonSchema
open Expecto

type inner = { inner1: int; inner2: string }

type outer = { outer1: inner option }

type outerReq = { outer1: inner }

type Apple = { Seeds: int; Bitten: bool }

type Food =
    | Apple of Apple
    | MaybeApple of Apple option

type Misc = { Amount: int; Name: string }

type Basket = { Misc: Misc option; Food: Food }

[<Tests>]
let tests =
    let equal (actual: NJsonSchema.JsonSchema) expected message =
        let actual = actual.ToJson()
        Expect.equal (Util.stripWhitespace actual) (Util.stripWhitespace expected) message

    testList
        "Bug report: Option types do not generate correct schema"
        [ test "reproduce inner / outer no option" {
            let expected =
                """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "outerReq",
  "type": "object",
  "additionalProperties": false,
  "required":["outer1"],
  "properties": {
    "outer1": {
      "$ref": "#/definitions/Inner"
    }
  },
  "definitions": {
    "Inner": {
      "type": "object",
      "additionalProperties": false,
      "required":["inner1","inner2"],
      "properties": {
        "inner1": {
          "type": "integer",
          "format": "int32"
        },
        "inner2": {
          "type": "string"
        }
      }
    }
  }
}"""

            let gen = Generator.CreateMemoized("out")
            let actual = gen (typeof<outerReq>)
            equal actual expected "Expected detailed type definition in definitions."
          }

          test "reproduce inner / outer option bug" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "outer",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "outer1": {
      "oneOf": [
        { "type": "null" },
        { "$ref": "#/definitions/Inner" }
      ]
    }
  },
  "definitions": {
    "Inner": {
      "type": "object",
      "additionalProperties": false,
      "required":["inner1","inner2"],
      "properties": {
        "inner1": {
          "type": "integer",
          "format": "int32"
        },
        "inner2": {
          "type": "string"
        }
      }
    }
  }
}"""

              let gen = Generator.CreateMemoized("out")
              let actual = gen (typeof<outer>)
              equal actual expected "Expected detailed type definition in definitions."
          } ]
