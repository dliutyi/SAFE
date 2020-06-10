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
    | PostOrderID of orderId: string *  AsyncReplyChannel<PostOrderIDResult>
    | GetOrderIDs of AsyncReplyChannel<GetOrderIDsResult>

type IOrderActor =
    inherit IDisposable
    abstract PostOrderID : string -> Async<PostOrderIDResult>
    abstract GetOrderIDs : unit -> Async<GetOrderIDsResult>

let orderActor () =
    let router = new MailboxProcessor<_>(fun (inbox: MailboxProcessor<OrderActorMsg>) ->
        let rec loop(orders: string list) = async {
            let! msg = inbox.Receive()
            match msg with
            | PostOrderID (orderId, reply) ->
                PostOrderIDResult.Successful |> reply.Reply
                return! loop (orderId :: orders)
            | GetOrderIDs reply ->
                orders |> List.toArray |> GetOrderIDsResult.Successful |> reply.Reply 
                return! loop orders
        }
        loop [] )
    let subscription = router.Error.Subscribe(fun ex -> printf "Exception in OrderActor %A" ex)
    router.Start()
    {
        new IOrderActor with
            member __.PostOrderID orderId = router.PostAndAsyncReply (fun reply -> PostOrderID (orderId, reply))
            member __.GetOrderIDs () = router.PostAndAsyncReply (fun reply -> GetOrderIDs reply)
            member __.Dispose () =
                subscription.Dispose()
                (router :> IDisposable).Dispose()
    }