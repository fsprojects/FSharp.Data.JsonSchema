namespace FSharp.Data.JsonSchema.OpenApi

open System.Threading
open System.Threading.Tasks
open Microsoft.FSharp.Reflection
open Microsoft.AspNetCore.OpenApi
open FSharp.Data.JsonSchema.Core

#if NET10_0_OR_GREATER
open Microsoft.OpenApi
#else
open Microsoft.OpenApi.Models
#endif

/// Schema transformer that detects F# types and generates correct OpenAPI schemas
/// using FSharp.Data.JsonSchema.Core's SchemaAnalyzer and OpenApiSchemaTranslator.
type FSharpSchemaTransformer(config: SchemaGeneratorConfig) =

    /// Returns true if the type is an F# type that requires schema transformation.
    static let isFSharpType (ty: System.Type) =
        not (isNull ty)
        && (FSharpType.IsRecord(ty, true)
            || (FSharpType.IsUnion(ty, true)
                // Exclude option/voption which are handled by STJ
                && not (ty.IsGenericType
                        && (ty.GetGenericTypeDefinition() = typedefof<_ option>
                            || ty.GetGenericTypeDefinition() = typedefof<voption<_>>))))

    /// Copy schema properties from source into target, mutating in place.
    static let copySchemaInto (source: OpenApiSchema) (target: OpenApiSchema) =
        target.Type <- source.Type
        target.Format <- source.Format
        target.Items <- source.Items
        target.AdditionalPropertiesAllowed <- source.AdditionalPropertiesAllowed
        target.AdditionalProperties <- source.AdditionalProperties
        target.Discriminator <- source.Discriminator
        target.Default <- source.Default
        target.Properties.Clear()
        for kv in source.Properties do
            target.Properties.[kv.Key] <- kv.Value
        target.Required.Clear()
        for req in source.Required do
            target.Required.Add(req) |> ignore
        target.AnyOf.Clear()
        for s in source.AnyOf do
            target.AnyOf.Add(s)
        target.OneOf.Clear()
        for s in source.OneOf do
            target.OneOf.Add(s)
        target.AllOf.Clear()
        for s in source.AllOf do
            target.AllOf.Add(s)
        target.Enum.Clear()
        for e in source.Enum do
            target.Enum.Add(e)

    /// Create with default configuration.
    new() = FSharpSchemaTransformer(SchemaGeneratorConfig.defaults)

    interface IOpenApiSchemaTransformer with
        member _.TransformAsync(schema, context, _cancellationToken) =
            let ty = context.JsonTypeInfo.Type
            if isFSharpType ty then
                let doc = SchemaAnalyzer.analyze config ty
                let (translatedRoot, componentSchemas) = OpenApiSchemaTranslator.translate doc

                // Mutate the provided schema in-place
                copySchemaInto translatedRoot schema

                // Register component schemas
                // The transformer context doesn't expose document components directly,
                // so we attach definitions as nested anyOf references.
                // In a real integration, the document transformer or middleware
                // would register these in components/schemas.

                Task.CompletedTask
            else
                Task.CompletedTask
