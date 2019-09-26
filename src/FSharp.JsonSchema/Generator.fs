module FSharp.JsonSchema.Generator

open System
open System.Collections.Generic
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Schema

type OptionGenerationProvider() =
    inherit Generation.JSchemaGenerationProvider()

    override __.GetSchema(context:Generation.JSchemaTypeGenerationContext) =
        let unionType = context.ObjectType
        let cases = FSharpType.GetUnionCases(unionType)
        let schema = JSchema(Type=Nullable JSchemaType.Object)
        for case in cases do
            match case.Name with
            | "None" ->
                schema.AnyOf.Add(JSchema(Type=Nullable JSchemaType.Null))
            | _ ->
                let field = case.GetFields() |> Array.head
                let propSchema = context.Generator.Generate(field.PropertyType)
                schema.AnyOf.Add(propSchema)
        schema

type MultiCaseDuGenerationProvider(?casePropertyName) =
    inherit Generation.JSchemaGenerationProvider()

    let casePropertyName = defaultArg casePropertyName "kind"

    override __.GetSchema(context:Generation.JSchemaTypeGenerationContext) =
        let unionType = context.ObjectType
        let cases = FSharpType.GetUnionCases(unionType)
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

let generate casePropertyName (ty:Type) =
    let generator =
        Generation.JSchemaGenerator(
            ContractResolver=Serialization.CamelCasePropertyNamesContractResolver())

    // Create generation providers for various types, e.g. Option and MultiCaseDu?
    generator.GenerationProviders.Add(Generation.StringEnumGenerationProvider())
    generator.GenerationProviders.Add(OptionGenerationProvider())
    generator.GenerationProviders.Add(MultiCaseDuGenerationProvider(casePropertyName))
