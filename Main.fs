namespace DataDeft.GistSync
open System


// Internal


// External

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.IO
open System.Collections.Generic


module Main =


  let logger (s:string) =
    System.Console.WriteLine s


  let syncGists localPath =

    

    ()

  let listGists localPath =
    ()

  [<EntryPoint>]
  let main args =

    let commandLineArgumentsParsed = Cli.parseCommandLine (Array.toList args)

    logger
    <| sprintf "%A" commandLineArgumentsParsed

    match commandLineArgumentsParsed.Command with
    | Sync -> syncGists commandLineArgumentsParsed.LocalPath
    | List -> listGists commandLineArgumentsParsed.LocalPath

    0
