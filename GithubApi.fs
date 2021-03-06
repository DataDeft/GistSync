namespace DataDeft.GistSync


// External

open FSharp.Data
open FSharp.Data.HttpRequestHeaders
open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open System.IO
open System.Collections.Generic
open System
open System.Text.RegularExpressions
open System.Text


type FileEntry = {
  //Filename: string
  RawUrl: string
}

type Gist = {
  Url: string
  Id: string
  Files: Dictionary<string, FileEntry>
  CreatedAt: string
  UpdatedAt: string
}

[<StructuredFormatDisplay("Limit: {Limit} :: Used: {Used} Remaining: {Remaining} :: Reset: {Reset}" )>]
type RateLimitDetails = {
  Limit: int
  Used: int
  Remaining: int
  Reset: int
}

type RateLimit = {
  Rate: RateLimitDetails
}


module GithubApi =


  let logger (s:string) =
    System.Console.WriteLine s


  let httpRequestAsync url token =
    async {

      let authString =
        "token " + token

      let httpString =
        Http.AsyncRequest
          (url, httpMethod = "GET",
          query   = [  ],
          headers = [
            Authorization authString
            Accept HttpContentTypes.Json
            UserAgent "GistSync"
          ])

      try
        let! resp = httpString
        //logger <| sprintf "%A" resp.Headers
        return (Some (resp.StatusCode, resp.Headers, resp.Body))
      with ex ->
        logger <| sprintf "Error happened while talking to Github API: %s" ex.Message
        return None
    }


  let defaultJsonOptions : JsonSerializerSettings =
    let contractResolver =
      DefaultContractResolver(
        NamingStrategy = SnakeCaseNamingStrategy())
    JsonSerializerSettings(
      ContractResolver = contractResolver,
      DefaultValueHandling = DefaultValueHandling.Ignore)


  let getRateLimitFromJson s =
    try
      Some (JsonConvert.DeserializeObject<RateLimit>(s, defaultJsonOptions))
    with ex ->
      logger <| sprintf "%s" ex.Message
      None


  let logRateLimit (token) =
    let response =
      httpRequestAsync "https://api.github.com/rate_limit" token
      |> Async.RunSynchronously
    match response with
    | Some (_httpStatus, _headers, body) ->
        // logger <| sprintf "%A" body
        match body with
        | Text txt ->
            logger <| sprintf "%A" (getRateLimitFromJson txt)
        | Binary bin ->
            logger <| sprintf "%A" (getRateLimitFromJson (Encoding.UTF8.GetString(bin)))
    | None ->
        logger <| sprintf "%A" response


  let swapNext (x:string, y:string) =
    (((y.Split "=").[1]).Trim('"'), x.Trim('<').Trim('>'))


  let getFirstTwo (xs) =
    match xs with
    | x :: y :: _xxs -> Some (x, y)
    | _ -> None


  let handleNext s =
    let r = new Regex("(<https://api.github.com/user/[0-9]+/gists\?page=[0-9]+>;\ rel=\"next\")")
    r.Matches s
    //|> tee logger
    |> Seq.tryHead
    |> Option.map (fun m -> m.Value)
    |> Option.map (fun x -> x.Split ";")
    |> Option.map (Seq.map (fun x -> x.Trim(' ')))
    |> Option.bind (fun xs -> getFirstTwo (Seq.toList xs))
    |> Option.map swapNext


  let getGistFromJson s =
    try
      Some (JsonConvert.DeserializeObject<List<Gist>>(s, defaultJsonOptions))
    with ex ->
      logger <| sprintf "%s" ex.Message
      None


  let getBodyParts body =
    match body with
    | Text txt ->
        getGistFromJson txt
    | Binary bin ->
        getGistFromJson (Encoding.UTF8.GetString(bin))


  let downloadRawUrl token url =
    let response =
      httpRequestAsync url token
      |> Async.RunSynchronously
    match response with
    | Some (_httpStatus, _headers, body) ->
        // logger <| sprintf "%A" body
        match body with
        | Text txt ->
            Some txt
        | Binary bin ->
            Some (Encoding.UTF8.GetString(bin))
    | None ->
        None


  let writeContentToDisk (path:string) (content:string) =
    try
      logger <| sprintf "%A" path
      File.WriteAllText(path, content)
    with ex ->
      logger <| sprintf "%A" ex.Message

  let downloadAndSave token folder (f:KeyValuePair<string,FileEntry>) =
    (downloadRawUrl token f.Value.RawUrl)
    |> Option.map (writeContentToDisk (Path.Combine(folder, f.Key)))
    |> ignore


  let isFolder p =
    Directory.Exists(p)


  let writeToDisk token (localPath) (g:Gist) =
    try

      let id = g.Id

      g.Files
      |> Seq.iter
        (fun f ->

          logger <| sprintf "localPath: %A id:%A f: %A" localPath id f.Key
          let folder = Path.Combine(localPath, id)

          match (isFolder folder) with
          | true ->
            downloadAndSave token folder f

          | false ->
            let dir = Directory.CreateDirectory(folder)
            downloadAndSave token folder f
        )
    with ex ->
      logger <| sprintf "%A" ex.Message


  let processChunk token (localPath:string) body =
    getBodyParts body

    // TODO Add async parallel

    |> Option.map
      (fun xs ->
        (Seq.iter (writeToDisk token localPath) xs))
    |> Option.defaultWith
      (fun x -> logger <| sprintf "%A" x)
