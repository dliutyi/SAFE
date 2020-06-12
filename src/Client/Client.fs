module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fable.Core.JsInterop
open Fetch.Types
open Thoth.Fetch
open Fulma
open Thoth.Json
open Fable.Core
open Fable.FontAwesome

type Model = {
    OrderIDs: string list
    SecretString: string
    Token: string
}

type Auth = {
    AUTHSTR: string
}

type AuthData = {
    Token: string
}

type Msg =
    | InitialOrderIDsLoaded of string list
    | UpdateOrderIDs 
    | SetSecretString of string
    | TryToAuth
    | Authed of AuthData
    | Error of exn

let initialOrderIDs () = Fetch.fetchAs<unit, string list> "/api/orders"

let authUser secretString = promise {
    let body = Encode.Auto.toString(0, { AUTHSTR = secretString} )
    let props = [
        Method HttpMethod.POST
        Fetch.requestHeaders [ ContentType "application/json" ]
        Body !^body
    ]

    try
        let! res = Fetch.fetch "/api/auth" props
        let! txt = res.text()
        return Decode.Auto.unsafeFromString<AuthData> txt
    with _ ->
        return! failwithf "Could not authenticate user."
}

let loadOrderIDsCmd = Cmd.OfPromise.perform initialOrderIDs () InitialOrderIDsLoaded

let init () : Model * Cmd<Msg> =
    let initialModel = { OrderIDs = List.empty; SecretString = ""; Token = ""; }
    initialModel, loadOrderIDsCmd

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | InitialOrderIDsLoaded orderIds->
        let nextModel = { currentModel with OrderIDs = orderIds }
        nextModel, Cmd.none
    | UpdateOrderIDs ->
        currentModel, loadOrderIDsCmd
    | SetSecretString secretString ->
        let nextModel = { currentModel with SecretString = secretString }
        nextModel, Cmd.none
    | TryToAuth ->
        currentModel, Cmd.OfPromise.either authUser (currentModel.SecretString) Authed Error
    | Authed token ->
        let nextModel = { currentModel with Token = token.Token }
        nextModel, Cmd.none
    | _ -> currentModel, Cmd.none


let view (model : Model) (dispatch : Msg -> unit) =
    div [ Style [ CSSProp.FontSize "12pt"; CSSProp.Padding "1rem" ] ] [
        div [] [
            div [ Style [ CSSProp.TextAlign TextAlignOptions.Center ] ] [
                b [] [ str "Enter secret string - " ]
                input [ Size 20.0; Value model.SecretString; OnChange (fun e -> e.Value |> SetSecretString |> dispatch) ]
                button [ Style [ CSSProp.MarginLeft "0.1rem" ]; OnClick (fun e -> TryToAuth |> dispatch) ] [ str "Sign In" ]
            ]
            div [ Style [ CSSProp.Color "white"; CSSProp.Margin "0.5rem"; CSSProp.Padding "0.5rem"; CSSProp.Background ( if model.Token.Length > 0 then "green" else "red" ) ] ] [
                div [ Style [ CSSProp.FontSize "9pt" ] ] [ b [] [ str "JWT" ] ]
                div [ Style [ CSSProp.TextAlign TextAlignOptions.Center ] ] [ i [ Style [ CSSProp.WordBreak "break-all" ] ] [ if model.Token.Length = 0 then str "Not Auth" else str model.Token ] ]
            ]
        ]
        br []
        div [] [
            div [] [
                button [ OnClick ( fun e -> UpdateOrderIDs |> dispatch ) ] [ Icon.icon [] [ Fa.i [ Fa.Solid.Retweet ] [] ] ]
                span [] [str " Order IDs:" ]
            ]
            ol [ Style [ CSSProp.PaddingLeft "5rem"; CSSProp.PaddingTop "0.25rem"  ] ] [
                for orderId in model.OrderIDs do
                    yield li [] [ str orderId ]
            ]
        ]
        
    ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactBatched "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
