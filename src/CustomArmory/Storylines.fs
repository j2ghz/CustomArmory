module Storylines

open FSharp.Data
open System.IO

let (+/) a b = Path.Combine(a,b)

let basePath = "Data/Storylines"
let getStorylinePath name = basePath +/ sprintf "%s.json" name

type Storyline = JsonProvider<"Data/Storylines/12997.json">

let getStoryline (path:string) = Storyline.Load path

let getStorylines =
    System.IO.Directory.EnumerateFiles(basePath)
    |> Seq.map getStoryline