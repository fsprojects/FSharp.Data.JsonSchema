// FSharp.Data.JsonSchema public API surface (post-refactoring)
// Existing public API preserved exactly. Internal flow changes.
// This is a design contract — not a compilable signature file.

namespace FSharp.Data.JsonSchema

// ── Existing public types (UNCHANGED) ──

type OptionSchemaProcessor =
    new: unit -> OptionSchemaProcessor
    interface NJsonSchema.Generation.ISchemaProcessor

type SingleCaseDuSchemaProcessor =
    new: unit -> SingleCaseDuSchemaProcessor
    interface NJsonSchema.Generation.ISchemaProcessor

type MultiCaseDuSchemaProcessor =
    new: ?casePropertyName: string -> MultiCaseDuSchemaProcessor
    interface NJsonSchema.Generation.ISchemaProcessor

type RecordSchemaProcessor =
    new: unit -> RecordSchemaProcessor
    interface NJsonSchema.Generation.ISchemaProcessor

[<AbstractClass; Sealed>]
type Generator =
    /// Creates a generator. Internally: analyze → translate.
    static member Create: ?casePropertyName: string -> (System.Type -> NJsonSchema.JsonSchema)
    /// Creates a memoized generator with global cache.
    static member CreateMemoized: ?casePropertyName: string -> (System.Type -> NJsonSchema.JsonSchema)

module Validation =
    val validate: schema: NJsonSchema.JsonSchema -> json: string -> Result<unit, NJsonSchema.Validation.ValidationError array>

// ── NEW internal module (not public) ──

module internal NJsonSchemaTranslator =
    val translate: FSharp.Data.JsonSchema.Core.SchemaDocument -> NJsonSchema.JsonSchema

// ── Existing extension (UNCHANGED) ──

namespace FSharp.Data

[<AbstractClass; Sealed>]
type Json =
    static member internal DefaultCasePropertyName: string
    static member DefaultOptions: System.Text.Json.JsonSerializerOptions
    static member Serialize: value: 'T -> string
    static member Serialize: value: 'T * casePropertyName: string -> string
    static member Deserialize<'T>: json: System.ReadOnlySpan<byte> -> 'T
    static member Deserialize<'T>: json: string -> 'T
    static member Deserialize<'T>: json: byref<System.Text.Json.Utf8JsonReader> -> 'T
    static member Deserialize<'T>: json: System.ReadOnlySpan<byte> * casePropertyName: string -> 'T
    static member Deserialize<'T>: json: string * casePropertyName: string -> 'T
    static member Deserialize<'T>: json: byref<System.Text.Json.Utf8JsonReader> * casePropertyName: string -> 'T
    static member DeserializeWithValidation<'T>: json: string * schema: NJsonSchema.JsonSchema -> Result<'T, NJsonSchema.Validation.ValidationError array>
    static member DeserializeWithValidation<'T>: json: string * schema: NJsonSchema.JsonSchema * casePropertyName: string -> Result<'T, NJsonSchema.Validation.ValidationError array>
