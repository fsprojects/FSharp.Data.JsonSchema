namespace FSharp.Data.JsonSchema.Tests

open System
open System.Collections.Generic

// Regression test types for format-annotated types
type RecWithDateTime = { EventDate: DateTime; ModifiedAt: DateTimeOffset }
type RecWithGuid = { Id: Guid; CorrelationId: Guid }
type RecWithUri = { Homepage: Uri; ApiEndpoint: Uri }
type RecWithTimeSpan = { Duration: TimeSpan; Timeout: TimeSpan }
type RecWithByteArray = { Data: byte[]; Signature: byte[] }
type RecWithMap = { Metadata: Map<string, string>; Tags: Map<string, int> }
type RecWithDictionary = { Properties: Dictionary<string, string>; Counters: Dictionary<string, int> }
type RecWithSet = { UniqueIds: Set<int>; Categories: Set<string> }

#if NET6_0_OR_GREATER
type RecWithDateOnly = { BirthDate: DateOnly; StartDate: DateOnly }
type RecWithTimeOnly = { MeetingTime: TimeOnly; Deadline: TimeOnly }
#endif

type TestClass() =
    member val FirstName = "" with get, set
    member val LastName = "" with get, set

type TestRecord = { FirstName: string; LastName: string }

[<Struct>]
type TestStructRecord = { A: int; B: float }

type TestList =
    { Id: int
      Name: string
      Records: TestRecord list }

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

type DuWithDecimal =
    | Nothing
    | Amount of decimal

type TestDecimal =
    { Test: DuWithDecimal
      Total: decimal }

type RecWithOption =
    { Name: string
      Description: string option }

type RecWithValueOption =
    { Count: int voption
      Hey: string }

type RecWithGenericOption<'T> =
    { Car: string
      CarType: 'T option }

type RecWithGenericValueOption<'T> =
    { Car: string
      CarType: 'T voption }

type RecWithArrayOption =
    { Hey: string; Many: string array option }

type RecWithNullable =
    { Need: int
      NoNeed: System.Nullable<int> }

type RecWithSkippableSeq =
    { Post: string
      Likes: System.Text.Json.Serialization.Skippable<string seq> }

type PaginatedResult<'T> =
    { Page: int
      PerPage: int
      Total: int
      Results: 'T seq }

type SingleCaseDU =
    | OnlyCase of onlyCase: TestRecord

open System.ComponentModel.DataAnnotations

type RecordWithAnnotations =
    { [<Required>] RegEx: string
      [<MaxLength(10)>] MaxLength : string
      [<Range(0, 100)>] Range: int }

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


type RecWithRecArray = { V : TestRecord array }

type DUWithRecArray = AA | Records of TestRecord array

type DUWithDUArray = Dus of TestDU array

module Util =
    let stripWhitespace text =
        System.Text.RegularExpressions.Regex.Replace(text, @"\s+", "")
