module FSharp.Data.JsonSchema.OpenApi.Tests.EndToEndTests

open Expecto
open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting.Server
open Microsoft.AspNetCore.Hosting.Server.Features
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open FSharp.Data.JsonSchema.OpenApi

/// Matches the exact repro from GitHub issue #30.
type Shape =
    | Circle of radius: float
    | Rectangle of width: float * height: float
    | Point

/// Self-recursive DU, matching the existing TreeNode pattern used elsewhere in this suite.
type TreeNode =
    | Leaf of int
    | Branch of TreeNode * TreeNode

/// A second, differently-shaped self-recursive type, used to prove two
/// self-recursive types in the same document don't collide on component id.
type LinkedNode =
    | Empty
    | Node of value: int * next: LinkedNode

/// Shares a case name ("Leaf") with TreeNode, to prove case-name collisions
/// across different types are resolved by qualifying component ids with the
/// owning type's name.
type Plant =
    | Leaf of species: string
    | Root of Plant

let private startApp (mapEndpoints: WebApplication -> unit) : WebApplication =
    let builder = WebApplication.CreateBuilder()
    builder.Logging.ClearProviders() |> ignore
    builder.WebHost.UseUrls("http://127.0.0.1:0") |> ignore
    builder.Services.ConfigureHttpJsonOptions(fun options ->
        JsonFSharpOptions.Default().AddToJsonSerializerOptions options.SerializerOptions
    ) |> ignore
    builder.Services.AddOpenApi(fun options ->
        options.AddSchemaTransformer(FSharpSchemaTransformer()) |> ignore
    ) |> ignore
    let app = builder.Build()
    try
        app.MapOpenApi() |> ignore
        mapEndpoints app
        app.StartAsync().GetAwaiter().GetResult()
        app
    with _ ->
        app.DisposeAsync().AsTask().GetAwaiter().GetResult()
        reraise ()

let private stopApp (app: WebApplication) : unit =
    app.StopAsync().GetAwaiter().GetResult()
    app.DisposeAsync().AsTask().GetAwaiter().GetResult()

let private baseAddress (app: WebApplication) : string =
    let server = app.Services.GetRequiredService<IServer>()
    let feature = server.Features.Get<IServerAddressesFeature>()
    feature.Addresses |> Seq.head

let private getOpenApiDocument (app: WebApplication) : System.Net.HttpStatusCode * string =
    use client = new HttpClient()
    let response = client.GetAsync(baseAddress app + "/openapi/v1.json").GetAwaiter().GetResult()
    let body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
    (response.StatusCode, body)

let private hasSchema (root: JsonElement) (name: string) : bool =
    match root.TryGetProperty "components" with
    | false, _ -> false
    | true, components ->
        match components.TryGetProperty "schemas" with
        | false, _ -> false
        | true, schemas -> schemas.TryGetProperty name |> fst

/// Matches the exact repro from GitHub issue #29.
type WeatherForecast =
    { Date: System.DateTime
      TemperatureC: int
      shape: Shape
      Summary: string option }

[<Tests>]
let endToEndTests =
    testList "endToEnd" [
        test "OpenAPI document generation inlines DateTime fields instead of a dangling $ref" {
            let app =
                startApp (fun app ->
                    app.MapGet(
                        "/weather",
                        System.Func<WeatherForecast>(fun () ->
                            { Date = System.DateTime.Now; TemperatureC = 1; shape = Point; Summary = None })
                    )
                    |> ignore
                )
            try
                let (status, body) = getOpenApiDocument app
                Expect.equal status System.Net.HttpStatusCode.OK "200 OK"
                use jsonDoc = JsonDocument.Parse body
                let root = jsonDoc.RootElement
#if NET10_0_OR_GREATER
                Expect.isFalse (hasSchema root "WeatherForecast.DateTime") "DateTime must not be registered as its own component"
#endif
                let dateSchema =
                    root
                        .GetProperty("components")
                        .GetProperty("schemas")
                        .GetProperty("WeatherForecast")
                        .GetProperty("properties")
                        .GetProperty("date")
                Expect.equal (dateSchema.GetProperty("type").GetString()) "string" "date field inlines as a string"
                Expect.equal (dateSchema.GetProperty("format").GetString()) "date-time" "date field keeps the date-time format"
                Expect.isFalse (dateSchema.TryGetProperty("$ref") |> fst) "date field must not be a $ref"
            finally
                stopApp app
        }
        test "OpenAPI document generation succeeds for a discriminated union response" {
            let app = startApp (fun app -> app.MapGet("/shape", System.Func<Shape>(fun () -> Point)) |> ignore)
            try
                let (status, body) = getOpenApiDocument app
                Expect.equal status System.Net.HttpStatusCode.OK "200 OK, not the InvalidOperationException from #30"
                use jsonDoc = JsonDocument.Parse body
#if NET10_0_OR_GREATER
                let root = jsonDoc.RootElement
                Expect.isTrue (hasSchema root "Shape.Circle") "Circle registered as a component schema, qualified by its type"
                Expect.isTrue (hasSchema root "Shape.Rectangle") "Rectangle registered as a component schema, qualified by its type"
                Expect.isTrue (hasSchema root "Shape.Point") "Point registered as a component schema, qualified by its type"
#endif
                ()
            finally
                stopApp app
        }

        test "OpenAPI document generation succeeds for a self-recursive discriminated union" {
            let app = startApp (fun app -> app.MapGet("/tree", System.Func<TreeNode>(fun () -> TreeNode.Leaf 1)) |> ignore)
            try
                let (status, body) = getOpenApiDocument app
                Expect.equal status System.Net.HttpStatusCode.OK "200 OK, not the InvalidOperationException from #30"
                use jsonDoc = JsonDocument.Parse body
#if NET10_0_OR_GREATER
                let root = jsonDoc.RootElement
                Expect.isTrue (hasSchema root "TreeNode.Leaf") "Leaf registered as a component schema, qualified by its type"
                Expect.isTrue (hasSchema root "TreeNode.Branch") "Branch registered as a component schema, qualified by its type"
                Expect.isTrue (hasSchema root "TreeNode") "TreeNode root registered as a component schema (self-ref target)"
#endif
                ()
            finally
                stopApp app
        }

        test "OpenAPI document generation registers distinct components for two different self-recursive types" {
            let app =
                startApp (fun app ->
                    app.MapGet("/tree", System.Func<TreeNode>(fun () -> TreeNode.Leaf 1)) |> ignore
                    app.MapGet("/linked", System.Func<LinkedNode>(fun () -> Empty)) |> ignore
                )
            try
                let (status, body) = getOpenApiDocument app
                Expect.equal status System.Net.HttpStatusCode.OK "200 OK, not the InvalidOperationException from #30"
                use jsonDoc = JsonDocument.Parse body
#if NET10_0_OR_GREATER
                let root = jsonDoc.RootElement
                Expect.isTrue (hasSchema root "TreeNode") "TreeNode root registered as a component schema"
                Expect.isTrue (hasSchema root "TreeNode.Leaf") "Leaf registered as a component schema, qualified by its type"
                Expect.isTrue (hasSchema root "TreeNode.Branch") "Branch registered as a component schema, qualified by its type"
                Expect.isTrue (hasSchema root "LinkedNode") "LinkedNode root registered as a component schema"
                Expect.isTrue (hasSchema root "LinkedNode.Empty") "Empty registered as a component schema, qualified by its type"
                Expect.isTrue (hasSchema root "LinkedNode.Node") "Node registered as a component schema, qualified by its type"
#endif
                ()
            finally
                stopApp app
        }

        test "OpenAPI document generation disambiguates two different types sharing a case name" {
            let app =
                startApp (fun app ->
                    app.MapGet("/tree", System.Func<TreeNode>(fun () -> TreeNode.Leaf 1)) |> ignore
                    app.MapGet("/plant", System.Func<Plant>(fun () -> Plant.Leaf "fern")) |> ignore
                )
            try
                let (status, body) = getOpenApiDocument app
                Expect.equal status System.Net.HttpStatusCode.OK "200 OK, not the InvalidOperationException from #30"
                use jsonDoc = JsonDocument.Parse body
#if NET10_0_OR_GREATER
                let root = jsonDoc.RootElement
                // TreeNode and Plant both have a case named "Leaf" — proving neither
                // silently overwrites the other's component is the point of this test.
                Expect.isTrue (hasSchema root "TreeNode.Leaf") "TreeNode's Leaf case registered under its own qualified id"
                Expect.isTrue (hasSchema root "Plant.Leaf") "Plant's Leaf case registered under its own qualified id"
#endif
                ()
            finally
                stopApp app
        }
    ]
