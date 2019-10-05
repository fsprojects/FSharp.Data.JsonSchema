module FSharp.JsonSchema.Tests.ValidationTests

open FSharp.Data
open FSharp.Data.JsonSchema
open Expecto

[<Tests>]
let tests =
    let generator = Generator.CreateMemoized("tag")

    testList "schema validation" [
        test "Enum validates against schema" {
            let schema = generator(typeof<TestEnum>)
            let json = Json.Serialize(TestEnum.First, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "Class validates against schema" {
            let schema = generator(typeof<TestClass>)
            let json = Json.Serialize(TestClass(FirstName="Ryan", LastName="Riley"), "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "Record validates against schema" {
            let schema = generator(typeof<TestRecord>)
            let json = Json.Serialize({FirstName="Ryan"; LastName="Riley"}, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "None validates against schema for option<_>" {
            let schema = generator(typeof<option<_>>)
            let json = Json.Serialize(None, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "None validates against schema for option<string>" {
            let schema = generator(typeof<option<string>>)
            let json = Json.Serialize(None, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "None validates against schema for option<int>" {
            let schema = generator(typeof<option<int>>)
            let json = Json.Serialize(None, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "None validates against schema for option<TestRecord>" {
            let schema = generator(typeof<option<TestRecord>>)
            let json = Json.Serialize(None, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "Some \"test\" validates against schema for option<_>" {
            let schema = generator(typeof<option<_>>)
            let json = Json.Serialize(Some "test", "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        ptest "Some \"test\" validates against schema for option<string>" {
            let schema = generator(typeof<option<_>>)
            let json = Json.Serialize(Some "test", "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "Some 1 validates against schema for option<_>" {
            let schema = generator(typeof<option<_>>)
            let json = Json.Serialize(Some 1, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "Some 1 validates against schema for option<int>" {
            let schema = generator(typeof<option<int>>)
            let json = Json.Serialize(Some 1, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        ptest "TestSingleDU.Single validates against schema" {
            let schema = generator(typeof<TestSingleDU>)
            let json = Json.Serialize(TestSingleDU.Single, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "TestSingleDU.Double validates against schema" {
            let schema = generator(typeof<TestSingleDU>)
            let json = Json.Serialize(TestSingleDU.Double, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "TestSingleDU.Triple validates against schema" {
            let schema = generator(typeof<TestSingleDU>)
            let json = Json.Serialize(TestSingleDU.Triple, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "TestDU.Case validates against schema" {
            let schema = generator(typeof<TestDU>)
            let json = Json.Serialize(TestDU.Case, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "TestDU.WithOneField 1 validates against schema" {
            let schema = generator(typeof<TestDU>)
            let json = Json.Serialize(TestDU.WithOneField 1, "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }

        test "TestDU.WithNamedFields(\"name\", 1.0) validates against schema" {
            let schema = generator(typeof<TestDU>)
            let json = Json.Serialize(TestDU.WithNamedFields("name", 1.0), "tag")
            let actual = Validation.validate schema json
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.equal actual (Ok())
        }
    ]
