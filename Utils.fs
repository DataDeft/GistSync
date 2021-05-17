namespace DataDeft.GistSync

// Internal

// External
open System
open System.IO


module Utils =


  let logger (s:string) =
    System.Console.WriteLine s


  let getWaitTimeExp (retryCount: int) =
    int (Math.Pow(2.0, (float retryCount)) * 100.0)


  let tee (log: string -> unit) x =
    log <| sprintf "%A" x
    x




