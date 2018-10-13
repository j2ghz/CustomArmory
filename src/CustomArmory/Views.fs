module Views
open Giraffe.GiraffeViewEngine
open System
open Storylines
open System.Collections.Generic

let layout (content: XmlNode list) =
    html [ _lang "en" ] [
        head [] [
            meta [ _charset "utf-8" ]
            meta [ _name "viewport"; _content "width=device-width, initial-scale=1, shrink-to-fit=no" ]
            title []  [ encodedText "CustomArmory" ]
            link [
                _rel  "stylesheet"
                _type "text/css"
                _href "https://stackpath.bootstrapcdn.com/bootstrap/4.1.3/css/bootstrap.min.css"
                _crossorigin "anonymous"
                _integrity "sha384-MCw98/SFnGE8fJT3GXwEOngsV7Zt27NXFoaoApmYm81iuXoPkFOJwJ8ERdknLPMO"
            ]
            link [
                _rel  "stylesheet"
                _type "text/css"
                _href "https://use.fontawesome.com/releases/v5.3.1/css/all.css"
                _crossorigin "anonymous"
                _integrity "sha384-mzrmE5qonljUremFsqc01SB46JvROS7bZs3IO2EmfFsd15uHvIt+Y8vEf7N7fWAU"
            ]
            link [
                _rel  "stylesheet"
                _type "text/css"
                _href "/main.css"
            ]
            script [] [ rawText "var whTooltips = {colorLinks: true, iconizeLinks: true, renameLinks: true, iconSize: 'tiny'};" ]
        ]
        body [] [
            div [ _class "container" ] [yield! content]
            script [ _src "https://code.jquery.com/jquery-3.3.1.slim.min.js"; _integrity "sha384-q8i/X+965DzO0rT7abK41JStQIAqVgRVzpbzo5smXKp4YfRvH+8abtTE1Pi6jizo"; _crossorigin "anonymous" ] []
            script [ _src "https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.3/umd/popper.min.js"; _integrity "sha384-ZMP7rVo3mIykV+2+9J3UJ46jBk0WLaUAdn689aCwoqbBJiSnjAK/l8WvCWPIPm49"; _crossorigin "anonymous" ] []
            script [ _src "https://stackpath.bootstrapcdn.com/bootstrap/4.1.3/js/bootstrap.min.js"; _integrity "sha384-ChfqqxuZUCnJSK3+MXmPNIyE6ZbWh2IMqE241rYiqJxyMiZ6OW/JmZQ5stwEULTy"; _crossorigin "anonymous" ] []
            script [ _async; _src "https://wow.zamimg.com/widgets/power.js" ] []
        ]
    ]

let calendar (model: seq<DateTime*seq<int64*XmlNode>>) =
    [
        h1 [] [encodedText "Calendar"]
        div [] [ yield! model |> Seq.map (fun (day,items) -> div [] [
            h2 [] [ encodedText (day.ToLongDateString())]
            ul [] [ yield! items |> Seq.map (fun (_,link) -> li [] [ link ])]]
        )]
    ] |> layout

let join (separator:string) (items:string list) = String.Join(separator,items)

let ifTrueClass list =
    list
    |> List.where (fun (b,c) -> b)
    |> List.map snd
    |> join " "
    |> _class

let wrapIcon (items,earned) =
    i [ _class (
            match earned with
            | true ->  "fas fa-check"
            | false -> "fas fa-times"
    )] []
    ::     
    items

let card header content =
    div [ _class "card" ] [
        div [ _class "card-header" ] header

        div [ _class "card-body" ] content
    ]

let rec storyline = function
    | TextHeading(title, item, completed) ->
        [ card [ h1 [] [ encodedText title ] ] ( item |> storyline ) ]
    | ItemHeading(title, item, completed) ->
        [ card [ h1 [] ( title |> storyline ) ] ( item |> storyline ) ]
    | Sequential(items, _) ->
        [ div [ _class "d-inline-flex flex-column" ] (items |> List.collect storyline) ]
    | Parallel(items, _) ->
        [ div [ _class "d-inline-flex" ] (items |> List.collect storyline) ]
    | Achievement (id,earned) ->
        [ a [
                match earned with
                    | Some(who,time) -> sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id who time
                    | None -> sprintf "//wowhead.com/achievement=%i" id
                |> _href
                ] []
        ]
    | Level (required,earned,n) ->
        [
            p [] [ sprintf "Level %i/%i" n required |> encodedText ]
        ]
    | Quest (id,earned) ->
        [
            a [ sprintf "//wowhead.com/quest=%i" id |> _href ] []
        ]
    | Reputation (id,stnading,value,earned) ->
        [
            a [ ( sprintf "//wowhead.com/faction=%i" id |> _href) ] [ encodedText "Unknown faction" ]
            p [] [
                match earned with
                | Some(e,s,v) -> sprintf "Standing: %i/%i Value: %i/%i" s stnading v value
                | None -> "Reputation with faction not found"
                |> encodedText
            ]
        ]

let storylines (model:ProcessedStorylineItem list) =
    [
        h1 [] [ encodedText "Storylines" ]
        div [] [yield! Seq.collect (storyline) model]
    ] |> layout
