module Orders

open FSharp.Control.Tasks.V2
open Giraffe
open Microsoft.AspNetCore.Http
open OrderActor
open Shared

let PostOrders (actor: IOrderActor) (next: HttpFunc) (ctx: HttpContext) = task {
    let! orderIdsList = ctx.BindJsonAsync<OrderIDsList>()
    printfn "We've received %A" orderIdsList

    let! result = actor.PostOrderIDs orderIdsList.OrderIDs
    match result with
    | PostOrderIDResult.Successful ->
        return! text "Ok" next ctx
    | PostOrderIDResult.Failed ->
        ctx.SetStatusCode 400
        return Some ctx
}

let GetOrders (actor: IOrderActor) (next: HttpFunc) (ctx: HttpContext) = task {
    printfn "Try get order ids"
    let! result = actor.GetOrderIDs()
    match result with
    | GetOrderIDsResult.Successful orderIds ->
        return! json orderIds next ctx
    | GetOrderIDsResult.Failed ->
        ctx.SetStatusCode 400
        return Some ctx
}