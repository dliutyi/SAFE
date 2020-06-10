module Client

open Elmish
open Elmish.React
open Fable.React
open Fable.React.Props
open Fetch.Types
open Thoth.Fetch
open Fulma
open Thoth.Json

type Model = { OrderIDs: string list }

type Msg =
    | InitialOrderIDsLoaded of string list

let initialOrderIDs () = Fetch.fetchAs<unit, string list> "/api/orders"

let init () : Model * Cmd<Msg> =
    let initialModel = { OrderIDs = List.empty }
    let loadOrderIDsCmd =
        Cmd.OfPromise.perform initialOrderIDs () InitialOrderIDsLoaded
    initialModel, loadOrderIDsCmd

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match currentModel.OrderIDs, msg with
    | _, InitialOrderIDsLoaded orderIds->
        let nextModel = { OrderIDs = orderIds }
        nextModel, Cmd.none
    | _ -> currentModel, Cmd.none


let view (model : Model) (_dispatch : Msg -> unit) =
    div [] [
        div [] [ str "Order IDs:" ]
        ol [] [
            for orderId in model.OrderIDs do
                yield li [] [ str orderId ]
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
