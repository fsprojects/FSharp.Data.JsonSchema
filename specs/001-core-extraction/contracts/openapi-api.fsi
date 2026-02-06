// FSharp.Data.JsonSchema.OpenApi public API surface
// This is a design contract — not a compilable signature file.

namespace FSharp.Data.JsonSchema.OpenApi

open FSharp.Data.JsonSchema.Core

// ── Translator ──

module OpenApiSchemaTranslator =
    /// Translate a SchemaDocument to an OpenApiSchema.
    /// Returns (root schema, component schemas for registration).
    val translate: SchemaDocument -> Microsoft.OpenApi.Models.OpenApiSchema * Map<string, Microsoft.OpenApi.Models.OpenApiSchema>

// ── ASP.NET Core Integration ──

/// Schema transformer that detects F# types and generates
/// correct OpenAPI schemas using FSharp.Data.JsonSchema.Core.
type FSharpSchemaTransformer =
    /// Create with default configuration.
    new: unit -> FSharpSchemaTransformer
    /// Create with explicit configuration.
    new: config: SchemaGeneratorConfig -> FSharpSchemaTransformer
    /// IOpenApiSchemaTransformer implementation.
    interface Microsoft.AspNetCore.OpenApi.IOpenApiSchemaTransformer
