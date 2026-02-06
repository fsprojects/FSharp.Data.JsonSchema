namespace FSharp.Data.JsonSchema.Core.Tests

open System

type TestRecord = { FirstName: string; LastName: string }

[<Struct>]
type TestStructRecord = { A: int; B: float }

type EmptyRecord = { _placeholder: unit }

type TestClass() =
    member val FirstName = "" with get, set
    member val LastName = "" with get, set

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
    | WithNamedFields of name: string * value: float

type Nested =
    | Rec of TestRecord
    | Du of TestDU
    | SingleDu of TestSingleDU
    | Enum of TestEnum
    | Class of TestClass
    | Opt of TestRecord option

type RecWithOption = { Name: string; Description: string option }

type RecWithValueOption = { Count: int voption; Hey: string }

type RecWithNullable = { Need: int; NoNeed: Nullable<int> }

type RecWithSkippableSeq =
    { Post: string
      Likes: System.Text.Json.Serialization.Skippable<string seq> }

type TestList = { Id: int; Name: string; Records: TestRecord list }

type RecWithArray = { Items: string array }

type PaginatedResult<'T> = { Page: int; PerPage: int; Total: int; Results: 'T seq }

type Chicken =
    | Have of Egg
    | DontHaveEgg
and Egg =
    | Have of Chicken
    | DontHaveChicken

type Even =
    | Even of Odd option
and Odd =
    | Odd of Even option

type DUWithRecArray = AA | Records of TestRecord array

type DUWithDUArray = Dus of TestDU array

type RecWithObjField = { Data: obj; Name: string }

type SingleCaseDU = | OnlyCase of onlyCase: TestRecord
