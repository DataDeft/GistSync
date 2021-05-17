namespace DataDeft.GistSync

// Internal

open GithubApi
open Utils

// External

open FSharp.Data
open System
open System.IO
open System.Collections.Generic
open System.Text


module Main =


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


  let rec traverseGists localPath (acc:int) url token  =
    logger <| sprintf "Entering iteration: %i" acc
    let response =
      httpRequestAsync url token
      |> Async.RunSynchronously

    match response with
    | Some (status, headers, body) ->

        let nextMaybe = handleNext (headers.Item("Link"))
        //logger <| sprintf "%A" nextMaybe

        match status, nextMaybe with
        | 200, Some ("next", urlNext) ->
          processChunk token localPath body
          logRateLimit token
          traverseGists localPath (acc+1) urlNext token

        | 503, _  ->
          logger <| sprintf "You are being throttled!"
          let waitTime = getWaitTimeExp acc
          async { do! Async.Sleep(waitTime) }
          |> Async.RunSynchronously
          // Retrying with the current context
          traverseGists localPath acc url token

        | 200, None ->
          logger <| sprintf "The end."
          processChunk token localPath body
          Some acc

        | httpStatus, next ->
          logger <| sprintf "Potential problem: httpStatus: %A next: %A" httpStatus next
          None

    | None ->
        logger <| sprintf "%A" response
        None


  let syncGists localPath =

    let initialUrl =
      sprintf "https://api.github.com/users/%s/gists" "l1x"

    let initialList =
      new List<Gist>()

    readPersonalToken()
    |> Option.bind (traverseGists localPath 0 initialUrl)
    |> Option.map (fun xs -> logger <| sprintf "%A" xs)
    |> Option.defaultWith (fun xs -> logger <| sprintf "%A" xs)


  let listGists localPath =

    ()

  let help () =

    logger <| Figgle.FiggleFonts.Isometric1.Render("GistSync Help")
    logger <| sprintf "--command sync --local-path gists/"
    logger <| sprintf "--command search --local-path gists/ --pattern ping"
    logger <| sprintf "--command help"


  [<EntryPoint>]
  let main args =

    let commandLineArgumentsParsed = Cli.parseCommandLine (Array.toList args)

    match commandLineArgumentsParsed.Command with
    | Sync -> syncGists commandLineArgumentsParsed.LocalPath
    | List -> listGists commandLineArgumentsParsed.LocalPath
    | Help -> help ()
    0
