namespace FSharp.Data.JsonSchema

open System
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open Newtonsoft.Json.Linq
open Newtonsoft.Json.FSharp.Idiomatic
open Newtonsoft.Json.Serialization
open NJsonSchema
open NJsonSchema.Generation

type internal OptionSchemaProcessor() =
    static let optionTy = typedefof<option<_>>

    member this.Process(context:SchemaProcessorContext) =
        if context.Type.IsGenericType
           && optionTy.Equals(context.Type.GetGenericTypeDefinition()) then
            let schema = context.Schema
            let cases = FSharpType.GetUnionCases(context.Type)
            let schemaType =
                [|for case in cases do
                    match case.Name with
                    | "None" ->
                        yield JsonObjectType.Null
                    | _ ->
                        let field = case.GetFields() |> Array.head
                        let schema = context.Generator.Generate(field.PropertyType)
                        match schema.Type with
                        | JsonObjectType.None ->
                            yield JsonObjectType.String |||
                                  JsonObjectType.Number |||
                                  JsonObjectType.Integer |||
                                  JsonObjectType.Boolean |||
                                  JsonObjectType.Object |||
                                  JsonObjectType.Array
                        | ty -> yield ty|]
                |> Array.reduce (|||)
            schema.Type <- schemaType

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)

(*
open Newtonsoft.Json.Schema
open Newtonsoft.Json.Schema.Generation

type internal OptionGenerationProvider() =
    inherit JSchemaGenerationProvider()

    static let optionTy = typedefof<option<_>>

    override __.CanGenerateSchema(context:JSchemaTypeGenerationContext) =
        context.ObjectType.IsGenericType
        && optionTy.Equals(context.ObjectType.GetGenericTypeDefinition())

    override __.GetSchema(context:JSchemaTypeGenerationContext) =
        let cases = FSharpType.GetUnionCases(context.ObjectType)
        let schemaType =
            [|for case in cases do
                match case.Name with
                | "None" ->
                    yield JSchemaType.Null
                | _ ->
                    let field = case.GetFields() |> Array.head
                    let propSchema = context.Generator.Generate(field.PropertyType)
                    if propSchema.Type.HasValue then
                        // Use the generator to produce a schema for the
                        // contained type and use it's schema type.
                        yield propSchema.Type.Value
                    else
                        // Use None to represent an unspecified type (e.g. generic or unit)
                        yield JSchemaType.None|]
            |> Array.reduce (|||)
        JSchema(Type=Nullable schemaType)

type internal SingleCaseDuGenerationProvider() =
    inherit JSchemaGenerationProvider()

    override __.CanGenerateSchema(context:JSchemaTypeGenerationContext) =
        FSharpType.IsUnion(context.ObjectType)
        && Reflection.allCasesEmpty context.ObjectType

    override __.GetSchema(context:JSchemaTypeGenerationContext) =
        let cases = FSharpType.GetUnionCases(context.ObjectType)
        let schema = JSchema(Type=Nullable JSchemaType.String)
        for case in cases do
            schema.Enum.Add(JValue case.Name)
        schema

type internal MultiCaseDuGenerationProvider(?casePropertyName) =
    inherit JSchemaGenerationProvider()

    let casePropertyName = defaultArg casePropertyName "kind"

    override __.CanGenerateSchema(context:JSchemaTypeGenerationContext) =
        FSharpType.IsUnion(context.ObjectType)
        && not (Reflection.allCasesEmpty context.ObjectType)
        && not (Reflection.isList context.ObjectType)
        && not (Reflection.isOption context.ObjectType)

    override __.GetSchema(context:JSchemaTypeGenerationContext) =
        let cases = FSharpType.GetUnionCases(context.ObjectType)
        let schema = JSchema(Type=Nullable JSchemaType.Object)
        for case in cases do
            let propSchema = JSchema(Type=Nullable JSchemaType.Object)
            propSchema.Properties.Add(casePropertyName, JSchema(Type=Nullable JSchemaType.String))
            let fields = case.GetFields()
            for field in fields do
                let fieldSchema = context.Generator.Generate(field.PropertyType)
                propSchema.Properties.Add(KeyValuePair(field.Name, fieldSchema))
            schema.AnyOf.Add(propSchema)
        schema
*)

[<AbstractClass; Sealed>]
type Generator private () =
    static let cache = Collections.Concurrent.ConcurrentDictionary<string * Type, JsonSchema>()
    static let settings =
        let s = JsonSchemaGeneratorSettings(SerializerSettings=FSharp.Data.Json.DefaultSettings)
        s.SchemaProcessors.Add(OptionSchemaProcessor())
        s

    /// Creates a generator using the specified casePropertyName and generationProviders.
    static member Create(?casePropertyName) =
        fun ty -> JsonSchema.FromType(ty, settings)

    /// Creates a memoized generator that stores generated schemas in a global cache by Type.
    static member CreateMemoized(?casePropertyName) =
        let casePropertyName = defaultArg casePropertyName FSharp.Data.Json.DefaultCasePropertyName
        fun ty ->
            cache.GetOrAdd((casePropertyName, ty), JsonSchema.FromType(ty, settings))

module Validation =

    let validate schema (json:string) =
        let validator = Validation.JsonSchemaValidator()
        let errors = validator.Validate(json, schema)
        if errors.Count > 0 then Error(Seq.toArray errors) else Ok()

    type FSharp.Data.Json with

        static member ParseWithValidation<'T>(json, schema) =
            validate schema json
            |> Result.map (fun _ ->
                FSharp.Data.Json.Parse<'T> json)

        static member ParseWithValidation<'T>(json, schema, casePropertyName) =
            validate schema json
            |> Result.map (fun _ ->
                FSharp.Data.Json.Parse<'T>(json, casePropertyName))
