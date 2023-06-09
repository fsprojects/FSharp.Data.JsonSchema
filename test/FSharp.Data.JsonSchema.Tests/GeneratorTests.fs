module FSharp.Data.JsonSchema.Tests.GeneratorTests

open FSharp.Data.JsonSchema
open Expecto
open VerifyTests
open VerifyExpecto

// do VerifyDiffPlex.Initialize()
do ClipboardAccept.Enable()

let verifySettings =
    let s = VerifySettings()
    s.UseDirectory("generator-verified")
    s

type VerifyBuilder(name,focusState) =
    inherit TestCaseBuilder(name,focusState)
    let makeValidFilePath (input: string) : string =
        let invalidChars = System.IO.Path.GetInvalidFileNameChars() |> Array.append  [| '\''; '"'; '<'; '>'; '|'; '?'; '*'; ':'; '\\'|]

        let replaceChar = '_'
        input.Trim()
        |> Seq.map(fun c -> if Array.contains c invalidChars then replaceChar else c )
        |> System.String.Concat

    member __.Return<'T>(v:'T) = Verifier.Verify(makeValidFilePath name, v,settings= verifySettings).Wait()

let verify name = VerifyBuilder(name,FocusState.Normal)

let json ( schema: NJsonSchema.JsonSchema ) = schema.ToJson()

[<Tests>]
let tests =
    let generator = Generator.CreateMemoized("tag")

    let equal (actual: NJsonSchema.JsonSchema) expected message =
        let actual = actual.ToJson()
        Expect.equal (Util.stripWhitespace actual) (Util.stripWhitespace expected) message

    testList
        "schema generation"
        [ verify "Enum generates proper schema" {
            return generator (typeof<TestEnum>) |> json
          }

          verify "Class generates proper schema" {
              return generator (typeof<TestClass>) |> json
          }

          verify "Record generates proper schema" {
               return generator (typeof<TestRecord>) |> json
          }

          verify "option<'a> generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "Any",
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
              let actual = generator (ty)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
              return json actual
          }

          verify "option<int> generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "Integer",
  "type": [
    "integer",
    "null"
  ],
  "format": "int32"
}"""

              let ty = typeof<option<int>>
              let actual = generator (ty)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
              return json actual
          }

          verify "TestSingleDU generates proper schema" {
              let expected =
                  """{
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
              let actual = generator (ty)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
              return json actual
          }

          verify "Multi-case DU generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestDU",
  "definitions": {
    "Case": {
      "type": "string",
      "default": "Case",
      "additionalProperties": false,
      "x-enumNames": ["Case"],
      "enum": ["Case"]
    },
    "WithOneField": {
      "type": "object",
      "additionalProperties": false,
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
          "type": "integer",
          "format": "int32"
        }
      }
    },
    "WithNamedFields": {
      "type": "object",
      "additionalProperties": false,
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
          "type": "number",
          "format": "double"
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
              let actual = generator (ty)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
              return json actual
          }

          verify "Nested generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "Nested",
  "definitions": {
    "TestRecord": {
      "title": "TestRecord",
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "firstName": {
          "type": "string"
        },
        "lastName": {
          "type": "string"
        }
      }
    },
    "Rec": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "tag",
        "item"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "Rec",
          "x-enumNames": [
            "Rec"
          ],
          "enum": [
            "Rec"
          ]
        },
        "item": {
          "$ref": "#/definitions/TestRecord"
        }
      }
    },
    "TestDU": {
      "title": "TestDU",
      "definitions": {
        "Case": {
          "type": "string",
          "default": "Case",
          "additionalProperties": false,
          "x-enumNames": [
            "Case"
          ],
          "enum": [
            "Case"
          ]
        },
        "WithOneField": {
          "type": "object",
          "additionalProperties": false,
          "required": [
            "tag",
            "item"
          ],
          "properties": {
            "tag": {
              "type": "string",
              "default": "WithOneField",
              "x-enumNames": [
                "WithOneField"
              ],
              "enum": [
                "WithOneField"
              ]
            },
            "item": {
              "type": "integer",
              "format": "int32"
            }
          }
        },
        "WithNamedFields": {
          "type": "object",
          "additionalProperties": false,
          "required": [
            "tag",
            "name",
            "value"
          ],
          "properties": {
            "tag": {
              "type": "string",
              "default": "WithNamedFields",
              "x-enumNames": [
                "WithNamedFields"
              ],
              "enum": [
                "WithNamedFields"
              ]
            },
            "name": {
              "type": "string"
            },
            "value": {
              "type": "number",
              "format": "double"
            }
          }
        }
      },
      "anyOf": [
        {
          "$ref": "#/definitions/TestDU/definitions/Case"
        },
        {
          "$ref": "#/definitions/TestDU/definitions/WithOneField"
        },
        {
          "$ref": "#/definitions/TestDU/definitions/WithNamedFields"
        }
      ]
    },
    "Du": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "tag",
        "item"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "Du",
          "x-enumNames": [
            "Du"
          ],
          "enum": [
            "Du"
          ]
        },
        "item": {
          "$ref": "#/definitions/TestDU"
        }
      }
    },
    "TestSingleDU": {
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
    },
    "SingleDu": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "tag",
        "item"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "SingleDu",
          "x-enumNames": [
            "SingleDu"
          ],
          "enum": [
            "SingleDu"
          ]
        },
        "item": {
          "$ref": "#/definitions/TestSingleDU"
        }
      }
    },
    "TestEnum": {
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
    },
    "Enum": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "tag",
        "item"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "Enum",
          "x-enumNames": [
            "Enum"
          ],
          "enum": [
            "Enum"
          ]
        },
        "item": {
          "$ref": "#/definitions/TestEnum"
        }
      }
    },
    "TestClass": {
      "title": "TestClass",
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "firstName": {
          "type": "string"
        },
        "lastName": {
          "type": "string"
        }
      }
    },
    "Class": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "tag",
        "item"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "Class",
          "x-enumNames": [
            "Class"
          ],
          "enum": [
            "Class"
          ]
        },
        "item": {
          "$ref": "#/definitions/TestClass"
        }
      }
    },
    "Opt": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "tag"
      ],
      "properties": {
        "tag": {
          "type": "string",
          "default": "Opt",
          "x-enumNames": [
            "Opt"
          ],
          "enum": [
            "Opt"
          ]
        },
        "item": {
          "$ref": "#/definitions/TestRecord"
        }
      }
    }
  },
  "anyOf": [
    {
      "$ref": "#/definitions/Rec"
    },
    {
      "$ref": "#/definitions/Du"
    },
    {
      "$ref": "#/definitions/SingleDu"
    },
    {
      "$ref": "#/definitions/Enum"
    },
    {
      "$ref": "#/definitions/Class"
    },
    {
      "$ref": "#/definitions/Opt"
    }
  ]
}"""

              let actual = generator (typeof<Nested>)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
              return json actual
          }

          verify "RecWithOption generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "RecWithOption",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "name": {
      "type": "string"
    },
    "description": {
      "type": [
        "null",
        "string"
      ]
    }
  }
}"""

              let actual = generator (typeof<RecWithOption>)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
              return json actual
          }

          verify "PaginatedResult<'T> generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "PaginatedResultOfObject",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "page": {
      "type": "integer",
      "format":"int32"
    },
    "perPage": {
      "type": "integer",
      "format": "int32"
    },
    "total": {
      "type": "integer",
      "format": "int32"
    },
    "results": {
      "type": "array",
      "items": {}
    }
  }
}"""

              let actual = generator (typeof<PaginatedResult<_>>)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
              return json actual
          }
          
          verify "FSharp list generates proper schema" {
              let expected = """
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestList",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "id": {
      "type": "integer",
      "format": "int32"
    },
    "name": {
      "type": "string"
    },
    "records": {
      "type": "array",
      "items": {
        "$ref": "#/definitions/TestRecord"
      }
    }
  },
  "definitions": {
    "TestRecord": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "firstName": {
          "type": "string"
        },
        "lastName": {
          "type": "string"
        }
      }
    }
  }
}
"""
              let actual = generator typeof<TestList>
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected 
              return json actual
          }
          verify "FSharp decimal generates correct schema" {
            let expected = """
{
  "$schema": "http://json-schema.org/draft-04/schema#",
  "title": "TestDecimal",
  "type": "object",
  "additionalProperties": false,
  "properties": {
    "test": {
      "$ref": "#/definitions/DuWithDecimal"
    },
    "total": {
      "type": "number",
      "format": "decimal"
    }
  },
  "definitions": {
    "DuWithDecimal": {
      "definitions": {
        "Nothing": {
          "type": "string",
          "default": "Nothing",
          "additionalProperties": false,
          "x-enumNames": [
            "Nothing"
          ],
          "enum": [
            "Nothing"
          ]
        },
        "Amount": {
          "type": "object",
          "additionalProperties": false,
          "required": [
            "tag",
            "item"
          ],
          "properties": {
            "tag": {
              "type": "string",
              "default": "Amount",
              "x-enumNames": [
                "Amount"
              ],
              "enum": [
                "Amount"
              ]
            },
            "item": {
              "type": "number",
              "format": "decimal"
            }
          }
        }
      },
      "anyOf": [
        {
          "$ref": "#/definitions/DuWithDecimal/definitions/Nothing"
        },
        {
          "$ref": "#/definitions/DuWithDecimal/definitions/Amount"
        }
      ]
    }
  }
}
"""
            let actual = generator typeof<TestDecimal>
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
            return json actual
          } ]
