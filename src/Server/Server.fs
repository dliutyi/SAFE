open System.IO
open System.Threading.Tasks

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open FSharp.Control.Tasks.V2
open Giraffe
open Saturn
open Shared
open OrderActor


let tryGetEnv key = 
    match Environment.GetEnvironmentVariable key with
    | x when String.IsNullOrWhiteSpace x -> None 
    | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let apiRouter (actor: IOrderActor) = router {
    not_found_handler (setStatusCode 404 >=> text "Not Found (404)")
    get "/api/orders" (fun next ctx ->
        task {
            printfn "Try get order ids"
            let! result = actor.GetOrderIDs()
            match result with
            | GetOrderIDsResult.Successful orderIds ->
                return! json orderIds next ctx
            | GetOrderIDsResult.Failed ->
                ctx.SetStatusCode 400
                return Some ctx
        })
    post "/api/order" (fun next ctx ->
        task {
            use stream = new StreamReader(ctx.Request.Body)
            let! orderId = stream.ReadToEndAsync()
            printfn "We've received %s" orderId

            let! result = actor.PostOrderID orderId
            match result with
            | PostOrderIDResult.Successful ->
                return! text "Ok" next ctx
            | PostOrderIDResult.Failed ->
                ctx.SetStatusCode 400
                return Some ctx
        })
}

let webApp actor = router {
    forward "" (apiRouter actor)
}

let app actor = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router (webApp actor)
    memory_cache
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip
}

let actor = orderActor()
app actor |> run
