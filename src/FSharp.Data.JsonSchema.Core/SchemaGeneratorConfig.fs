namespace FSharp.Data.JsonSchema.Core

open System

/// Configuration controlling schema analyzer behavior.
type SchemaGeneratorConfig = {
    /// Union encoding style.
    UnionEncoding: UnionEncodingStyle
    /// Discriminator property name for tagged unions.
    DiscriminatorPropertyName: string
    /// Naming policy for record fields and DU case fields.
    PropertyNamingPolicy: string -> string
    /// Whether objects allow additional properties by default.
    AdditionalPropertiesDefault: bool
    /// Resolves a type to a string identifier for $ref generation.
    TypeIdResolver: Type -> string
    /// How option types are represented in the schema.
    OptionStyle: OptionSchemaStyle
    /// Whether single-case DUs with fields are unwrapped to the inner field schema.
    UnwrapSingleCaseDU: bool
    /// Whether record fields are all marked as required.
    RecordFieldsRequired: bool
    /// Whether fieldless DUs are represented as string enums.
    UnwrapFieldlessTags: bool
}

/// Functions for creating and working with SchemaGeneratorConfig.
module SchemaGeneratorConfig =

    /// Convert the first character of a string to lowercase (camelCase).
    let private camelCase (name: string) =
        if String.IsNullOrEmpty(name) then name
        elif name.Length = 1 then string (Char.ToLowerInvariant name.[0])
        else string (Char.ToLowerInvariant name.[0]) + name.Substring(1)

    /// Capitalize the first character (matching NJsonSchema's DefaultTypeNameGenerator behavior).
    let private pascalCase (name: string) =
        if String.IsNullOrEmpty(name) then name
        elif name.Length = 1 then string (Char.ToUpperInvariant name.[0])
        else string (Char.ToUpperInvariant name.[0]) + name.Substring(1)

    /// Default type ID resolver: uses the type's short name with PascalCase first character.
    /// For generic types, appends "Of" + type argument names (e.g., "PaginatedResultOfTestRecord").
    let private defaultTypeIdResolver (ty: Type) =
        if ty.IsGenericType then
            let baseName = ty.Name.Substring(0, ty.Name.IndexOf('`'))
            let args = ty.GetGenericArguments() |> Array.map (fun a -> pascalCase a.Name)
            pascalCase baseName + "Of" + String.Join("And", args)
        else
            pascalCase ty.Name

    /// Default configuration matching current library behavior:
    /// InternalTag encoding, "kind" discriminator, camelCase naming,
    /// additionalProperties true, nullable options, no single-case unwrap,
    /// record fields required, fieldless tags unwrapped.
    let defaults = {
        UnionEncoding = UnionEncodingStyle.InternalTag
        DiscriminatorPropertyName = "kind"
        PropertyNamingPolicy = camelCase
        AdditionalPropertiesDefault = true
        TypeIdResolver = defaultTypeIdResolver
        OptionStyle = OptionSchemaStyle.Nullable
        UnwrapSingleCaseDU = false
        RecordFieldsRequired = true
        UnwrapFieldlessTags = true
    }
