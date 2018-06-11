// Learn more about F# at http://fsharp.org

open System
open System.IO
open FSharp.Data
open System.Text.RegularExpressions

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

// e.g. aaaa-aaa'a.
[<Literal>]
let wordPattern = "(^[\w][\.]?$)|(^[\w](\w|\-|\')*[\w][\.]?$)"

let shuffle (array : 'a array) =
    let rng = new Random()
    let n = array.Length
    for x in 1..n do
        let i = n-x
        let j = rng.Next(i+1)
        let tmp = array.[i]
        array.[i] <- array.[j]
        array.[j] <- tmp
    array

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
    let posts = shuffle(Posts.Parse(File.ReadAllText(postsFilePath)).Posts)
                    |> Array.take(postsLimit)
    let comments = Comments.Parse(File.ReadAllText(commentsFilePath)).Comments
    
    for post in posts do
        let question = Array.append [|post.Body.Value|] (comments                                                            
                                                            |> Array.filter(fun x -> x.PostId = post.Id) 
                                                            |> Array.map(fun x -> x.Text)) |> String.concat " "

        let normalizedQuestion = question.Split(' ') 
                                    |> Array.filter(fun x -> Regex.IsMatch(x, wordPattern)) 
                                    |> String.concat " "


        let result = Http.RequestString
                        ( "https://api.aylien.com/api/v1/sentiment", httpMethod = "POST",
                        body = FormValues [ "text", normalizedQuestion; "mode", "document"; "language", "en" ],
                        headers = [ "Accept", "application/json"; "x-aylien-textapi-application-key", secrets |> Seq.last; "x-aylien-textapi-application-id", secrets |> Seq.head])
        let sentiment = Sentiment.Parse(result)
        Console.WriteLine("Post id={0}, has {1} polarity with {2} confidence, and {3} subjectivity with {4} confidence." , post.Id, sentiment.Polarity, sentiment.PolarityConfidence, sentiment.Subjectivity, sentiment.SubjectivityConfidence)
    Console.ReadKey();
    0
