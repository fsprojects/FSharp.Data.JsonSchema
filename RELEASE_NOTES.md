### FSharp.Data.JsonSchema.OpenApi 3.1.0

* Fix `InvalidOperationException: The input schema must be an OpenApiSchema or OpenApiSchemaReference` thrown by `Microsoft.AspNetCore.OpenApi` 10.0.3+ for any endpoint whose request/response type is (or contains) an F# discriminated union or a self-recursive type (#30)
* `FSharpSchemaTransformer` now registers component schemas into the live `OpenApiDocument` on net10 instead of leaving them unregistered with dangling references
* Fix self-recursive types (e.g. a tree or linked-list shaped DU) all resolving their self-reference to the same hardcoded component id on net10; each now gets its own correctly-named component
* On net10, definition/component ids (DU cases and nested referenced types alike) are now qualified by their owning root type's name (e.g. `TreeNode.Leaf` instead of `Leaf`), so two different types that happen to share a case or type name no longer silently collide in `components/schemas`
* Pin the net10 `Microsoft.OpenApi` dependency to 2.9.0 (was transitively 2.0.0), above the version affected by GHSA-v5pm-xwqc-g5wc
* Fix format-annotated types (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `Guid`, `Uri`, `TimeSpan`) generating a needless `$ref` to a single-purpose component (e.g. `WeatherForecast.DateTime`) instead of an inline `{"type": "string", "format": "date-time"}` schema (#29)

### FSharp.Data.JsonSchema.NJsonSchema 3.1.0

* No changes (version bump for consistency)

### FSharp.Data.JsonSchema.Core 3.1.0

* No changes (version bump for consistency)

### FSharp.Data.JsonSchema.NJsonSchema 3.0.1

* Fix recursive type serialization for self-referential types with nullable fields
* Fix NJsonSchemaTranslator to properly handle Ref("#") in nullable contexts

### FSharp.Data.JsonSchema.Core 3.0.1

* No changes (version bump for consistency)

### FSharp.Data.JsonSchema.OpenApi 3.0.1

* No changes (version bump for consistency)

### FSharp.Data.JsonSchema.NJsonSchema 3.0.0

* **New Package**: Renamed from FSharp.Data.JsonSchema (this is now the go-forward package name)
* Internal implementation now uses Core IR pipeline (`SchemaAnalyzer.analyze >> NJsonSchemaTranslator.translate`)
* Add dependency on FSharp.Data.JsonSchema.Core
* Add net9.0 and net10.0 target frameworks
* Existing public API (`Generator.Create`, `Generator.CreateMemoized`, schema processors, `Validation` module) remains unchanged
* All existing schema output remains byte-identical
* Critical regression fixes for format-annotated types (DateTime, Guid, Uri, byte[], Map, Dictionary, Set, etc.)

### FSharp.Data.JsonSchema 3.0.0 [DEPRECATED]

* **This package is deprecated** - renamed to FSharp.Data.JsonSchema.NJsonSchema
* This is a compatibility shim that references FSharp.Data.JsonSchema.NJsonSchema
* Please update your package reference to FSharp.Data.JsonSchema.NJsonSchema
* This package will not receive updates beyond 3.0.0
* Marked with `[<Obsolete>]` attributes to provide compiler warnings

### FSharp.Data.JsonSchema.Core 3.0.0

* **Version aligned** with main package family (was 1.0.0)
* Target-agnostic JSON Schema IR library
* `SchemaNode` discriminated union with 11 variants for representing JSON Schema
* `SchemaAnalyzer.analyze` for recursive F# type analysis
* `SchemaGeneratorConfig` for configuring discriminator property name, naming policy, and additional properties
* Supports: records, struct records, multi-case DUs, fieldless DUs, enums, option/voption, Nullable, lists, arrays, sequences, recursive types, generic types, .NET classes
* Only depends on FSharp.Core and FSharp.SystemTextJson
* Targets netstandard2.0 through net10.0

### FSharp.Data.JsonSchema.OpenApi 3.0.0

* **Version aligned** with main package family (was 1.0.0)
* OpenAPI schema translator for F# types
* `OpenApiSchemaTranslator.translate` converts Core IR to OpenApiSchema
* `FSharpSchemaTransformer` implements `IOpenApiSchemaTransformer` for ASP.NET Core OpenAPI integration
* Supports Microsoft.OpenApi 1.6.x (net9.0) and 2.0.x (net10.0) via conditional compilation
* Targets net9.0 and net10.0

### New in 2.0.2 - (Released 2023/04/16)
* Fix Multi Case DUs should set 'additionalProperties' to false (#16)
* Fix DUs with decimals cause the 'Decimal' type to get redefined (#18)
* Thank you, @blakeSaucier, for the issues and fixes!

### New in 2.0.1 - (Released 2022/11/10)
* Fix generation of F# list type (#12)

### New in 2.0.0 - (Released 2021/11/03)
* Fix stack overflow when generating schema for `seq<'a>` with an open generic parameter (#7)
* Fix nested type schema generation (#10)
* Enforce required and optional fields, particularly strings (#11)

### New in 1.0.0 - (Released 2020/11/18)
* Explicit support for .NET 5.0

### New in 0.1.0 - (Released 2020/06/05)
* Use System.Text.Json rather than Newtonsoft.Json [#4](https://github.com/panesofglass/FSharp.Data.JsonSchema/pull/5)

### New in 0.0.2 - (Released 2019/10/22)
* Default null value handling set to ignore null values
* Add support for netstandard2.1

### New in 0.0.1 - (Released 2019/10/22)
* Initial, non-alpha release

### New in 0.0.1-alpha1 - (Released 2019/10/22)
* Initial release
