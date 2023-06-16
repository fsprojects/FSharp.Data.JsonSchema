namespace FSharp.Data

open System.Text.Json
open System.Text.Json.Serialization


[<AutoOpen>]
module private Defaults =
    let private jsonFSharpConverterOptions =
        JsonFSharpOptions
          .Default()
          .WithUnionInternalTag()
          .WithUnionNamedFields()
          .WithUnwrapOption()
          .WithSkippableOptionFields()
          .WithUnionUnwrapFieldlessTags()
          .WithUnionUnwrapSingleCaseUnions(false)

    let mkOptions unionTagName =

        let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

        options.Converters.Add(JsonStringEnumConverter())

        options.Converters.Add(
            JsonFSharpConverter(
                jsonFSharpConverterOptions
                  .WithUnionTagName(unionTagName)
            )
        )

        options
                  

[<AbstractClass; Sealed>]
type Json private () =

    static let optionsCache =
        System.Collections.Concurrent.ConcurrentDictionary(dict [| Json.DefaultCasePropertyName, Json.DefaultOptions |])

    static member internal DefaultCasePropertyName = "kind"

    static member DefaultOptions = mkOptions Json.DefaultCasePropertyName

    static member Serialize(value) =
        JsonSerializer.Serialize(value, Json.DefaultOptions)

    static member Serialize(value, casePropertyName) =
        let options =
            optionsCache.GetOrAdd(
                casePropertyName,
                fun key -> mkOptions casePropertyName
            )

        JsonSerializer.Serialize(value, options)

    static member Deserialize<'T>(json: System.ReadOnlySpan<byte>) =
        JsonSerializer.Deserialize<'T>(json, options = Json.DefaultOptions)

    static member Deserialize<'T>(json: string) =
        JsonSerializer.Deserialize<'T>(json, options = Json.DefaultOptions)

    static member Deserialize<'T>(json: byref<Utf8JsonReader>) =
        JsonSerializer.Deserialize<'T>(&json, options = Json.DefaultOptions)

    static member Deserialize<'T>(json: System.ReadOnlySpan<byte>, casePropertyName) =
        let options =
            optionsCache.GetOrAdd(
                casePropertyName,
                fun key -> mkOptions casePropertyName
            )

        JsonSerializer.Deserialize<'T>(json, options = options)

    static member Deserialize<'T>(json: string, casePropertyName) =
        let options =
            optionsCache.GetOrAdd(
                casePropertyName,
                fun key -> mkOptions casePropertyName
            )

        JsonSerializer.Deserialize<'T>(json, options = options)

    static member Deserialize<'T>(json: byref<Utf8JsonReader>, casePropertyName) =
        let options =
            optionsCache.GetOrAdd(
                casePropertyName,
                fun key -> mkOptions casePropertyName
            )

        JsonSerializer.Deserialize<'T>(&json, options = options)
