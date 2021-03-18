module FSharp.Data.JsonSchema.Tests.JsonSerializationTests

open System
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

        test "Option.None should roundtrip" {
            let expected = None
            let actual = Json.Deserialize(Json.Serialize(expected, "tag"), "tag")
            Expect.equal actual expected "Expected serializer to convert None to null"
        }

        test "Option.Some(1) should serialize as 1" {
            let expected = "1"
            let actual = Json.Serialize(Some 1, "tag")
            Expect.equal actual expected "Expected serializer to convert option to unwrapped value"
        }

        test "Option.Some(1) should roundtrip" {
            let expected = Some 1
            let actual = Json.Deserialize(Json.Serialize(expected, "tag"), "tag")
            Expect.equal actual expected "Expected serializer to convert option to unwrapped value"
        }

        test "tuple should serialize as array" {
            let expected = """["2021-03-01T00:00:00",10.01]"""
            let actual = Json.Serialize((DateTime(2021,3,1), 10.01))
            Expect.equal actual expected "Expected serializer to convert tuple to array"
        }

        test "tuple should roundtrip" {
            let expected = (DateTime(2021,3,1), 10.01)
            let actual = Json.Deserialize(Json.Serialize(expected))
            Expect.equal actual expected "Expected serializer to convert tuple to array"
        }

        test "array of tuples should serialize as array of arrays" {
            let expected = """[["2021-03-01T00:00:00",10.01]]"""
            let actual = Json.Serialize([|DateTime(2021,3,1), 10.01|])
            Expect.equal actual expected "Expected serializer to convert array of tuple to array of arrays"
        }

        test "array of tuples should roundtrip" {
            let expected = [|DateTime(2021,3,1), 10.01|]
            let actual = Json.Deserialize(Json.Serialize(expected))
            Expect.equal actual expected "Expected serializer to convert array of tuple to array of arrays"
        }

        test "TestDU.Case should serialize as \"Case\"" {
            let expected = "\"Case\""
            let actual = Json.Serialize(Case, "tag")
            Expect.equal actual expected "Expected serializer to convert union case with no fields to untagged string literal"
        }

        test "TestDU.Case should roundtrip" {
            let expected = Case
            let actual = Json.Deserialize(Json.Serialize(expected, "tag"), "tag")
            Expect.equal actual expected "Expected serializer to convert union case with no fields to untagged string literal"
        }

        test "TestDU.WithOneField(1) should serialize as {\"tag\":\"WithOneField\",\"item\":1}" {
            let expected = """{"tag":"WithOneField","item":1}"""
            let actual = Json.Serialize(WithOneField 1, "tag")
            Expect.equal actual expected "Expected serializer to convert union case with unnamed fields to object with tag named \"tag\" with unnamed field to Item1"
        }

        test "TestDU.WithOneField(1) should roundtrip" {
            let expected = WithOneField 1
            let actual = Json.Deserialize(Json.Serialize(expected, "tag"), "tag")
            Expect.equal actual expected "Expected serializer to convert union case with unnamed fields to object with tag named \"tag\" with unnamed field to Item1"
        }

        test "TestDU.WithNamedFields(\"name\", 1.) should serialize as {\"tag\":\"WithOneField\",\"name\":\"name\",\"value\":1}" {
            let expected = """{"tag":"WithNamedFields","name":"name","value":1}"""
            let actual = Json.Serialize(WithNamedFields("name", 1.), "tag")
            Expect.equal actual expected "Expected serializer to convert union case with named fields to object with tag named \"tag\" and named fields"
        }

        test "TestDU.WithNamedFields(\"name\", 1.) should roundtrip" {
            let expected = WithNamedFields("name", 1.)
            let actual = Json.Deserialize(Json.Serialize(expected, "tag"), "tag")
            Expect.equal actual expected "Expected serializer to convert union case with named fields to object with tag named \"tag\" and named fields"
        }

        test "TestEnum.First should serialize as First" {
            let expected = "\"First\""
            let actual = Json.Serialize(TestEnum.First, "tag")
            Expect.equal actual expected "Expected serializer to use JsonStringEnumConverter"
        }

        test "TestEnum.First should roundtrip" {
            let expected = TestEnum.First
            let actual = Json.Deserialize(Json.Serialize(expected, "tag"), "tag")
            Expect.equal actual expected "Expected serializer to use JsonStringEnumConverter"
        }

        test "TestEnum.Single should serialize as \"Single\"" {
            let expected = "\"Single\""
            let actual = Json.Serialize(TestSingleDU.Single, "tag")
            Expect.equal actual expected "Expected serializer to convert union case with no fields to string literal"
        }

        test "TestEnum.Single should roundtrip" {
            let expected = TestSingleDU.Single
            let actual = Json.Deserialize(Json.Serialize(expected, "tag"), "tag")
            Expect.equal actual expected "Expected serializer to convert union case with no fields to string literal"
        }
    ]
