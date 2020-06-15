open Giraffe
open Saturn
open System
open System.IO
open OrderActor

let tryGetEnv key = 
    match Environment.GetEnvironmentVariable key with
    | x when String.IsNullOrWhiteSpace x -> None 
    | x -> Some x

let publicPath = Path.GetFullPath "../Client/public"

let port = 
    "SERVER_PORT"
    |> tryGetEnv |> Option.map uint16 |> Option.defaultValue 8085us

let apiRouter (actor: IOrderActor) =
    let secured = router {
        pipe_through (Saturn.Auth.requireAuthentication Saturn.ChallengeType.JWT)
        post "/api/orders" (Orders.PostOrders actor)
    }

    router {
        not_found_handler (setStatusCode 404 >=> text "Not Found (404)")
        get "/api/orders" (Orders.GetOrders actor)
        post "/api/auth" Authentication.auth
        forward "" secured
    }

let webApp actor = router {
    forward "" (apiRouter actor)
}

let app actor = application {
    url ("https://0.0.0.0:" + port.ToString() + "/")
    force_ssl
    use_router (webApp actor)
    memory_cache
    use_jwt_authentication Authentication.Secret Authentication.Issuer
    use_static publicPath
    use_json_serializer(Thoth.Json.Giraffe.ThothSerializer())
    use_gzip

}

let actor = orderActor()
app actor |> run
