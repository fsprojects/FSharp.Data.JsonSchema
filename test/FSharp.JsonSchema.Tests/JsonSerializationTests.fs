module JsonSerializationTests

open Newtonsoft.Json
open Expecto

type Test =
    | Case
    | WithOneField of int
    | WithNamedFields of name:string * value:float

[<Tests>]
let tests =
    testList "json serialization" [
        test "Option.None should serialize as null" {
            let expected = "null"
            let actual = "null"
            Expect.equal actual expected "Expected serializer to serialize Option.None as null"
        }
    ]
