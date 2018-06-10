// Learn more about F# at http://fsharp.org

open System
open System.IO
open FSharp.Data

[<Literal>]
let apiKeyFilePath = __SOURCE_DIRECTORY__ + "\\secret.txt"
[<Literal>]
let commentsFilePath = __SOURCE_DIRECTORY__ + "\\Comments.xml"
[<Literal>]
let postsFilePath = __SOURCE_DIRECTORY__ + "\\Posts.xml"
[<Literal>]
let sampleSentiment = """{
    "polarity": "neutral",
    "subjectivity": "unknown",
    "text": "blah blah blah",
    "polarity_confidence": 0.123,
    "subjectivity_confidence": 0
}"""

type Comments = XmlProvider<commentsFilePath>
type Posts = XmlProvider<postsFilePath>
type Sentiment = JsonProvider<sampleSentiment>

[<EntryPoint>]
let main argv =
    let secrets = seq {
                use sr = new StreamReader(apiKeyFilePath)
                while not sr.EndOfStream do
                    yield sr.ReadLine()
            }  
    let postsLimit = int(argv.[0])
    let posts = Posts.Parse(File.ReadAllText(postsFilePath)).Posts 
                                                               |> Array.take(postsLimit)
    let comments = Comments.Parse(File.ReadAllText(commentsFilePath)).Comments
    
    for post in posts do
        let question = Array.append [|post.Body.Value|] (comments                                                            
                                                            |> Array.filter(fun x -> x.PostId = post.Id) 
                                                            |> Array.map(fun x -> x.Text)) |> String.concat " "   
        let result = Http.RequestString
                        ( "https://api.aylien.com/api/v1/sentiment", httpMethod = "POST",
                        body = FormValues [ "text", question; "mode", "document"; "language", "en" ],
                        headers = [ "Accept", "application/json"; "x-aylien-textapi-application-key", secrets |> Seq.last; "x-aylien-textapi-application-id", secrets |> Seq.head])
        let sentiment = Sentiment.Parse(result)
        Console.WriteLine("Post id={0}, has {1} polarity with {2} confidence, and {3} subjectivity with {4} confidence." , post.Id, sentiment.Polarity, sentiment.PolarityConfidence, sentiment.Subjectivity, sentiment.SubjectivityConfidence)
    Console.ReadKey();
    0
