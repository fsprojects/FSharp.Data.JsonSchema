# FSharp.Data.JsonSchema

Provides an opinionated, idiomatic [JSON Schema](https://json-schema.org/) definition generation for F# types.

| Package | NuGet | Status |
|---------|-------|--------|
| FSharp.Data.JsonSchema.Core | [![NuGet](http://img.shields.io/nuget/v/FSharp.Data.JsonSchema.Core.svg?style=flat)](https://www.nuget.org/packages/FSharp.Data.JsonSchema.Core/) | ✅ Active |
| FSharp.Data.JsonSchema.NJsonSchema | [![NuGet](http://img.shields.io/nuget/v/FSharp.Data.JsonSchema.NJsonSchema.svg?style=flat)](https://www.nuget.org/packages/FSharp.Data.JsonSchema.NJsonSchema/) | ✅ Active |
| FSharp.Data.JsonSchema.OpenApi | [![NuGet](http://img.shields.io/nuget/v/FSharp.Data.JsonSchema.OpenApi.svg?style=flat)](https://www.nuget.org/packages/FSharp.Data.JsonSchema.OpenApi/) | ✅ Active |
| FSharp.Data.JsonSchema | [![NuGet](http://img.shields.io/nuget/v/FSharp.Data.JsonSchema.svg?style=flat)](https://www.nuget.org/packages/FSharp.Data.JsonSchema/) | ⚠️ Deprecated (use NJsonSchema) |

## Packages

### FSharp.Data.JsonSchema.Core

Target-agnostic JSON Schema IR (intermediate representation) and F# type analyzer. Use this package when you want to analyze F# types into a schema document and translate to any target format.

- `SchemaAnalyzer.analyze` recursively analyzes F# types into a `SchemaDocument`
- `SchemaNode` discriminated union represents JSON Schema concepts (Object, Array, AnyOf, OneOf, Nullable, Primitive, Enum, Ref, Map, Const, Any)
- `SchemaGeneratorConfig` controls discriminator name, naming policy, and additional properties
- **No NJsonSchema or OpenAPI dependency** — only FSharp.Core and FSharp.SystemTextJson
- Targets netstandard2.0 through net10.0

### FSharp.Data.JsonSchema.NJsonSchema

NJsonSchema-based JSON Schema generation for F# types (formerly `FSharp.Data.JsonSchema`).

- `Generator.Create` and `Generator.CreateMemoized` generate `NJsonSchema.JsonSchema` from F# types
- `Validation` module for validating JSON against a schema
- `FSharp.Data.Json` serializer with schema validation
- Targets netstandard2.0 through net10.0

### FSharp.Data.JsonSchema ⚠️ DEPRECATED

**This package is deprecated and renamed to `FSharp.Data.JsonSchema.NJsonSchema`.**

The `FSharp.Data.JsonSchema` package now serves as a compatibility shim that references `FSharp.Data.JsonSchema.NJsonSchema`. Please update your package reference to use `FSharp.Data.JsonSchema.NJsonSchema` directly. This deprecated package will not receive updates beyond version 3.0.0.

### FSharp.Data.JsonSchema.OpenApi

OpenAPI schema translator for F# types, designed for ASP.NET Core's built-in OpenAPI support.

- `FSharpSchemaTransformer` implements `IOpenApiSchemaTransformer` for use with `MapOpenApi()`
- Supports Microsoft.OpenApi 1.6.x (net9.0) and 2.0.x (net10.0)
- Targets net9.0 and net10.0

## Why JSON Schema?

[JSON Schema](https://json-schema.org/) is a standard, evolving format for specifying the structure of JSON documents. JSON Schema is used in [Open API](https://www.openapis.org/) and can be used by clients to validate that the payload received or to be sent matches the expected schema.

Tools exist for generating nicely formatted JSON from F#, e.g. [FSharpLu.Json](https://github.com/Microsoft/fsharplu) and [FSharp.SystemTextJson](https://github.com/Tarmil/FSharp.SystemTextJson).

[NJsonSchema](https://github.com/RicoSuter/NJsonSchema) provides a way to generate and validate JSON Schema for .NET languages, but these don't necessarily translate well to F# types, e.g. `anyOf` mapping to F#'s discriminated unions. This library strives to fill this gap.

## Usage

### Core: Analyze F# types into schema IR

```fsharp
open FSharp.Data.JsonSchema.Core

type Person = { FirstName: string; LastName: string; Age: int option }

type Shape =
    | Circle of radius: float
    | Rectangle of width: float * height: float

// Analyze a type into a SchemaDocument
let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<Person>

// Pattern match over the schema IR
match doc.Root with
| SchemaNode.Object obj ->
    printfn "Properties: %A" (obj.Properties |> List.map _.Name)
    printfn "Required: %A" obj.Required
| _ -> ()
```

### NJsonSchema: Generate and validate JSON Schema

```fsharp
#r "nuget: FSharp.Data.JsonSchema.NJsonSchema, 3.0.0"
#r "nuget: NJsonSchema, 11.0.0"

open FSharp.Data.JsonSchema
open FSharp.Data.JsonSchema.Validation
open NJsonSchema

// Generate a JSON Schema from an F# type
let schema : JsonSchema = Generator.Create<Person>()

// Validate JSON against a schema
let json = """{"firstName": "John", "lastName": "Doe"}"""
let result = Validation.validate schema json
```

### OpenAPI: ASP.NET Core integration

```fsharp
// In your ASP.NET Core application (Program.fs)
open FSharp.Data.JsonSchema.OpenApi

builder.Services.AddOpenApi(fun options ->
    options.AddSchemaTransformer<FSharpSchemaTransformer>()
)

// Or with custom configuration:
open FSharp.Data.JsonSchema.Core

let config = { SchemaGeneratorConfig.defaults with DiscriminatorPropertyName = "type" }
builder.Services.AddOpenApi(fun options ->
    options.AddSchemaTransformer(FSharpSchemaTransformer(config))
)
```

## Supported Types

| F# Type | Schema Representation |
|---------|----------------------|
| Records | `object` with properties and required |
| Struct records | `object` (same as records) |
| Multi-case DUs | `anyOf` with discriminator |
| Fieldless DUs | `string` enum |
| F# enums | `integer` enum |
| `option<'T>` / `voption<'T>` | nullable wrapper |
| `Nullable<'T>` | nullable wrapper |
| `list<'T>` / `'T[]` / `seq<'T>` | `array` with items |
| `Map<string, 'T>` | `object` with additionalProperties |
| Recursive types | `$ref` with definitions |
| Generic types | Distinct schema per instantiation |
| .NET classes | `object` via reflection |
