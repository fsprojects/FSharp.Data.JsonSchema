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

        verify "Choice<string, int> produces anyOf schema" {
            return generator typeof<RecWithChoice2> |> json
        }

        verify "Choice<string, int, bool> produces anyOf with three alternatives" {
            return generator typeof<RecWithChoice3> |> json
        }

        verify "Choice<string, TestRecord> produces anyOf with primitive and ref" {
            return generator typeof<RecWithChoiceComplex> |> json
        }

        verify "nested Choice types produce nested anyOf structures" {
            return generator typeof<RecWithNestedChoice> |> json
        }

        verify "anonymous record produces inline object schema" {
            return generator typeof<RecWithAnonRecord> |> json
        }

        verify "nested anonymous records produce nested inline objects" {
            return generator typeof<RecWithNestedAnonRecord> |> json
        }

        verify "anonymous record with optional field" {
            return generator typeof<RecWithOptionalAnonField> |> json
        }

        verify "anonymous record in collection produces inline schema" {
            return generator typeof<RecWithAnonInCollection> |> json
        }
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

[<Tests>]
let duEncodingTests =
    testList "DU encoding styles" [
        test "InternalTag: discriminator + fields in same object" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.InternalTag)
            let schema = gen typeof<TestDUForEncoding>
            let jsonStr = schema.ToJson()

            // Verify it's an anyOf schema
            Expect.equal schema.AnyOf.Count 3 "Should have 3 cases in anyOf"

            // Check MultiField case has kind + fields at same level
            let multiFieldDef = schema.Definitions.["MultiField"]
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("kind")) "Should have 'kind' discriminator"
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("name")) "Should have 'name' field"
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("count")) "Should have 'count' field"
            Expect.equal multiFieldDef.Properties.Count 3 "Should have 3 properties total"
        }

        test "AdjacentTag: separate tag and fields properties" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.AdjacentTag)
            let schema = gen typeof<TestDUForEncoding>

            // Verify it's an anyOf schema
            Expect.equal schema.AnyOf.Count 3 "Should have 3 cases in anyOf"

            // Check MultiField case has kind + fields structure
            let multiFieldDef = schema.Definitions.["MultiField"]
            Expect.equal multiFieldDef.Properties.Count 2 "Should have 2 properties (kind + fields)"
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("kind")) "Should have 'kind' property"
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("fields")) "Should have 'fields' property"

            // Verify fields property is an object with the actual fields
            let fieldsSchema = multiFieldDef.Properties.["fields"]
            Expect.equal fieldsSchema.Type NJsonSchema.JsonObjectType.Object "fields should be an object"
            Expect.isTrue (fieldsSchema.Properties.ContainsKey("name")) "fields should contain 'name'"
            Expect.isTrue (fieldsSchema.Properties.ContainsKey("count")) "fields should contain 'count'"
        }

        test "ExternalTag: case name as property key" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.ExternalTag)
            let schema = gen typeof<TestDUForEncoding>

            // Verify it's an anyOf schema
            Expect.equal schema.AnyOf.Count 3 "Should have 3 cases in anyOf"

            // Check MultiField case wraps fields in case name property
            let multiFieldDef = schema.Definitions.["MultiField"]
            Expect.equal multiFieldDef.Properties.Count 1 "Should have 1 property (case name)"
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("MultiField")) "Should have 'MultiField' property"

            // Verify the case property contains the fields
            let caseSchema = multiFieldDef.Properties.["MultiField"]
            Expect.equal caseSchema.Type NJsonSchema.JsonObjectType.Object "Case property should be an object"
            Expect.isTrue (caseSchema.Properties.ContainsKey("name")) "Should contain 'name' field"
            Expect.isTrue (caseSchema.Properties.ContainsKey("count")) "Should contain 'count' field"
        }

        test "Untagged: no discriminator, just fields" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.Untagged)
            let schema = gen typeof<TestDUForEncoding>

            // Verify it's an anyOf schema
            Expect.equal schema.AnyOf.Count 3 "Should have 3 cases in anyOf"

            // Check MultiField case has only fields (no discriminator)
            let multiFieldDef = schema.Definitions.["MultiField"]
            Expect.equal multiFieldDef.Properties.Count 2 "Should have 2 properties (only fields)"
            Expect.isFalse (multiFieldDef.Properties.ContainsKey("kind")) "Should NOT have 'kind' discriminator"
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("name")) "Should have 'name' field"
            Expect.isTrue (multiFieldDef.Properties.ContainsKey("count")) "Should have 'count' field"
        }

        test "Attribute override: per-type attribute overrides config" {
            // Config says InternalTag, but attribute on type says AdjacentTag
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.InternalTag)
            let schema = gen typeof<TestDUWithAttributeOverride>

            // Should use AdjacentTag from attribute, not InternalTag from config
            let case2Def = schema.Definitions.["Case2"]
            Expect.equal case2Def.Properties.Count 2 "Should have 2 properties (kind + fields) from AdjacentTag"
            Expect.isTrue (case2Def.Properties.ContainsKey("kind")) "Should have 'kind' property"
            Expect.isTrue (case2Def.Properties.ContainsKey("fields")) "Should have 'fields' property"

            // Verify fields property contains the case field
            let fieldsSchema = case2Def.Properties.["fields"]
            Expect.equal fieldsSchema.Type NJsonSchema.JsonObjectType.Object "fields should be an object"
            Expect.isTrue (fieldsSchema.Properties.ContainsKey("value")) "fields should contain 'value'"
        }

        verify "InternalTag schema snapshot" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.InternalTag)
            return gen typeof<TestDUForEncoding> |> json
        }

        verify "AdjacentTag schema snapshot" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.AdjacentTag)
            return gen typeof<TestDUForEncoding> |> json
        }

        verify "ExternalTag schema snapshot" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.ExternalTag)
            return gen typeof<TestDUForEncoding> |> json
        }

        verify "Untagged schema snapshot" {
            let gen = Generator.Create(unionEncoding = FSharp.Data.JsonSchema.Core.UnionEncodingStyle.Untagged)
            return gen typeof<TestDUForEncoding> |> json
        }
    ]
