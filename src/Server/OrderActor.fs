module OrderActor

open System

[<RequireQualifiedAccess>]
type PostOrderIDResult =
    | Successful
    | Failed

[<RequireQualifiedAccess>]
type GetOrderIDsResult =
    | Successful of string[]
    | Failed

type OrderActorMsg =
    | PostOrderIDs of orderId: string list *  AsyncReplyChannel<PostOrderIDResult>
    | GetOrderIDs of AsyncReplyChannel<GetOrderIDsResult>

type IOrderActor =
    inherit IDisposable
    abstract PostOrderIDs : string list -> Async<PostOrderIDResult>
    abstract GetOrderIDs : unit -> Async<GetOrderIDsResult>

let orderActor () =
    let router = new MailboxProcessor<_>(fun (inbox: MailboxProcessor<OrderActorMsg>) ->
        let rec loop(orders: string list) = async {
            let! msg = inbox.Receive()
            match msg with
            | PostOrderIDs (orderIds, reply) ->
                PostOrderIDResult.Successful |> reply.Reply
                let uniqueOrderIds = orderIds |> List.filter (fun orderId -> orders |> List.contains orderId |> not )
                printfn "Unique Order IDs %A" uniqueOrderIds
                return! loop (orders |> List.append uniqueOrderIds)
            | GetOrderIDs reply ->
                orders |> List.toArray |> GetOrderIDsResult.Successful |> reply.Reply 
                return! loop orders
        }
        loop [] )
    let subscription = router.Error.Subscribe(fun ex -> printf "Exception in OrderActor %A" ex)
    router.Start()
    {
        new IOrderActor with
            member __.PostOrderIDs orderIds = router.PostAndAsyncReply (fun reply -> PostOrderIDs (orderIds, reply))
            member __.GetOrderIDs () = router.PostAndAsyncReply (fun reply -> GetOrderIDs reply)
            member __.Dispose () =
                subscription.Dispose()
                (router :> IDisposable).Dispose()
    }