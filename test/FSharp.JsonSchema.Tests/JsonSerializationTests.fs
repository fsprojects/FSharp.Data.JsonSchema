module JsonSerializationTests

open Newtonsoft.Json
open Newtonsoft.Json.FSharp.Idiomatic
open Expecto

type TestDU =
    | Case
    | WithOneField of int
    | WithNamedFields of name:string * value:float

let settings =
    JsonSerializerSettings(
        Converters=[|OptionConverter(); MultiCaseDuConverter("tag")|],
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

        test "Test.Case should serialize as {\"tag\":\"Case\"}" {
            let expected = """{"tag":"Case"}"""
            let actual = JsonConvert.SerializeObject(Case, settings)
            Expect.equal actual expected "Expected serializer to use OptionConverter"
        }

        test "Test.WithOneField(1) should serialize as {\"tag\":\"WithOneField\",\"Item\":1}" {
            let expected = """{"tag":"WithOneField","Item":1}"""
            let actual = JsonConvert.SerializeObject(WithOneField 1, settings)
            Expect.equal actual expected "Expected serializer to use OptionConverter"
        }

        test "Test.WithNamedFields(\"name\", 1.) should serialize as {\"tag\":\"WithOneField\",\"name\":\"name\",\"value\":1.0}" {
            let expected = """{"tag":"WithNamedFields","name":"name","value":1.0}"""
            let actual = JsonConvert.SerializeObject(WithNamedFields("name", 1.), settings)
            Expect.equal actual expected "Expected serializer to use OptionConverter"
        }
    ]
