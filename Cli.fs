namespace DataDeft.GistSync


// internal


// external
open System
open System.IO


type ValidCommand = Sync | List | Help


[<StructuredFormatDisplay("LocalPath: {LocalPath} :: Command: {Command}" )>]
type CommandLineOptions =
  {
    LocalPath: string
    Command: ValidCommand
  }


module Cli =


  let logger (s:string) =
    System.Console.WriteLine s


  type InvalidCommand = InvalidCommand of string


  let isValidPath p =
    try
      File.GetAttributes(p).HasFlag(FileAttributes.Directory)
    with _ex ->
      false


  let isValidLocalPath p =
    isValidPath p


  let isValidCommand c =
    match c with
    | "sync"  -> Ok Sync
    | "list"  -> Ok List
    | "help"  -> Ok Help
    | any     -> Error (InvalidCommand any)


  let rec private parseCommandLineRec args optionsSoFar =
    match args with

    //
    // Empty args
    //

    | [] ->
      optionsSoFar

    //
    // LocalPath
    //

    | "--local-path" :: xs ->
        match xs with
        | localPath :: xss ->
            match isValidLocalPath localPath with
            | true -> parseCommandLineRec xss { optionsSoFar with LocalPath = localPath }
            | false ->
                logger <| sprintf "local-path might not exist: %s" localPath
                Environment.Exit 1
                parseCommandLineRec xss optionsSoFar // never reach

        | [] ->
            logger <| sprintf "local-path is empty"
            parseCommandLineRec xs optionsSoFar


    //
    // Command
    //


    | "--command" :: xs ->
        match xs with
        | command :: xss ->
            match isValidCommand command with
            | Ok validCommand       ->
                parseCommandLineRec xss { optionsSoFar with Command = validCommand }
            | Error invalidCommand  ->
                logger <| sprintf "Unsupported command: %s" command
                Environment.Exit 1
                parseCommandLineRec xss optionsSoFar // never reach

        | [] ->
            logger <| sprintf "command cannot be empty"
            Environment.Exit 1
            parseCommandLineRec xs optionsSoFar // never reach

    //
    // Help
    //


    | "--help" :: xs ->

      parseCommandLineRec [] { optionsSoFar with Command = Help }


    //
    // Unknown args
    //


    | x :: xs ->
        logger <| sprintf "Option %A is unrecognized" x
        parseCommandLineRec xs optionsSoFar


  //
  // Calling parseCommandLine with args and defaultOptions
  //


  let parseCommandLine args =
    let defaultOptions =
      {
        LocalPath = "/tmp"
        Command = List
      }
    parseCommandLineRec args defaultOptions


// END
