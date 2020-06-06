# FSharp.Data.JsonSchema

![Build status](https://github.com/panesofglass/FSharp.Data.JsonSchema/workflows/CI/badge.svg)

[![Build History](https://buildstats.info/github/chart/panesofglass/FSharp.Data.JsonSchema?branch=master)](https://github.com/panesofglass/FSharp.Data.JsonSchema/actions?query=workflow%3ACI)

[![NuGet Badge](https://buildstats.info/nuget/fsharp.data.jsonschema)](https://www.nuget.org/packages/FSharp.Data.JsonSchema/)

The goal of this project is to provide generation of idiomatic [JSON Schema](https://json-schema.org/) definitions for F# types.

## Why JSON Schema?

[JSON Schema](https://json-schema.org/) is a standard, evolving format for specifying the structure of JSON documents.
Tools exist for generating nicely formatted JSON from F#, e.g. [FSharpLu.Json](https://github.com/Microsoft/fsharplu), [Newtonsoft.Json.FSharp](https://github.com/haf/Newtonsoft.Json.FSharp), and [Newtonsoft.Json.FSharp.Idiomatic](https://github.com/baronfel/Newtonsoft.Json.FSharp.Idiomatic).
[Newtonsoft.Json Schema](https://www.newtonsoft.com/jsonschema) and [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) provide a way to generate and validate JSON Schema for .NET languages, but these don't necessarily translate well to F# types, e.g. `anyOf` mapping to F#'s discriminated unions.

## Which JSON Schema?

.NET offers several possibile targets:

- [Newtonsoft.Json Schema](https://www.newtonsoft.com/jsonschema) - complete schema generation and validation
- [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) - OSS JsonSchema generator and validator, used in [NSwag](https://github.com/RicoSuter/NSwag).
- [OpenAPI.NET](https://github.com/Microsoft/OpenAPI.NET) - part of the Open API toolkit for defining Open API's version of JSON Schema but no built-in validation

## Approach

This library takes the approach of using F# Reflection to generate the JSON Schema matching intended JSON output.
For the initial implementation, FSharp.Data.JsonSchema uses [Newtonsoft.Json.FSharp.Idiomatic](https://github.com/baronfel/Newtonsoft.Json.FSharp.Idiomatic) as its target JSON format.
For the greatest freedom in generating and validating JSON Schema, this library uses [NJsonSchema](https://github.com/RicoSuter/NJsonSchema).
A later goal is to allow the schema generation to be driven by additional options.
