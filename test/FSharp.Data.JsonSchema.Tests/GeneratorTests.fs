module FSharp.Data.JsonSchema.Tests.GeneratorTests

open FSharp.Data.JsonSchema
open Expecto
open VerifyTests
open VerifyExpecto

// do VerifyDiffPlex.Initialize()
do ClipboardAccept.Enable()

let verifySettings =
    let s = VerifySettings()
    s.UseDirectory("generator-verified")
    s

type VerifyBuilder(name,focusState) =
    inherit TestCaseBuilder(name,focusState)
    let makeValidFilePath (input: string) : string =
        let invalidChars = System.IO.Path.GetInvalidFileNameChars() |> Array.append  [| '\''; '"'; '<'; '>'; '|'; '?'; '*'; ':'; '\\'|]

        let replaceChar = '_'
        input.Trim()
        |> Seq.map(fun c -> if Array.contains c invalidChars then replaceChar else c )
        |> System.String.Concat

    member __.Return<'T>(v:'T) = Verifier.Verify(makeValidFilePath name, v,settings= verifySettings).Wait()

let verify name = VerifyBuilder(name,FocusState.Normal)
let fverify name = VerifyBuilder(name,FocusState.Focused)

let json ( schema: NJsonSchema.JsonSchema ) = schema.ToJson()

[<Tests>]
let tests =
    let generator = Generator.CreateMemoized("tag")

    let equal (actual: NJsonSchema.JsonSchema) expected message =
        let actual = actual.ToJson()
        Expect.equal (Util.stripWhitespace actual) (Util.stripWhitespace expected) message

    testList
        "schema generation"
        [ verify "Enum generates proper schema" {
            return generator typeof<TestEnum> |> json
          }

          verify "Class generates proper schema" {
              return generator typeof<TestClass> |> json
          }

          verify "Record generates proper schema" {
               return generator typeof<TestRecord> |> json
          }

          verify "option<'a> generates proper schema" {
              return generator typeof<option<_>> |> json
          }

          verify "option<int> generates proper schema" {
              return generator typeof<option<int>> |> json
          }

          verify "TestSingleDU generates proper schema" {
              return generator typeof<TestSingleDU> |> json
          }

          verify "Multi-case DU generates proper schema" {
              return generator typeof<TestDU> |> json
          }

          verify "Nested generates proper schema" {
              return generator typeof<Nested> |> json
          }

          verify "RecWithOption generates proper schema" {
              return generator typeof<RecWithOption> |> json
          }

          verify "RecWithGenericOption generates proper schema" {
              return generator typeof<RecWithGenericOption<TestDU>> |> json
          }

          verify "RecWithArrayOption generates proper schema" {
              return generator typeof<RecWithArrayOption> |> json
          }

          verify "RecWithNullable generates proper schema" {
              return generator typeof<RecWithNullable> |> json
          }

          verify "PaginatedResult<'T> generates proper schema" {
              return generator typeof<PaginatedResult<_>> |> json
          }
          
          verify "FSharp list generates proper schema" {
              return generator typeof<TestList> |> json
          }

          verify "FSharp decimal generates correct schema" {
            return generator typeof<TestDecimal> |> json
          }

          verify "Record with annotations generates proper schema" {
            return generator typeof<RecordWithAnnotations> |> json
          }

          verify "Interdependent DUs generate proper schema" {
            return generator typeof<Chicken> |> json
          }

          verify "DU with array of records generates proper schema" {
            return generator typeof<DUWithRecArray> |> json
          }

          verify "DU with array of DUs generates proper schema" {
            return generator typeof<DUWithDUArray> |> json
          }

          verify "Record with array of records generates proper schema" {
            return generator typeof<RecWithRecArray> |> json
          }

          verify "Interdependent DUs with optional fields generate proper schema" {
            return generator typeof<Even> |> json
          }]
