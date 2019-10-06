namespace FSharp.Data.JsonSchema.Tests

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

module Util =
    let stripWhitespace text =
        System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "")
