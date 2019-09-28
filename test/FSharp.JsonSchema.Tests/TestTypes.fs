namespace FSharp.JsonSchema.Tests

type TestClass() =
    member val FirstName = "" with get, set
    member val LastName = "" with get, set

type TestRecord =
    { FirstName : string
      LastName : string }

type TestEnum =
    | First = 0
    | Second = 1
    | Third = 2

[<RequireQualifiedAccess>]
type TestSingleDU =
    | Single
    | Double
    | Triple

type TestDU =
    | Case
    | WithOneField of int
    | WithNamedFields of name:string * value:float

[<AutoOpen>]
module Common =
    open Newtonsoft.Json
    open Newtonsoft.Json.FSharp.Idiomatic

    let settings =
        JsonSerializerSettings(
            Converters=[|Converters.StringEnumConverter()
                         OptionConverter()
                         SingleCaseDuConverter()
                         MultiCaseDuConverter("tag")|],
            ContractResolver=Serialization.CamelCasePropertyNamesContractResolver())
