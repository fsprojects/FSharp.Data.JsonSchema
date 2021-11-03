module FSharp.Data.JsonSchema.Tests.GeneratorTests

open FSharp.Data.JsonSchema
open Expecto

[<Tests>]
let tests =
    let generator = Generator.CreateMemoized("tag")

    let equal (actual: NJsonSchema.JsonSchema) expected message =
        let actual = actual.ToJson()
        Expect.equal (Util.stripWhitespace actual) (Util.stripWhitespace expected) message

    testList
        "schema generation"
        [ test "Enum generates proper schema" {
            let expected =
                """{
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

            let actual = generator (typeof<TestEnum>)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
          }

          test "Class generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
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
}"""

              let actual = generator (typeof<TestClass>)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
          }

          test "Record generates proper schema" {
              let expected =
                  """{
  "$schema": "http://json-schema.org/draft-04/schema#",
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
}"""

              let actual = generator (typeof<TestRecord>)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
          }

          test "option<'a> generates proper schema" {
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
  "description": "The type of optional values. When used from other CLI languages the\nempty option is the null value.",
  "additionalProperties": false
}"""

              let ty = typeof<option<_>>
              let actual = generator (ty)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
          }

          test "option<int> generates proper schema" {
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
          }

          test "TestSingleDU generates proper schema" {
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
          }

          test "Multi-case DU generates proper schema" {
              let expected =
                  """{
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
              let actual = generator (ty)
              "╰〳 ಠ 益 ಠೃ 〵╯" |> equal actual expected
          }

          test "Nested generates proper schema" {
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
          "x-enumNames": [
            "Case"
          ],
          "enum": [
            "Case"
          ]
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
              "x-enumNames": [
                "WithOneField"
              ],
              "enum": [
                "WithOneField"
              ]
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
              "type": "number"
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
          }

          test "RecWithOption generates proper schema" {
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
          }

          test "PaginatedResult<'T> generates proper schema" {
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
          } ]
