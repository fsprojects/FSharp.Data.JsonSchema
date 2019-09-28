module FSharp.JsonSchema.Tests.ValidationTests

open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Schema
open Newtonsoft.Json.Schema.Generation
open Expecto

[<Tests>]
let tests =
    testList "schema validation" [
        test "Enum validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let schema= generator.Generate(typeof<TestEnum>)
            let json = JsonConvert.SerializeObject(TestEnum.First, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "Class validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let schema = generator.Generate(typeof<TestClass>)
            let json = JsonConvert.SerializeObject(TestClass(FirstName="Ryan", LastName="Riley"), settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "Record validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let schema = generator.Generate(typeof<TestRecord>)
            let json = JsonConvert.SerializeObject({FirstName="Ryan"; LastName="Riley"}, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "None validates against schema for option<_>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<_>>)
            let json = JsonConvert.SerializeObject(None, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        ptest "None validates against schema for option<string>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<string>>)
            let json = JsonConvert.SerializeObject(None, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        ptest "None validates against schema for option<int>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<int>>)
            let json = JsonConvert.SerializeObject(None, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        ptest "None validates against schema for option<TestRecord>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<TestRecord>>)
            let json = JsonConvert.SerializeObject(None, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "Some \"test\" validates against schema for option<_>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<_>>)
            let json = JsonConvert.SerializeObject(Some "test", settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        ptest "Some \"test\" validates against schema for option<string>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<_>>)
            let json = JsonConvert.SerializeObject(Some "test", settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        ptest "Some 1 validates against schema for option<_>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<_>>)
            let json = JsonConvert.SerializeObject(Some 1, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "Some 1 validates against schema for option<int>" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<option<int>>)
            let json = JsonConvert.SerializeObject(Some 1, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        ptest "TestSingleDU.Single validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let schema = generator.Generate(typeof<TestSingleDU>)
            let json = JsonConvert.SerializeObject(TestSingleDU.Single, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "TestSingleDU.Double validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let schema = generator.Generate(typeof<TestSingleDU>)
            let json = JsonConvert.SerializeObject(TestSingleDU.Double, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        ptest "TestSingleDU.Triple validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create()
            let schema = generator.Generate(typeof<TestSingleDU>)
            let json = JsonConvert.SerializeObject(TestSingleDU.Triple, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "TestDU.Case validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<TestDU>)
            let json = JsonConvert.SerializeObject(TestDU.Case, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "TestDU.WithOneField 1 validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<TestDU>)
            let json = JsonConvert.SerializeObject(TestDU.WithOneField 1, settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }

        test "TestDU.WithNamedFields(\"name\", 1.0) validates against schema" {
            let generator : JSchemaGenerator = JSchemaGenerator.Create(casePropertyName="tag")
            let schema = generator.Generate(typeof<TestDU>)
            let json = JsonConvert.SerializeObject(TestDU.WithNamedFields("name", 1.0), settings)
            let jtoken = JToken.Parse json
            let actual = jtoken.IsValid(schema)
            "╰〳 ಠ 益 ಠೃ 〵╯" |> Expect.isTrue actual
        }
    ]
