namespace FSharp.Data.JsonSchema

open System
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open Newtonsoft.Json.Linq
open Newtonsoft.Json.FSharp.Idiomatic
open Newtonsoft.Json.Serialization
open NJsonSchema
open NJsonSchema.Generation

type OptionSchemaProcessor() =
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

type SingleCaseDuSchemaProcessor() =

    member this.Process(context:SchemaProcessorContext) =
        if FSharpType.IsUnion(context.Type)
           && Reflection.allCasesEmpty context.Type then
            let schema = context.Schema
            let cases = FSharpType.GetUnionCases(context.Type)
            schema.Type <- JsonObjectType.String
            for case in cases do
                schema.Enumeration.Add(case.Name)
                schema.EnumerationNames.Add(case.Name)

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)

type MultiCaseDuSchemaProcessor(?casePropertyName) =
    let casePropertyName = defaultArg casePropertyName "kind"

    member this.Process(context:SchemaProcessorContext) =
        if FSharpType.IsUnion(context.Type)
           && not (Reflection.allCasesEmpty context.Type)
           && not (Reflection.isList context.Type)
           && not (Reflection.isOption context.Type) then
            let cases = FSharpType.GetUnionCases(context.Type)
            let schema = context.Schema
            schema.Type <- JsonObjectType.Object
            for case in cases do
                let propSchema = JsonSchema(Type=JsonObjectType.Object)
                propSchema.Properties.Add(casePropertyName, JsonSchemaProperty(Type=JsonObjectType.String))
                let fields = case.GetFields()
                for field in fields do
                    let fieldSchema = context.Generator.Generate(field.PropertyType)
                    propSchema.Properties.Add(field.Name, JsonSchemaProperty(Type=fieldSchema.Type))
                schema.AnyOf.Add(propSchema)

    interface ISchemaProcessor with
        member this.Process(context) = this.Process(context)

[<AbstractClass; Sealed>]
type Generator private () =
    static let cache = Collections.Concurrent.ConcurrentDictionary<string * Type, JsonSchema>()

    static member internal CreateInternal(?casePropertyName) =
        let settings = JsonSchemaGeneratorSettings(SerializerSettings=FSharp.Data.Json.DefaultSettings)
        settings.SchemaProcessors.Add(OptionSchemaProcessor())
        settings.SchemaProcessors.Add(SingleCaseDuSchemaProcessor())
        settings.SchemaProcessors.Add(MultiCaseDuSchemaProcessor(?casePropertyName=casePropertyName))
        fun ty -> JsonSchema.FromType(ty, settings)

    /// Creates a generator using the specified casePropertyName and generationProviders.
    static member Create(?casePropertyName) =
        Generator.CreateInternal(?casePropertyName=casePropertyName)

    /// Creates a memoized generator that stores generated schemas in a global cache by Type.
    static member CreateMemoized(?casePropertyName) =
        let casePropertyName = defaultArg casePropertyName FSharp.Data.Json.DefaultCasePropertyName
        fun ty ->
            cache.GetOrAdd((casePropertyName, ty),
                let generator = Generator.CreateInternal(casePropertyName)
                generator ty)

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
