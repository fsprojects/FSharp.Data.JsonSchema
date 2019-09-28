namespace FSharp.Data

open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.FSharp.Idiomatic

[<AbstractClass; Sealed>]
type Json private () =
    static let defaultSettings =
        JsonSerializerSettings(
            Converters=[|Converters.StringEnumConverter()
                         OptionConverter()
                         SingleCaseDuConverter()
                         MultiCaseDuConverter()|],
            ContractResolver=Serialization.CamelCasePropertyNamesContractResolver())

    static member Serialize(value) =
        JsonConvert.SerializeObject(value, defaultSettings)

    static member Serialize(value, casePropertyName) =
        let settings =
            JsonSerializerSettings(
                Converters=[|Converters.StringEnumConverter()
                             OptionConverter()
                             SingleCaseDuConverter()
                             MultiCaseDuConverter(casePropertyName)|],
                ContractResolver=Serialization.CamelCasePropertyNamesContractResolver())
        JsonConvert.SerializeObject(value, settings)

    static member Parse<'T>(json) =
        JsonConvert.DeserializeObject<'T>(json, defaultSettings)

    static member Parse<'T>(json, casePropertyName) =
        let settings =
            JsonSerializerSettings(
                Converters=[|Converters.StringEnumConverter()
                             OptionConverter()
                             SingleCaseDuConverter()
                             MultiCaseDuConverter(casePropertyName)|],
                ContractResolver=Serialization.CamelCasePropertyNamesContractResolver())
        JsonConvert.DeserializeObject<'T>(json, settings)

    static member ParseJToken(json) =
        JToken.Parse json
