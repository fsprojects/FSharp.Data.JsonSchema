# FSharp.Data.JsonSchema

Provides an opinionated, idiomatic JSON serializer and [JSON Schema](https://json-schema.org/) definition generation for F# types using [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson) and [NJsonSchema](https://github.com/RicoSuter/NJsonSchema).

![Build status](https://github.com/panesofglass/FSharp.Data.JsonSchema/workflows/CI/badge.svg)

[![Build History](https://buildstats.info/github/chart/panesofglass/FSharp.Data.JsonSchema?branch=master)](https://github.com/panesofglass/FSharp.Data.JsonSchema/actions?query=workflow%3ACI)

[![NuGet Badge](https://buildstats.info/nuget/fsharp.data.jsonschema)](https://www.nuget.org/packages/FSharp.Data.JsonSchema/)

## Why JSON Schema?

[JSON Schema](https://json-schema.org/) is a standard, evolving format for specifying the structure of JSON documents. JSON Schema is used in [Open API](https://www.openapis.org/) and can be used by clients to validate that the payload received or to be sent matches the expected schema. Use of these documents can allow for a more nuanced approach to versioning APIs, as well.

Tools exist for generating nicely formatted JSON from F#, e.g. [FSharpLu.Json](https://github.com/Microsoft/fsharplu), [Newtonsoft.Json.FSharp](https://github.com/haf/Newtonsoft.Json.FSharp), [Newtonsoft.Json.FSharp.Idiomatic](https://github.com/baronfel/Newtonsoft.Json.FSharp.Idiomatic), and [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson).

[Newtonsoft.Json Schema](https://www.newtonsoft.com/jsonschema) and [NJsonSchema](https://github.com/RicoSuter/NJsonSchema) provide a way to generate and validate JSON Schema for .NET languages, but these don't necessarily translate well to F# types, e.g. `anyOf` mapping to F#'s discriminated unions. This library strives to fill this gap.

