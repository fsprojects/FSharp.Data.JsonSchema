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

let verify name = VerifyBuilder(name,Normal)
let fverify name = VerifyBuilder(name,Focused)
let pverify name = VerifyBuilder(name,Pending)

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

          verify "voption<'a> generates proper schema" {
              return generator typeof<voption<_>> |> json
          }

          verify "option<int> generates proper schema" {
              return generator typeof<option<int>> |> json
          }

          verify "voption<int> generates proper schema" {
              return generator typeof<voption<int>> |> json
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

          verify "RecWithValueOption generates proper schema" {
              return generator typeof<RecWithValueOption> |> json
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

[<Tests>]
let formatTests =
    let generator = Generator.CreateMemoized("tag")
    testList "format types" [
        verify "DateTime generates string with date-time format" {
            return generator typeof<RecWithDateTime> |> json
        }

        verify "Guid generates string with guid format" {
            return generator typeof<RecWithGuid> |> json
        }

        verify "Uri generates string with uri format" {
            return generator typeof<RecWithUri> |> json
        }

        verify "TimeSpan generates string with duration format" {
            return generator typeof<RecWithTimeSpan> |> json
        }

        verify "byte[] generates string with byte format" {
            return generator typeof<RecWithByteArray> |> json
        }

        verify "Map<string,T> generates object with additionalProperties" {
            return generator typeof<RecWithMap> |> json
        }

        verify "Dictionary<string,T> generates object with additionalProperties" {
            return generator typeof<RecWithDictionary> |> json
        }

        verify "Set<T> generates array schema" {
            return generator typeof<RecWithSet> |> json
        }

#if NET6_0_OR_GREATER
        verify "DateOnly generates string with date format" {
            return generator typeof<RecWithDateOnly> |> json
        }

        verify "TimeOnly generates string with time format" {
            return generator typeof<RecWithTimeOnly> |> json
        }
#endif
    ]

[<Tests>]
let configTests =
    testList "config" [
        test "Generator.Create with custom casePropertyName uses 'type' discriminator" {
            let gen = Generator.Create(casePropertyName = "type")
            let schema = gen typeof<TestDU>
            let jsonStr = schema.ToJson()
            // The discriminator property in WithNamedFields should be "type"
            Expect.stringContains jsonStr "\"type\"" "Should contain 'type' discriminator property"
            // Should NOT contain "kind" as a property (default discriminator)
            let definitions = schema.Definitions
            for kv in definitions do
                if kv.Value.Properties.Count > 0 then
                    Expect.isFalse (kv.Value.Properties.ContainsKey("kind")) (sprintf "Definition '%s' should not have 'kind' property" kv.Key)
        }

        test "Generator.Create with custom casePropertyName produces valid schema structure" {
            let gen = Generator.Create(casePropertyName = "type")
            let schema = gen typeof<TestDU>
            // Should have AnyOf with 3 cases
            Expect.equal schema.AnyOf.Count 3 "Should have 3 AnyOf entries"
            // WithNamedFields definition should have "type" as first property
            let namedFields = schema.Definitions.["WithNamedFields"]
            Expect.isTrue (namedFields.Properties.ContainsKey("type")) "WithNamedFields should have 'type' discriminator"
            Expect.isTrue (namedFields.RequiredProperties.Contains("type")) "'type' should be required"
        }
    ]
