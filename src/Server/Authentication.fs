module Authentication

open FSharp.Control.Tasks.V2
open Giraffe
open Microsoft.AspNetCore.Http
open Saturn
open System
open System.Security.Claims
open System.IdentityModel.Tokens.Jwt
open Shared

let Secret = "SUPER_SECRET_SECRET"
let Issuer = "SAFE"

let algorithm = Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256

let generateToken str =
    [ Claim(JwtRegisteredClaimNames.Sub, str);
      Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) ]
    |> Saturn.Auth.generateJWT (Secret, algorithm) Issuer (DateTime.UtcNow.AddHours(ConfigHelper.config.Value.TokenExpiration))



let auth (next: HttpFunc) (ctx: HttpContext) = task {
    let! login = ctx.BindJsonAsync<Shared.Auth>()
    printfn "Try to authenticate %A" login
    return!
        if login.AUTHSTR = ConfigHelper.config.Value.SecretString then
            let data = { Token = generateToken login.AUTHSTR }
            ctx.WriteJsonAsync data
        else
            Response.unauthorized ctx "Bearer" "" (sprintf "Can't be logged in.")
}