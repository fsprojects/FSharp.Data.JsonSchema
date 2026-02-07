module FSharp.Data.JsonSchema.Core.Tests.AnalyzerTests

open Expecto
open FSharp.Data.JsonSchema.Core
open FSharp.Data.JsonSchema.Core.Tests

let private analyze<'T> =
    SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<'T>

let private analyzeWith<'T> config =
    SchemaAnalyzer.analyze config typeof<'T>

let private hasDef key (doc: SchemaDocument) =
    doc.Definitions |> List.exists (fun (k, _) -> k = key)

let private getDef key (doc: SchemaDocument) =
    doc.Definitions |> List.pick (fun (k, v) -> if k = key then Some v else None)

[<Tests>]
let recordTests =
    testList "records" [
        test "simple record produces Object with correct properties and required" {
            let doc = analyze<TestRecord>
            match doc.Root with
            | SchemaNode.Object obj ->
                Expect.equal obj.Properties.Length 2 "Should have 2 properties"
                Expect.equal obj.Properties.[0].Name "firstName" "First prop camelCase"
                Expect.equal obj.Properties.[1].Name "lastName" "Second prop camelCase"
                Expect.equal obj.Required ["firstName"; "lastName"] "All fields required"
                Expect.isFalse obj.AdditionalProperties "No additional properties"
            | other -> failtestf "Expected Object, got %A" other
        }

        test "struct record produces Object" {
            let doc = analyze<TestStructRecord>
            match doc.Root with
            | SchemaNode.Object obj ->
                Expect.equal obj.Properties.Length 2 "Should have 2 properties"
                Expect.equal obj.Properties.[0].Name "a" "camelCase"
                Expect.equal obj.Required ["a"; "b"] "All fields required"
            | other -> failtestf "Expected Object, got %A" other
        }

        test "record with option field has optional property" {
            let doc = analyze<RecWithOption>
            match doc.Root with
            | SchemaNode.Object obj ->
                Expect.equal obj.Required ["name"] "Only non-option field required"
                let descProp = obj.Properties |> List.find (fun p -> p.Name = "description")
                match descProp.Schema with
                | SchemaNode.Nullable (SchemaNode.Primitive(PrimitiveType.String, None)) -> ()
                | other -> failtestf "Expected Nullable(Primitive String), got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "record with value option field has optional property" {
            let doc = analyze<RecWithValueOption>
            match doc.Root with
            | SchemaNode.Object obj ->
                Expect.equal obj.Required ["hey"] "Only non-voption field required"
                let countProp = obj.Properties |> List.find (fun p -> p.Name = "count")
                match countProp.Schema with
                | SchemaNode.Nullable (SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")) -> ()
                | other -> failtestf "Expected Nullable(Primitive Integer int32), got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "record with Nullable field" {
            let doc = analyze<RecWithNullable>
            match doc.Root with
            | SchemaNode.Object obj ->
                let noNeed = obj.Properties |> List.find (fun p -> p.Name = "noNeed")
                match noNeed.Schema with
                | SchemaNode.Nullable (SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")) -> ()
                | other -> failtestf "Expected Nullable(Primitive Integer int32), got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "record with Skippable field unwraps to inner type" {
            let doc = analyze<RecWithSkippableSeq>
            match doc.Root with
            | SchemaNode.Object obj ->
                let likes = obj.Properties |> List.find (fun p -> p.Name = "likes")
                match likes.Schema with
                | SchemaNode.Array (SchemaNode.Primitive(PrimitiveType.String, None)) -> ()
                | other -> failtestf "Expected Array(Primitive String), got %A" other
                Expect.contains obj.Required "likes" "Skippable field is required"
            | other -> failtestf "Expected Object, got %A" other
        }

        test "record with list field produces Array" {
            let doc = analyze<TestList>
            match doc.Root with
            | SchemaNode.Object obj ->
                let records = obj.Properties |> List.find (fun p -> p.Name = "records")
                match records.Schema with
                | SchemaNode.Array (SchemaNode.Ref _) -> ()
                | other -> failtestf "Expected Array(Ref), got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "record with array field produces Array" {
            let doc = analyze<RecWithArray>
            match doc.Root with
            | SchemaNode.Object obj ->
                let items = obj.Properties |> List.find (fun p -> p.Name = "items")
                match items.Schema with
                | SchemaNode.Array (SchemaNode.Primitive(PrimitiveType.String, None)) -> ()
                | other -> failtestf "Expected Array(Primitive String), got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "record with obj field produces Any" {
            let doc = analyze<RecWithObjField>
            match doc.Root with
            | SchemaNode.Object obj ->
                let data = obj.Properties |> List.find (fun p -> p.Name = "data")
                match data.Schema with
                | SchemaNode.Any -> ()
                | other -> failtestf "Expected Any, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }
    ]

[<Tests>]
let duTests =
    testList "discriminated unions" [
        test "fieldless DU produces Enum" {
            let doc = analyze<TestSingleDU>
            match doc.Root with
            | SchemaNode.Enum(values, PrimitiveType.String) ->
                Expect.equal values ["Single"; "Double"; "Triple"] "Enum values"
            | other -> failtestf "Expected Enum, got %A" other
        }

        test "integer enum produces Enum" {
            let doc = analyze<TestEnum>
            match doc.Root with
            | SchemaNode.Enum(values, PrimitiveType.String) ->
                Expect.equal values ["First"; "Second"; "Third"] "Enum values"
            | other -> failtestf "Expected Enum, got %A" other
        }

        test "multi-case DU produces AnyOf" {
            let doc = analyze<TestDU>
            match doc.Root with
            | SchemaNode.AnyOf schemas ->
                Expect.equal schemas.Length 3 "Should have 3 cases"
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "multi-case DU fieldless case produces Const" {
            let doc = analyze<TestDU>
            match doc.Root with
            | SchemaNode.AnyOf schemas ->
                // Root DU: cases are Refs to definitions
                let caseRef = schemas.[0]
                match caseRef with
                | SchemaNode.Ref "Case" ->
                    let caseDef = getDef "Case" doc
                    match caseDef with
                    | SchemaNode.Const("Case", PrimitiveType.String) -> ()
                    | other -> failtestf "Expected Const, got %A" other
                | other -> failtestf "Expected Ref Case, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "multi-case DU case with fields produces Object with discriminator" {
            let doc = analyze<TestDU>
            match doc.Root with
            | SchemaNode.AnyOf schemas ->
                let namedRef = schemas.[2]
                match namedRef with
                | SchemaNode.Ref "WithNamedFields" ->
                    let namedDef = getDef "WithNamedFields" doc
                    match namedDef with
                    | SchemaNode.Object obj ->
                        Expect.equal obj.Properties.[0].Name "kind" "Discriminator prop"
                        match obj.Properties.[0].Schema with
                        | SchemaNode.Const("WithNamedFields", PrimitiveType.String) -> ()
                        | other -> failtestf "Expected Const, got %A" other
                        Expect.equal obj.Properties.[1].Name "name" "Named field"
                        Expect.equal obj.Properties.[2].Name "value" "Named field"
                        Expect.contains obj.Required "kind" "Discriminator required"
                        Expect.contains obj.Required "name" "Named field required"
                    | other -> failtestf "Expected Object, got %A" other
                | other -> failtestf "Expected Ref WithNamedFields, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "nested DU produces AnyOf with Refs to definitions" {
            let doc = analyze<Nested>
            match doc.Root with
            | SchemaNode.AnyOf schemas ->
                Expect.equal schemas.Length 6 "Should have 6 cases"
                // All should be Refs (root DU)
                for s in schemas do
                    match s with
                    | SchemaNode.Ref _ -> ()
                    | other -> failtestf "Expected Ref, got %A" other
                // Check definitions exist
                Expect.isTrue (hasDef "TestRecord" doc) "TestRecord definition"
                Expect.isTrue (hasDef "TestDU" doc) "TestDU definition"
                Expect.isTrue (hasDef "Rec" doc) "Rec case definition"
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "DU with array of records creates intermediate definition" {
            let doc = analyze<DUWithRecArray>
            Expect.isTrue (hasDef "TestRecordOf" doc) "Intermediate array definition"
            match getDef "TestRecordOf" doc with
            | SchemaNode.Array (SchemaNode.Ref "TestRecord") -> ()
            | other -> failtestf "Expected Array(Ref TestRecord), got %A" other
        }
    ]

[<Tests>]
let recursiveTests =
    testList "recursive types" [
        test "mutually recursive DUs (Chicken/Egg) produce definitions" {
            let doc = analyze<Chicken>
            Expect.isTrue (hasDef "Egg" doc) "Egg definition exists"
            Expect.isTrue (hasDef "Have" doc) "Have case definition exists"
        }

        test "mutually recursive DUs with options (Even/Odd)" {
            let doc = analyze<Even>
            Expect.isTrue (hasDef "Odd" doc) "Odd definition exists"
            Expect.isTrue (hasDef "Even" doc) "Even case definition exists"
        }

        test "self-reference to root produces Ref #" {
            let doc = analyze<Chicken>
            // Egg.Have case references Chicken (root) → Ref "#"
            let egg = getDef "Egg" doc
            match egg with
            | SchemaNode.AnyOf cases ->
                let haveCase = cases |> List.pick (fun c ->
                    match c with
                    | SchemaNode.Object obj when obj.TypeId = Some "Have" -> Some obj
                    | _ -> None)
                let itemProp = haveCase.Properties |> List.find (fun p -> p.Name = "item")
                match itemProp.Schema with
                | SchemaNode.Ref "#" -> ()
                | other -> failtestf "Expected Ref #, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }
    ]

[<Tests>]
let genericTests =
    testList "generic types" [
        test "generic record produces Object with resolved type args" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<PaginatedResult<TestRecord>>
            match doc.Root with
            | SchemaNode.Object obj ->
                let results = obj.Properties |> List.find (fun p -> p.Name = "results")
                match results.Schema with
                | SchemaNode.Array (SchemaNode.Ref _) -> ()
                | other -> failtestf "Expected Array(Ref), got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }
    ]

[<Tests>]
let primitiveTests =
    testList "primitives" [
        test "string produces Primitive String" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<string>
            match doc.Root with
            | SchemaNode.Primitive(PrimitiveType.String, None) -> ()
            | other -> failtestf "Expected Primitive String, got %A" other
        }

        test "int produces Primitive Integer int32" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<int>
            match doc.Root with
            | SchemaNode.Primitive(PrimitiveType.Integer, Some "int32") -> ()
            | other -> failtestf "Expected Primitive Integer int32, got %A" other
        }

        test "float produces Primitive Number double" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<float>
            match doc.Root with
            | SchemaNode.Primitive(PrimitiveType.Number, Some "double") -> ()
            | other -> failtestf "Expected Primitive Number double, got %A" other
        }

        test "bool produces Primitive Boolean" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<bool>
            match doc.Root with
            | SchemaNode.Primitive(PrimitiveType.Boolean, None) -> ()
            | other -> failtestf "Expected Primitive Boolean, got %A" other
        }

        test "decimal produces Primitive Number decimal" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<decimal>
            match doc.Root with
            | SchemaNode.Primitive(PrimitiveType.Number, Some "decimal") -> ()
            | other -> failtestf "Expected Primitive Number decimal, got %A" other
        }
    ]

[<Tests>]
let optionTests =
    testList "option types" [
        test "option<obj> produces Any" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<option<_>>
            match doc.Root with
            | SchemaNode.Any -> ()
            | other -> failtestf "Expected Any, got %A" other
        }

        test "voption<obj> produces Any" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<voption<_>>
            match doc.Root with
            | SchemaNode.Any -> ()
            | other -> failtestf "Expected Any, got %A" other
        }

        test "option<int> produces Nullable Primitive" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<option<int>>
            match doc.Root with
            | SchemaNode.Nullable (SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")) -> ()
            | other -> failtestf "Expected Nullable(Primitive Integer), got %A" other
        }
    ]

[<Tests>]
let classTests =
    testList "classes" [
        test "class produces Object with no required fields" {
            let doc = analyze<TestClass>
            match doc.Root with
            | SchemaNode.Object obj ->
                Expect.equal obj.Properties.Length 2 "Should have 2 properties"
                Expect.isEmpty obj.Required "Class fields not required"
            | other -> failtestf "Expected Object, got %A" other
        }
    ]

[<Tests>]
let anyTests =
    testList "any/obj" [
        test "obj produces Any" {
            let doc = SchemaAnalyzer.analyze SchemaGeneratorConfig.defaults typeof<obj>
            match doc.Root with
            | SchemaNode.Any -> ()
            | other -> failtestf "Expected Any, got %A" other
        }
    ]

/// Exhaustive pattern match over all 11 SchemaNode variants.
/// This test has NO wildcard catch-all, so adding a new variant to SchemaNode
/// will produce a compiler incomplete-match warning here, ensuring consumers
/// are aware of the change.
let rec private describeNode (node: SchemaNode) : string =
    match node with
    | SchemaNode.Object obj -> sprintf "Object(%d props)" obj.Properties.Length
    | SchemaNode.Array items -> sprintf "Array(%s)" (describeNode items)
    | SchemaNode.AnyOf schemas -> sprintf "AnyOf(%d)" schemas.Length
    | SchemaNode.OneOf (schemas, _disc) -> sprintf "OneOf(%d)" schemas.Length
    | SchemaNode.Nullable inner -> sprintf "Nullable(%s)" (describeNode inner)
    | SchemaNode.Primitive (pt, fmt) -> sprintf "Primitive(%A, %A)" pt fmt
    | SchemaNode.Enum (values, pt) -> sprintf "Enum(%A, %A)" values pt
    | SchemaNode.Ref typeId -> sprintf "Ref(%s)" typeId
    | SchemaNode.Map valueSchema -> sprintf "Map(%s)" (describeNode valueSchema)
    | SchemaNode.Const (value, pt) -> sprintf "Const(%s, %A)" value pt
    | SchemaNode.Any -> "Any"

[<Tests>]
let exhaustivenessTests =
    testList "SchemaNode exhaustiveness" [
        test "all 11 SchemaNode variants are handled without wildcard" {
            // Exercise describeNode with representative values for each variant
            let nodes = [
                SchemaNode.Object { Properties = []; Required = []; AdditionalProperties = false; TypeId = None; Description = None; Title = None }
                SchemaNode.Array (SchemaNode.Any)
                SchemaNode.AnyOf []
                SchemaNode.OneOf ([], None)
                SchemaNode.Nullable SchemaNode.Any
                SchemaNode.Primitive (PrimitiveType.String, None)
                SchemaNode.Enum (["A"], PrimitiveType.String)
                SchemaNode.Ref "Test"
                SchemaNode.Map SchemaNode.Any
                SchemaNode.Const ("x", PrimitiveType.String)
                SchemaNode.Any
            ]
            let descriptions = nodes |> List.map describeNode
            Expect.equal descriptions.Length 11 "All 11 variants exercised"
            // Verify each description is non-empty (the function actually ran)
            for d in descriptions do
                Expect.isNotEmpty d "Description should be non-empty"
        }
    ]

[<Tests>]
let configTests =
    testList "config" [
        test "custom discriminator property name appears in DU case objects" {
            let config = { SchemaGeneratorConfig.defaults with DiscriminatorPropertyName = "type" }
            let doc = analyzeWith<TestDU> config
            match doc.Root with
            | SchemaNode.AnyOf schemas ->
                // WithNamedFields case should have discriminator named "type"
                let namedRef = schemas.[2]
                match namedRef with
                | SchemaNode.Ref "WithNamedFields" ->
                    let namedDef = getDef "WithNamedFields" doc
                    match namedDef with
                    | SchemaNode.Object obj ->
                        Expect.equal obj.Properties.[0].Name "type" "Discriminator uses custom name"
                        match obj.Properties.[0].Schema with
                        | SchemaNode.Const("WithNamedFields", PrimitiveType.String) -> ()
                        | other -> failtestf "Expected Const, got %A" other
                        Expect.contains obj.Required "type" "Custom discriminator required"
                    | other -> failtestf "Expected Object, got %A" other
                | other -> failtestf "Expected Ref WithNamedFields, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "custom PropertyNamingPolicy (PascalCase) produces PascalCase property names" {
            let config = { SchemaGeneratorConfig.defaults with PropertyNamingPolicy = id }
            let doc = analyzeWith<TestRecord> config
            match doc.Root with
            | SchemaNode.Object obj ->
                Expect.equal obj.Properties.[0].Name "FirstName" "PascalCase preserved"
                Expect.equal obj.Properties.[1].Name "LastName" "PascalCase preserved"
                Expect.equal obj.Required ["FirstName"; "LastName"] "Required uses PascalCase names"
            | other -> failtestf "Expected Object, got %A" other
        }

        test "custom PropertyNamingPolicy applies to DU case fields" {
            let config = { SchemaGeneratorConfig.defaults with PropertyNamingPolicy = id }
            let doc = analyzeWith<TestDU> config
            match doc.Root with
            | SchemaNode.AnyOf schemas ->
                let namedRef = schemas.[2]
                match namedRef with
                | SchemaNode.Ref "WithNamedFields" ->
                    let namedDef = getDef "WithNamedFields" doc
                    match namedDef with
                    | SchemaNode.Object obj ->
                        // Discriminator uses naming policy too
                        Expect.equal obj.Properties.[0].Name "kind" "Discriminator name unchanged"
                        Expect.equal obj.Properties.[1].Name "name" "DU field uses PascalCase policy"
                        Expect.equal obj.Properties.[2].Name "value" "DU field uses PascalCase policy"
                    | other -> failtestf "Expected Object, got %A" other
                | other -> failtestf "Expected Ref WithNamedFields, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "AdditionalPropertiesDefault does not affect record objects" {
            // Records always have AdditionalProperties = false regardless of config
            let config = { SchemaGeneratorConfig.defaults with AdditionalPropertiesDefault = true }
            let doc = analyzeWith<TestRecord> config
            match doc.Root with
            | SchemaNode.Object obj ->
                Expect.isFalse obj.AdditionalProperties "Record always has additionalProperties = false"
            | other -> failtestf "Expected Object, got %A" other
        }
    ]

[<Tests>]
let choiceTests =
    testList "Choice types" [
        test "Choice<string, int> produces AnyOf with two primitives" {
            let doc = analyze<RecWithChoice2>
            match doc.Root with
            | SchemaNode.Object obj ->
                let valueProp = obj.Properties |> List.find (fun p -> p.Name = "value")
                match valueProp.Schema with
                | SchemaNode.AnyOf schemas ->
                    Expect.equal schemas.Length 2 "Should have 2 alternatives"
                    match schemas.[0], schemas.[1] with
                    | SchemaNode.Primitive(PrimitiveType.String, None), SchemaNode.Primitive(PrimitiveType.Integer, Some "int32") -> ()
                    | other1, other2 -> failtestf "Expected String and Integer primitives, got %A and %A" other1 other2
                | other -> failtestf "Expected AnyOf, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "Choice<string, int, bool> produces AnyOf with three primitives" {
            let doc = analyze<RecWithChoice3>
            match doc.Root with
            | SchemaNode.Object obj ->
                let dataProp = obj.Properties |> List.find (fun p -> p.Name = "data")
                match dataProp.Schema with
                | SchemaNode.AnyOf schemas ->
                    Expect.equal schemas.Length 3 "Should have 3 alternatives"
                    match schemas.[0], schemas.[1], schemas.[2] with
                    | SchemaNode.Primitive(PrimitiveType.String, None),
                      SchemaNode.Primitive(PrimitiveType.Integer, Some "int32"),
                      SchemaNode.Primitive(PrimitiveType.Boolean, None) -> ()
                    | other1, other2, other3 -> failtestf "Expected String, Integer, Boolean, got %A, %A, %A" other1 other2 other3
                | other -> failtestf "Expected AnyOf, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "Choice<string, TestRecord> produces AnyOf with primitive and Ref" {
            let doc = analyze<RecWithChoiceComplex>
            match doc.Root with
            | SchemaNode.Object obj ->
                let resultProp = obj.Properties |> List.find (fun p -> p.Name = "result")
                match resultProp.Schema with
                | SchemaNode.AnyOf schemas ->
                    Expect.equal schemas.Length 2 "Should have 2 alternatives"
                    match schemas.[0], schemas.[1] with
                    | SchemaNode.Primitive(PrimitiveType.String, None), SchemaNode.Ref "TestRecord" -> ()
                    | other1, other2 -> failtestf "Expected String and Ref TestRecord, got %A and %A" other1 other2
                | other -> failtestf "Expected AnyOf, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "nested Choice<int, Choice<string, bool>> produces AnyOf with primitive and nested AnyOf" {
            let doc = analyze<RecWithNestedChoice>
            match doc.Root with
            | SchemaNode.Object obj ->
                let nestedProp = obj.Properties |> List.find (fun p -> p.Name = "nested")
                match nestedProp.Schema with
                | SchemaNode.AnyOf schemas ->
                    Expect.equal schemas.Length 2 "Should have 2 alternatives"
                    match schemas.[0], schemas.[1] with
                    | SchemaNode.Primitive(PrimitiveType.Integer, Some "int32"), SchemaNode.AnyOf innerSchemas ->
                        Expect.equal innerSchemas.Length 2 "Inner AnyOf should have 2 alternatives"
                        match innerSchemas.[0], innerSchemas.[1] with
                        | SchemaNode.Primitive(PrimitiveType.String, None), SchemaNode.Primitive(PrimitiveType.Boolean, None) -> ()
                        | other1, other2 -> failtestf "Expected String and Boolean in inner AnyOf, got %A and %A" other1 other2
                    | other1, other2 -> failtestf "Expected Integer and nested AnyOf, got %A and %A" other1 other2
                | other -> failtestf "Expected AnyOf, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }
    ]

[<Tests>]
let anonymousRecordTests =
    testList "anonymous records" [
        test "simple anonymous record produces inline Object with no TypeId" {
            let doc = analyze<RecWithAnonRecord>
            match doc.Root with
            | SchemaNode.Object obj ->
                let detailsProp = obj.Properties |> List.find (fun p -> p.Name = "details")
                match detailsProp.Schema with
                | SchemaNode.Object anonObj ->
                    Expect.isNone anonObj.TypeId "Anonymous record should have no TypeId"
                    Expect.isNone anonObj.Title "Anonymous record should have no Title"
                    Expect.equal anonObj.Properties.Length 2 "Should have 2 properties"
                    let field1 = anonObj.Properties |> List.find (fun p -> p.Name = "field1")
                    let field2 = anonObj.Properties |> List.find (fun p -> p.Name = "field2")
                    match field1.Schema, field2.Schema with
                    | SchemaNode.Primitive(PrimitiveType.String, None), SchemaNode.Primitive(PrimitiveType.Integer, Some "int32") -> ()
                    | other1, other2 -> failtestf "Expected String and Integer, got %A and %A" other1 other2
                | other -> failtestf "Expected Object for anonymous record, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "nested anonymous record produces nested inline Objects" {
            let doc = analyze<RecWithNestedAnonRecord>
            match doc.Root with
            | SchemaNode.Object obj ->
                let dataProp = obj.Properties |> List.find (fun p -> p.Name = "data")
                match dataProp.Schema with
                | SchemaNode.Object outerAnon ->
                    Expect.isNone outerAnon.TypeId "Outer anonymous record should have no TypeId"
                    let innerProp = outerAnon.Properties |> List.find (fun p -> p.Name = "inner")
                    match innerProp.Schema with
                    | SchemaNode.Object innerAnon ->
                        Expect.isNone innerAnon.TypeId "Inner anonymous record should have no TypeId"
                        Expect.equal innerAnon.Properties.Length 1 "Inner should have 1 property"
                    | other -> failtestf "Expected nested Object, got %A" other
                | other -> failtestf "Expected Object, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "anonymous record with optional field produces Nullable" {
            let doc = analyze<RecWithOptionalAnonField>
            match doc.Root with
            | SchemaNode.Object obj ->
                let infoProp = obj.Properties |> List.find (fun p -> p.Name = "info")
                match infoProp.Schema with
                | SchemaNode.Object anonObj ->
                    let ageProp = anonObj.Properties |> List.find (fun p -> p.Name = "age")
                    match ageProp.Schema with
                    | SchemaNode.Nullable (SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")) -> ()
                    | other -> failtestf "Expected Nullable Integer, got %A" other
                    Expect.equal anonObj.Required ["name"] "Only non-option field should be required"
                | other -> failtestf "Expected Object, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }

        test "anonymous record in collection produces inline Object in Array" {
            let doc = analyze<RecWithAnonInCollection>
            match doc.Root with
            | SchemaNode.Object obj ->
                let itemsProp = obj.Properties |> List.find (fun p -> p.Name = "items")
                match itemsProp.Schema with
                | SchemaNode.Array itemSchema ->
                    match itemSchema with
                    | SchemaNode.Object anonObj ->
                        Expect.isNone anonObj.TypeId "Anonymous record in array should have no TypeId"
                        Expect.equal anonObj.Properties.Length 2 "Should have 2 properties"
                    | other -> failtestf "Expected Object in array, got %A" other
                | other -> failtestf "Expected Array, got %A" other
            | other -> failtestf "Expected Object, got %A" other
        }
    ]

[<Tests>]
let duEncodingTests =
    testList "DU encoding styles" [
        test "InternalTag: discriminator + fields in same object" {
            let config = { SchemaGeneratorConfig.defaults with UnionEncoding = UnionEncodingStyle.InternalTag }
            let doc = analyzeWith<TestDUForEncoding> config
            match doc.Root with
            | SchemaNode.AnyOf cases ->
                Expect.equal cases.Length 3 "Should have 3 cases"
                // Check MultiField case has discriminator + fields
                let multiFieldDef = doc.Definitions |> List.find (fun (name, _) -> name = "MultiField") |> snd
                match multiFieldDef with
                | SchemaNode.Object obj ->
                    Expect.isTrue (obj.Properties |> List.exists (fun p -> p.Name = "kind")) "Should have discriminator property"
                    Expect.isTrue (obj.Properties |> List.exists (fun p -> p.Name = "name")) "Should have name field"
                    Expect.isTrue (obj.Properties |> List.exists (fun p -> p.Name = "count")) "Should have count field"
                    Expect.equal obj.Properties.Length 3 "Should have 3 properties (discriminator + 2 fields)"
                | other -> failtestf "Expected Object, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "AdjacentTag: separate tag and fields properties" {
            let config = { SchemaGeneratorConfig.defaults with UnionEncoding = UnionEncodingStyle.AdjacentTag }
            let doc = analyzeWith<TestDUForEncoding> config
            match doc.Root with
            | SchemaNode.AnyOf cases ->
                Expect.equal cases.Length 3 "Should have 3 cases"
                // Check MultiField case has kind + fields structure
                let multiFieldDef = doc.Definitions |> List.find (fun (name, _) -> name = "MultiField") |> snd
                match multiFieldDef with
                | SchemaNode.Object obj ->
                    Expect.equal obj.Properties.Length 2 "Should have 2 properties (kind + fields)"
                    let tagProp = obj.Properties |> List.find (fun p -> p.Name = "kind")
                    let fieldsProp = obj.Properties |> List.find (fun p -> p.Name = "fields")
                    match fieldsProp.Schema with
                    | SchemaNode.Object fieldsObj ->
                        Expect.equal fieldsObj.Properties.Length 2 "Fields object should have 2 properties"
                    | other -> failtestf "Expected Object for fields, got %A" other
                | other -> failtestf "Expected Object, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "ExternalTag: case name as property key" {
            let config = { SchemaGeneratorConfig.defaults with UnionEncoding = UnionEncodingStyle.ExternalTag }
            let doc = analyzeWith<TestDUForEncoding> config
            match doc.Root with
            | SchemaNode.AnyOf cases ->
                Expect.equal cases.Length 3 "Should have 3 cases"
                // Check MultiField case wraps fields in case name property
                let multiFieldDef = doc.Definitions |> List.find (fun (name, _) -> name = "MultiField") |> snd
                match multiFieldDef with
                | SchemaNode.Object obj ->
                    Expect.equal obj.Properties.Length 1 "Should have 1 property (case name)"
                    let caseProp = obj.Properties |> List.head
                    Expect.equal caseProp.Name "MultiField" "Property name should be case name"
                    match caseProp.Schema with
                    | SchemaNode.Object fieldsObj ->
                        Expect.equal fieldsObj.Properties.Length 2 "Should have 2 field properties"
                    | other -> failtestf "Expected Object for case value, got %A" other
                | other -> failtestf "Expected Object, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "Untagged: no discriminator, just fields" {
            let config = { SchemaGeneratorConfig.defaults with UnionEncoding = UnionEncodingStyle.Untagged }
            let doc = analyzeWith<TestDUForEncoding> config
            match doc.Root with
            | SchemaNode.AnyOf cases ->
                Expect.equal cases.Length 3 "Should have 3 cases"
                // Check MultiField case has only fields (no discriminator)
                let multiFieldDef = doc.Definitions |> List.find (fun (name, _) -> name = "MultiField") |> snd
                match multiFieldDef with
                | SchemaNode.Object obj ->
                    Expect.equal obj.Properties.Length 2 "Should have 2 properties (only fields, no discriminator)"
                    Expect.isFalse (obj.Properties |> List.exists (fun p -> p.Name = "kind")) "Should not have discriminator"
                | other -> failtestf "Expected Object, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }

        test "Attribute override: per-type attribute overrides config" {
            // Config says InternalTag, but attribute says AdjacentTag
            let config = { SchemaGeneratorConfig.defaults with UnionEncoding = UnionEncodingStyle.InternalTag }
            let doc = analyzeWith<TestDUWithAttributeOverride> config
            match doc.Root with
            | SchemaNode.AnyOf cases ->
                // Check Case2 uses AdjacentTag (tag + fields structure)
                let case2Def = doc.Definitions |> List.find (fun (name, _) -> name = "Case2") |> snd
                match case2Def with
                | SchemaNode.Object obj ->
                    // AdjacentTag should have 2 properties: kind and fields
                    Expect.equal obj.Properties.Length 2 "AdjacentTag should have kind + fields"
                    let hasKind = obj.Properties |> List.exists (fun p -> p.Name = "kind")
                    let hasFields = obj.Properties |> List.exists (fun p -> p.Name = "fields")
                    Expect.isTrue hasKind "Should have kind property (AdjacentTag)"
                    Expect.isTrue hasFields "Should have fields property (AdjacentTag)"
                | other -> failtestf "Expected Object, got %A" other
            | other -> failtestf "Expected AnyOf, got %A" other
        }
    ]
