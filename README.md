# FSharp.Data.JsonSchema

Provides an opinionated, idiomatic JSON serializer and [JSON Schema](https://json-schema.org/) definition generation for F# types using [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson) and [NJsonSchema](https://github.com/RicoSuter/NJsonSchema).

![Build status](https://github.com/panesofglass/FSharp.Data.JsonSchema/workflows/CI/badge.svg)

[![Build History](https://buildstats.info/github/chart/panesofglass/FSharp.Data.JsonSchema?branch=master)](https://github.com/panesofglass/FSharp.Data.JsonSchema/actions?query=workflow%3ACI)

[![NuGet Badge](https://buildstats.info/nuget/fsharp.data.jsonschema)](https://www.nuget.org/packages/FSharp.Data.JsonSchema/)

## Why JSON Schema?

[JSON Schema](https://json-schema.org/) is a standard, evolving format for specifying the structure of JSON documents. JSON Schema is used in [Open API](https://www.openapis.org/) and can be used by clients to validate that the payload received or to be sent matches the expected schema. Use of these documents can allow for a more nuanced approach to versioning APIs, as well.

Tools exist for generating nicely formatted JSON from F#, e.g. [FSharpLu.Json](https://github.com/Microsoft/fsharplu), [Newtonsoft.Json.FSharp](https://github.com/haf/Newtonsoft.Json.FSharp), [Newtonsoft.Json.FSharp.Idiomatic](https://github.com/baronfel/Newtonsoft.Json.FSharp.Idiomatic), and [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson).

[Newtonsoft.Json Schema](https://www.newtonsoft.com/jsonschema) and [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) provide a way to generate and validate JSON Schema for .NET languages, but these don't necessarily translate well to F# types, e.g. `anyOf` mapping to F#'s discriminated unions. This library strives to fill this gap.

## Usage

Below is a simple example on how to deserialize a JSON string into a F# record type using a json schema to validate the structure of the data:

```fsharp
#r "nuget: FSharp.Data.JsonSchema, 2.0.2"
#r "nuget: NJsonSchema, 11.0.0"

open FSharp.Data.JsonSchema
open FSharp.Data.JsonSchema.Validation
open NJsonSchema

let personJson = """
{
    "id": 7,
    "name": "John Doe",
    "age": 22,
    "hobbies": {
        "indoor": [
            "Chess"
        ],
        "outdoor": [
            "Basketball",
            "Stand-up Comedy"
        ]
    }
}
"""

let personJsonSchema = """
{
    "$schema": "http://json-schema.org/draft-04/schema#",
    "$id": "https://example.com/employee.schema.json",
    "title": "Record of employee",
    "description": "This document records the details of an employee",
    "type": "object",
    "properties": {
        "id": {
            "description": "A unique identifier for an employee",
            "type": "number"
        },
        "name": {
            "description": "Full name of the employee",
            "type": "string"
        },
        "age": {
            "description": "Age of the employee",
            "type": "number"
        },
        "hobbies": {
            "description": "Hobbies of the employee",
            "type": "object",
            "properties": {
                "indoor": {
                    "type": "array",
                    "items": {
                        "description": "List of indoor hobbies",
                        "type": "string"
                    }
                },
                "outdoor": {
                    "type": "array",
                    "items": {
                        "description": "List of outdoor hobbies",
                        "type": "string"
                    }
                }
            }
        }
    }
}
"""

type Person = {
    Id: int
    Name: string
    Age: int
    Hobbies: {|
        Indoor: string list
        Outdoor: string list
    |}
}

let jsonSchema = JsonSchema.FromJsonAsync(personJsonSchema).Result

let result : Result<unit, Validation.ValidationError array> = Validation.validate jsonSchema personJson

let person : Result<Person, Validation.ValidationError array> = FSharp.Data.Json.DeserializeWithValidation<Person>(personJson, jsonSchema)
```
