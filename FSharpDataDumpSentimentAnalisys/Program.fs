// Learn more about F# at http://fsharp.org

open System
open System.IO
open FSharp.Data

[<Literal>]
let apiKeyFilePath = __SOURCE_DIRECTORY__ + "\\secret.txt"
let dataDumpFilePath = __SOURCE_DIRECTORY__ + "\\Comments.xml"

type Comments = XmlProvider<"""<comments><row Id="1" PostId="1" Score="0" Text="Foo." CreationDate="2010-07-28T19:36:59.773" UserId="1" /><row Id="1" PostId="1" Score="0" Text="Foo." CreationDate="2010-07-28T19:36:59.773" UserId="1" /></comments>""">

[<EntryPoint>]
let main argv =
    let apiKey =
                use sr = new StreamReader(apiKeyFilePath)
                sr.ReadToEnd()
    let rawData =
                use sr = new StreamReader(dataDumpFilePath)
                sr.ReadToEnd()

    let comments = Comments.Parse(rawData)
    let result = Http.RequestString
                    ( "https://api.tap.aylien.com/v1/models/718dbdc0-98fa-4ace-b814-a555859c094d", httpMethod = "GET",
                    query   = [ "x-aylien-tap-application-key", apiKey; "text", "batman" ],
                    headers = [ "Accept", "application/json" ])
    
    Console.Write(result)
    Console.ReadKey();
    0 // return an integer exit code
