module FSharp.JsonSchema.Tests.JsonSerializationTests

open Newtonsoft.Json
open Newtonsoft.Json.FSharp.Idiomatic
open Expecto

let settings =
    JsonSerializerSettings(
        Converters=[|Converters.StringEnumConverter(); OptionConverter(); SingleCaseDuConverter(); MultiCaseDuConverter("tag")|],
        ContractResolver=Serialization.CamelCasePropertyNamesContractResolver())

[<Tests>]
let tests =
    testList "json serialization" [
        test "Option.None should serialize as null" {
            let expected = "null"
            let actual = JsonConvert.SerializeObject(None, settings)
            Expect.equal actual expected "Expected serializer to use OptionConverter"
        }

        test "Option.Some(1) should serialize as 1" {
            let expected = "1"
            let actual = JsonConvert.SerializeObject(Some 1, settings)
            Expect.equal actual expected "Expected serializer to use OptionConverter"
        }

        test "TestDU.Case should serialize as {\"tag\":\"Case\"}" {
            let expected = """{"tag":"Case"}"""
            let actual = JsonConvert.SerializeObject(Case, settings)
            Expect.equal actual expected "Expected serializer to use MultiCaseDuConverter"
        }

        test "TestDU.WithOneField(1) should serialize as {\"tag\":\"WithOneField\",\"Item\":1}" {
            let expected = """{"tag":"WithOneField","Item":1}"""
            let actual = JsonConvert.SerializeObject(WithOneField 1, settings)
            Expect.equal actual expected "Expected serializer to use MultiCaseDuConverter"
        }

        test "TestDU.WithNamedFields(\"name\", 1.) should serialize as {\"tag\":\"WithOneField\",\"name\":\"name\",\"value\":1.0}" {
            let expected = """{"tag":"WithNamedFields","name":"name","value":1.0}"""
            let actual = JsonConvert.SerializeObject(WithNamedFields("name", 1.), settings)
            Expect.equal actual expected "Expected serializer to use MultiCaseDuConverter"
        }

        test "TestEnum.First should serialize as First" {
            let expected = "\"First\""
            let actual = JsonConvert.SerializeObject(TestEnum.First, settings)
            Expect.equal actual expected "Expected serializer to use StringEnumConverter"
        }

        test "TestEnum.First should serialize as Single" {
            let expected = "\"Single\""
            let actual = JsonConvert.SerializeObject(TestSingleDU.Single, settings)
            Expect.equal actual expected "Expected serializer to use SingleCaseDuConverter"
        }
    ]
