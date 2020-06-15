module ConfigHelper

open Thoth.Json.Net

type Config = {
    SecretString: string
    TokenExpiration: float
}

let config = lazy ("config.json" |> System.IO.File.ReadAllText |> Decode.Auto.unsafeFromString<Config>)