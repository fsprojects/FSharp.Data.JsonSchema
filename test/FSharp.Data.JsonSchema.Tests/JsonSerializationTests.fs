module FSharp.JsonSchema.Tests.JsonSerializationTests

open FSharp.Data
open Expecto

[<Tests>]
let tests =
    testList "json serialization" [
        test "Option.None should serialize as null" {
            let expected = "null"
            let actual = Json.Serialize(None, "tag")
            Expect.equal actual expected "Expected serializer to use OptionConverter"
        }

        test "Option.Some(1) should serialize as 1" {
            let expected = "1"
            let actual = Json.Serialize(Some 1, "tag")
            Expect.equal actual expected "Expected serializer to use OptionConverter"
        }

        test "TestDU.Case should serialize as {\"tag\":\"Case\"}" {
            let expected = """{"tag":"Case"}"""
            let actual = Json.Serialize(Case, "tag")
            Expect.equal actual expected "Expected serializer to use MultiCaseDuConverter"
        }

        test "TestDU.WithOneField(1) should serialize as {\"tag\":\"WithOneField\",\"Item\":1}" {
            let expected = """{"tag":"WithOneField","Item":1}"""
            let actual = Json.Serialize(WithOneField 1, "tag")
            Expect.equal actual expected "Expected serializer to use MultiCaseDuConverter"
        }

        test "TestDU.WithNamedFields(\"name\", 1.) should serialize as {\"tag\":\"WithOneField\",\"name\":\"name\",\"value\":1.0}" {
            let expected = """{"tag":"WithNamedFields","name":"name","value":1.0}"""
            let actual = Json.Serialize(WithNamedFields("name", 1.), "tag")
            Expect.equal actual expected "Expected serializer to use MultiCaseDuConverter"
        }

        test "TestEnum.First should serialize as First" {
            let expected = "\"First\""
            let actual = Json.Serialize(TestEnum.First, "tag")
            Expect.equal actual expected "Expected serializer to use StringEnumConverter"
        }

        test "TestEnum.First should serialize as Single" {
            let expected = "\"Single\""
            let actual = Json.Serialize(TestSingleDU.Single, "tag")
            Expect.equal actual expected "Expected serializer to use SingleCaseDuConverter"
        }
    ]
