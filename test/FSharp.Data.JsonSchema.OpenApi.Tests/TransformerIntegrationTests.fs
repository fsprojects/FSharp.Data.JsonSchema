module FSharp.Data.JsonSchema.OpenApi.Tests.TransformerIntegrationTests

open Expecto
open FSharp.Data.JsonSchema.Core
open FSharp.Data.JsonSchema.OpenApi

#if NET10_0_OR_GREATER
open Microsoft.OpenApi
type OASchema = Microsoft.OpenApi.OpenApiSchema
#else
open Microsoft.OpenApi.Models
type OASchema = Microsoft.OpenApi.Models.OpenApiSchema
#endif

type TestRecord = { FirstName: string; LastName: string }

type TestDU =
    | Case
    | WithField of value: int

/// Integration tests verifying end-to-end SchemaAnalyzer → OpenApiSchemaTranslator pipeline.
[<Tests>]
let integrationTests =
    testList "integration" [
        test "record type produces correct OpenApiSchema" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<TestRecord>
            let (schema: OASchema, _components) = OpenApiSchemaTranslator.translate doc
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.Object)) "object type"
#else
            Expect.equal schema.Type "object" "object type"
#endif
            Expect.isTrue (schema.Properties.ContainsKey "firstName") "has firstName"
            Expect.isTrue (schema.Properties.ContainsKey "lastName") "has lastName"
            Expect.isTrue (schema.Required.Contains "firstName") "firstName required"
            Expect.isTrue (schema.Required.Contains "lastName") "lastName required"
        }

        test "DU type produces AnyOf with discriminator" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<TestDU>
            let (schema: OASchema, components) = OpenApiSchemaTranslator.translate doc
            // Root should be AnyOf
            Expect.isGreaterThan schema.AnyOf.Count 0 "has anyOf entries"
            // Should have component schemas for cases
            Expect.isGreaterThan components.Count 0 "has component schemas"
        }

        test "FSharpSchemaTransformer can be instantiated with default config" {
            let transformer = FSharpSchemaTransformer()
            Expect.isNotNull (transformer :> obj) "transformer created"
        }

        test "FSharpSchemaTransformer can be instantiated with custom config" {
            let config = { SchemaGeneratorConfig.defaults with DiscriminatorPropertyName = "type" }
            let transformer = FSharpSchemaTransformer(config)
            Expect.isNotNull (transformer :> obj) "transformer created with custom config"
        }
    ]
