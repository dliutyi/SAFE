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
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt

let tryGetEnv key = 
    match Environment.GetEnvironmentVariable key with
    | x when String.IsNullOrWhiteSpace x -> None 
    | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let port =
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let secret = "SUPER_SECRET_SECRET"
let issuer = "SAFE"
let algorithm = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256

let generateToken str =
    [ Claim(JwtRegisteredClaimNames.Sub, str);
      Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) ]
    |> Saturn.Auth.generateJWT (secret, algorithm) issuer (DateTime.UtcNow.AddHours(1.0))

type JWT = string
type Auth =
    {
        AUTHSTR: string
    }
    member this.IsValid() = this.AUTHSTR = "SECRET_STRING"

type AuthData = { Token: JWT }

let apiRouter (actor: IOrderActor) =
    let secured = router {
        pipe_through (Saturn.Auth.requireAuthentication Saturn.ChallengeType.JWT)
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

    router {
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
        post "/api/auth" (fun _next ctx ->
            task {
                let! login = ctx.BindJsonAsync<Auth>()
                printfn "Try to authenticate %A" login
                return!
                    if login.IsValid() then
                        let data = { Token = generateToken login.AUTHSTR }
                        ctx.WriteJsonAsync data
                    else
                        Response.unauthorized ctx "Bearer" "" (sprintf "Can't be logged in.")
            })
        forward "" secured
    }

let webApp actor = router {
    forward "" (apiRouter actor)
}

let app actor = application {
    url ("http://0.0.0.0:" + port.ToString() + "/")
    use_router (webApp actor)
    memory_cache
    use_jwt_authentication secret issuer
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip
}

let actor = orderActor()
app actor |> run
