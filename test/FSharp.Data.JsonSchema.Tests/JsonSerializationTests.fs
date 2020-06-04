module FSharp.Data.JsonSchema.Tests.JsonSerializationTests

open FSharp.Data
open Expecto

[<Tests>]
let tests =
    testList "json serialization" [
        test "Option.None should serialize as null" {
            let expected = "null"
            let actual = Json.Serialize(None, "tag")
            Expect.equal actual expected "Expected serializer to convert None to null"
        }

        test "Option.Some(1) should serialize as 1" {
            let expected = "1"
            let actual = Json.Serialize(Some 1, "tag")
            Expect.equal actual expected "Expected serializer to convert option to unwrapped value"
        }

        test "TestDU.Case should serialize as \"Case\"" {
            let expected = "\"Case\""
            let actual = Json.Serialize(Case, "tag")
            Expect.equal actual expected "Expected serializer to convert union case with no fields to untagged string literal"
        }

        test "TestDU.WithOneField(1) should serialize as {\"tag\":\"WithOneField\",\"item\":1}" {
            let expected = """{"tag":"WithOneField","item":1}"""
            let actual = Json.Serialize(WithOneField 1, "tag")
            Expect.equal actual expected "Expected serializer to convert union case with unnamed fields to object with tag named \"tag\" with unnamed field to Item1"
        }

        test "TestDU.WithNamedFields(\"name\", 1.) should serialize as {\"tag\":\"WithOneField\",\"name\":\"name\",\"value\":1}" {
            let expected = """{"tag":"WithNamedFields","name":"name","value":1}"""
            let actual = Json.Serialize(WithNamedFields("name", 1.), "tag")
            Expect.equal actual expected "Expected serializer to convert union case with named fields to object with tag named \"tag\" and named fields"
        }

        test "TestEnum.First should serialize as First" {
            let expected = "\"First\""
            let actual = Json.Serialize(TestEnum.First, "tag")
            Expect.equal actual expected "Expected serializer to use JsonStringEnumConverter"
        }

        test "TestEnum.First should serialize as \"Single\"" {
            let expected = "\"Single\""
            let actual = Json.Serialize(TestSingleDU.Single, "tag")
            Expect.equal actual expected "Expected serializer to convert union case with no fields to string literal"
        }
    ]
