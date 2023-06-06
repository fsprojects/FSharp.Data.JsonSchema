namespace FSharp.Data

open System.Text.Json
open System.Text.Json.Serialization

[<AbstractClass; Sealed>]
type Json private () =

    static let optionsCache =
        System.Collections.Concurrent.ConcurrentDictionary(dict [| Json.DefaultCasePropertyName, Json.DefaultOptions |])

    static member internal DefaultCasePropertyName = "kind"

    static member DefaultOptions =
        let options =
            JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

        options.Converters.Add(JsonStringEnumConverter())

        options.Converters.Add(
            JsonFSharpConverter(
                JsonFSharpOptions
                    .Default()
                    .WithUnionInternalTag()
                    .WithUnionNamedFields()
                    .WithUnwrapOption()
                    .WithSkippableOptionFields()
                    .WithUnionTagName(Json.DefaultCasePropertyName)
                    .WithUnionUnwrapFieldlessTags()
            )
        )

        options
    (*
        JsonSerializerOptions(
            Converters=[|Converters.StringEnumConverter()
                         OptionConverter()
                         SingleCaseDuConverter()
                         MultiCaseDuConverter()|],
            ContractResolver=Serialization.CamelCasePropertyNamesContractResolver(),
            NullValueHandling=NullValueHandling.Ignore)
*)

    static member Serialize(value) =
        JsonSerializer.Serialize(value, Json.DefaultOptions)

    static member Serialize(value, casePropertyName) =
        let options =
            optionsCache.GetOrAdd(
                casePropertyName,
                let options =
                    JsonSerializerOptions(IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

                options.Converters.Add(JsonStringEnumConverter())

                options.Converters.Add(
                    JsonFSharpConverter(
                        JsonUnionEncoding.InternalTag
                        ||| JsonUnionEncoding.NamedFields
                        ||| JsonUnionEncoding.UnwrapFieldlessTags
                        ||| JsonUnionEncoding.UnwrapOption,
                        unionTagName = casePropertyName
                    )
                )

                options
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
                fun key ->
                    let options =
                        JsonSerializerOptions(
                            IgnoreNullValues = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        )

                    options.Converters.Add(JsonStringEnumConverter())

                    options.Converters.Add(
                        JsonFSharpConverter(
                            JsonUnionEncoding.InternalTag
                            ||| JsonUnionEncoding.NamedFields
                            ||| JsonUnionEncoding.UnwrapFieldlessTags
                            ||| JsonUnionEncoding.UnwrapOption,
                            unionTagName = casePropertyName
                        )
                    )

                    options
            )

        JsonSerializer.Deserialize<'T>(json, options = options)

    static member Deserialize<'T>(json: string, casePropertyName) =
        let options =
            optionsCache.GetOrAdd(
                casePropertyName,
                fun key ->
                    let options =
                        JsonSerializerOptions(
                            IgnoreNullValues = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        )

                    options.Converters.Add(JsonStringEnumConverter())

                    options.Converters.Add(
                        JsonFSharpConverter(
                            JsonUnionEncoding.InternalTag
                            ||| JsonUnionEncoding.NamedFields
                            ||| JsonUnionEncoding.UnwrapFieldlessTags
                            ||| JsonUnionEncoding.UnwrapOption,
                            unionTagName = casePropertyName
                        )
                    )

                    options
            )

        JsonSerializer.Deserialize<'T>(json, options = options)

    static member Deserialize<'T>(json: byref<Utf8JsonReader>, casePropertyName) =
        let options =
            optionsCache.GetOrAdd(
                casePropertyName,
                fun key ->
                    let options =
                        JsonSerializerOptions(
                            IgnoreNullValues = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        )

                    options.Converters.Add(JsonStringEnumConverter())

                    options.Converters.Add(
                        JsonFSharpConverter(
                            JsonUnionEncoding.InternalTag
                            ||| JsonUnionEncoding.NamedFields
                            ||| JsonUnionEncoding.UnwrapFieldlessTags
                            ||| JsonUnionEncoding.UnwrapOption,
                            unionTagName = casePropertyName
                        )
                    )

                    options
            )

        JsonSerializer.Deserialize<'T>(&json, options = options)
