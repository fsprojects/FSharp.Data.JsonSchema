module FSharp.Data.JsonSchema.OpenApi.Tests.TranslatorTests

open Expecto
open FSharp.Data.JsonSchema.Core
open FSharp.Data.JsonSchema.OpenApi

#if NET10_0_OR_GREATER
open Microsoft.OpenApi
type OASchema = Microsoft.OpenApi.OpenApiSchema
#else
open Microsoft.OpenApi.Models
type OASchema = Microsoft.OpenApi.Models.OpenApiSchema
#endif

let private translate doc : OASchema * Map<string, OASchema> =
    OpenApiSchemaTranslator.translate doc

let private simpleDoc root : SchemaDocument =
    { Root = root; Definitions = [] }

[<Tests>]
let primitiveTests =
    testList "translator/primitives" [
        test "Primitive String translates to string type" {
            let (schema: OASchema, _) = translate (simpleDoc (SchemaNode.Primitive(PrimitiveType.String, None)))
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.String)) "string type"
#else
            Expect.equal schema.Type "string" "string type"
#endif
        }

        test "Primitive Integer int32 translates with format" {
            let (schema: OASchema, _) = translate (simpleDoc (SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")))
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.Integer)) "integer type"
#else
            Expect.equal schema.Type "integer" "integer type"
#endif
            Expect.equal schema.Format "int32" "int32 format"
        }

        test "Primitive Boolean translates correctly" {
            let (schema: OASchema, _) = translate (simpleDoc (SchemaNode.Primitive(PrimitiveType.Boolean, None)))
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.Boolean)) "boolean type"
#else
            Expect.equal schema.Type "boolean" "boolean type"
#endif
        }
    ]

[<Tests>]
let objectTests =
    testList "translator/object" [
        test "Object translates to object type with properties" {
            let obj = SchemaNode.Object {
                Properties = [
                    { Name = "name"; Schema = SchemaNode.Primitive(PrimitiveType.String, None); Description = None }
                    { Name = "age"; Schema = SchemaNode.Primitive(PrimitiveType.Integer, Some "int32"); Description = None }
                ]
                Required = ["name"; "age"]
                AdditionalProperties = false
                TypeId = None
                Description = None
                Title = None
            }
            let (schema: OASchema, _) = translate (simpleDoc obj)
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.Object)) "object type"
#else
            Expect.equal schema.Type "object" "object type"
#endif
            Expect.equal schema.Properties.Count 2 "2 properties"
            Expect.isTrue (schema.Properties.ContainsKey("name")) "has name"
            Expect.isTrue (schema.Properties.ContainsKey("age")) "has age"
            Expect.isTrue (schema.Required.Contains("name")) "name required"
            Expect.isTrue (schema.Required.Contains("age")) "age required"
        }
    ]

[<Tests>]
let arrayTests =
    testList "translator/array" [
        test "Array translates to array type with items" {
            let arr = SchemaNode.Array(SchemaNode.Primitive(PrimitiveType.String, None))
            let (schema: OASchema, _) = translate (simpleDoc arr)
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.Array)) "array type"
#else
            Expect.equal schema.Type "array" "array type"
#endif
            Expect.isNotNull schema.Items "has items"
        }
    ]

[<Tests>]
let anyOfTests =
    testList "translator/anyOf" [
        test "AnyOf translates to anyOf collection" {
            let anyOf = SchemaNode.AnyOf [
                SchemaNode.Primitive(PrimitiveType.String, None)
                SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")
            ]
            let (schema: OASchema, _) = translate (simpleDoc anyOf)
            Expect.equal schema.AnyOf.Count 2 "2 anyOf entries"
        }
    ]

[<Tests>]
let nullableTests =
    testList "translator/nullable" [
        test "Nullable wraps inner schema" {
            let nullable = SchemaNode.Nullable(SchemaNode.Primitive(PrimitiveType.String, None))
            let (schema: OASchema, _) = translate (simpleDoc nullable)
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.String ||| JsonSchemaType.Null)) "nullable string"
#else
            Expect.isTrue schema.Nullable "nullable flag"
            Expect.equal schema.Type "string" "string type"
#endif
        }
    ]

[<Tests>]
let enumTests =
    testList "translator/enum" [
        test "Enum translates to string type with enum values" {
            let enum = SchemaNode.Enum(["A"; "B"; "C"], PrimitiveType.String)
            let (schema: OASchema, _) = translate (simpleDoc enum)
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.String)) "string type"
#else
            Expect.equal schema.Type "string" "string type"
#endif
            Expect.equal schema.Enum.Count 3 "3 enum values"
        }
    ]

[<Tests>]
let constTests =
    testList "translator/const" [
        test "Const translates to single-value enum with default" {
            let c = SchemaNode.Const("Hello", PrimitiveType.String)
            let (schema: OASchema, _) = translate (simpleDoc c)
            Expect.equal schema.Enum.Count 1 "1 enum value"
            Expect.isNotNull schema.Default "has default"
        }
    ]

[<Tests>]
let anyTests =
    testList "translator/any" [
        test "Any translates to empty schema" {
            let (schema: OASchema, _) = translate (simpleDoc SchemaNode.Any)
            Expect.equal schema.Properties.Count 0 "no properties"
            Expect.equal schema.AnyOf.Count 0 "no anyOf"
        }
    ]

[<Tests>]
let refTests =
    testList "translator/ref" [
        test "Ref produces reference to component schema" {
            let doc = {
                Root = SchemaNode.Object {
                    Properties = [
                        { Name = "child"; Schema = SchemaNode.Ref "Child"; Description = None }
                    ]
                    Required = ["child"]
                    AdditionalProperties = false
                    TypeId = None
                    Description = None
                    Title = None
                }
                Definitions = [
                    "Child", SchemaNode.Object {
                        Properties = [
                            { Name = "value"; Schema = SchemaNode.Primitive(PrimitiveType.String, None); Description = None }
                        ]
                        Required = ["value"]
                        AdditionalProperties = false
                        TypeId = None
                        Description = None
                        Title = None
                    }
                ]
            }
            let (_schema: OASchema, components) = translate doc
            Expect.isTrue (components.ContainsKey "Child") "Child in components"
        }
    ]

[<Tests>]
let mapTests =
    testList "translator/map" [
        test "Map translates to object with additionalProperties" {
            let m = SchemaNode.Map(SchemaNode.Primitive(PrimitiveType.String, None))
            let (schema: OASchema, _) = translate (simpleDoc m)
#if NET10_0_OR_GREATER
            Expect.equal schema.Type (System.Nullable(JsonSchemaType.Object)) "object type"
#else
            Expect.equal schema.Type "object" "object type"
#endif
            Expect.isNotNull schema.AdditionalProperties "has additional properties schema"
        }
    ]

[<Tests>]
let definitionsTests =
    testList "translator/definitions" [
        test "definitions produce component schemas" {
            let doc = {
                Root = SchemaNode.AnyOf [SchemaNode.Ref "A"; SchemaNode.Ref "B"]
                Definitions = [
                    "A", SchemaNode.Primitive(PrimitiveType.String, None)
                    "B", SchemaNode.Primitive(PrimitiveType.Integer, Some "int32")
                ]
            }
            let (_, components) = translate doc
            Expect.equal components.Count 2 "2 component schemas"
            Expect.isTrue (components.ContainsKey "A") "has A"
            Expect.isTrue (components.ContainsKey "B") "has B"
        }
    ]
