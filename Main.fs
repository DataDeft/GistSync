namespace DataDeft.GistSync
open System


// Internal


// External

open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.IO
open System.Collections.Generic
open System


module Main =


  let logger (s:string) =
    System.Console.WriteLine s


  let readPersonalToken () =
    try
      let home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
      let token = (File.ReadAllText (Path.Combine (List.toArray [home; ".git"; "githubToken"])))
      let trimmedToken = token.TrimEnd('\r', '\n')
      Some trimmedToken
    with ex ->
      logger <| sprintf "Could not read token file: %A" ex.Message
      logger <| sprintf "Create a file in ~/.git/ with the name githubToken"
      None


  let httpValami userName token =

    let authString =
      "token " + token

    let httpString =
      Http.AsyncRequestString
        ("https://api.github.com/user/repos", httpMethod = "GET",
        query   = [  ],
        headers = [
          Authorization authString
          Accept HttpContentTypes.Json
          UserAgent "GistSync"
        ])

    async {
      try
        let! resp = httpString
        logger <| sprintf "%A" resp
      with ex ->
        logger <| sprintf "Error happened while talking to Github API: %s" ex.Message
    }
    |> Async.RunSynchronously


  let syncGists localPath =
    readPersonalToken()
    |> Option.map (httpValami "l1x")
    |> Option.defaultWith (fun x -> logger <| sprintf "%A" x)


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
