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
                _href "/main.css"
            ]
            script [] [ rawText "var whTooltips = {colorLinks: true, iconizeLinks: true, renameLinks: false, iconSize: 'large'};" ]
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

let ifTrueInclude =
    List.where (snd >> eq true) >> List.map fst

let card header body list =
    div [ _class "card" ] (ifTrueInclude [
        div [ _class "card-header" ] [ encodedText header],true
        div [ ifTrueClass [true,"card-body container-fluid"; List.forall snd body,"list-group-item-success"] ] [
            div [ _class "row" ] (
                body |> List.map (fun (item,earned) -> div [ ifTrueClass [true, "col"; earned, "list-group-item-success"]] item )
            )
        ],List.isEmpty body |> not
        ul [ _class "list-group list-group-flush" ] (
            list
            |> List.map (fun (link,b) ->
                li [ (ifTrueClass [ (true,"list-group-item"); (b,"list-group-item-success") ] ) ] (link))
        ),List.isEmpty list |> not
    ])

let rec storyline = function
    | Step (name,required,slis) ->
        let steps = slis |> List.map storyline
        ([
            card name ( required |> List.map (storyline) ) ( steps )
        ],
        steps |> List.last |>  snd |> eq true )
    | ParallelStep (name,required,slis) ->
        let steps = slis |> List.map storyline
        ([
            card name ( required |> List.map (storyline) ) ( steps )
        ],
        steps |> List.forall (snd >> eq true) )
    | Achievement (id,earned) ->
        ([ a [
                match earned with
                    | Some(who,time) -> sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id who time
                    | None -> sprintf "//wowhead.com/achievement=%i" id
                |> _href
                ] []
        ],earned.IsSome)
    | Level (required,earned,n) ->
        ([
            p [] [ sprintf "Level %i/%i" n required |> encodedText ]
        ],earned)
    | Quest (id,earned) ->
        ([
            a [ sprintf "//wowhead.com/quest=%i" id |> _href ] []
        ],earned)
    | Reputation (id,stnading,value,earned) ->
        ([
            a [ ( sprintf "//wowhead.com/faction=%i" id |> _href) ] [ encodedText "Unknown faction" ]
            p [] [
                match earned with
                | Some(e,s,v) -> sprintf "Standing: %i/%i Value: %i/%i" s stnading v value
                | None -> "Reputation with faction not found"
                |> encodedText
            ]
        ], match earned with | Some (e,_,_) -> e | None -> false)

let storylines (model:ProcessedStorylineItem list) =
    [
        script [] [ rawText "whTooltips.renameLinks= true;" ]
        h1 [] [ encodedText "Storylines" ]
        div [] [yield! Seq.collect (storyline >> fst) model]
    ] |> layout