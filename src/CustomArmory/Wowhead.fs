module Wowhead

open FSharp.Data

let split (character:char) (string:string) =
    string.Split(character)

let splitTakeLast character =
    split character >> Array.last

let cssSelect selector (doc:FSharp.Data.HtmlDocument) =
    CssSelectorExtensions.CssSelect(doc,selector)

let getComment (url:string) =
    let id = splitTakeLast '#' url
    HtmlDocument.Load url
    |> cssSelect (sprintf "#%swo" id)