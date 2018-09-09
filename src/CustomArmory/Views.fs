module Views
open Giraffe.GiraffeViewEngine
open System
open Storylines

let layout (content: XmlNode list) =
    html [] [
        head [] [
            title []  [ encodedText "CustomArmory" ]
            link [
                _rel  "stylesheet"
                _type "text/css"
                _href "/main.css"
            ]
            script [] [ rawText "var whTooltips = {colorLinks: true, iconizeLinks: true, renameLinks: false, iconSize: 'large'};" ]
            script [ _async; _src "https://wow.zamimg.com/widgets/power.js" ] []
        ]
        body [] content
    ]

let achievementLink' character (id,crs) =
    let earned = Seq.tryFind (fun (i,_) -> i = id) (character |> Character.completedAchievements) |> Option.bind (snd >> Some)
    let who = if earned.IsSome then "Kosiilspaan" else ""
    let time = Option.defaultValue 0L earned
    let criteria = character |> Character.completedCriteria |> Character.filterCriteria crs
    a [ _href (sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id who time);  _rel ( criteria ); _class (if Option.isSome earned then "" else "missing") ] []

let achievementLink2 character (a:Character.AllAchievements.Achievement2) =
    achievementLink' character (a.Id,a.Criteria |> Array.map (fun c -> c.Id))

let achievementLink3 character (a:Character.AllAchievements.Achievement3) =
    achievementLink' character (a.Id,a.Criteria |> Array.map (fun c -> c.Id) |> Array.toSeq)

let index (model : Character.AllAchievements.Achievement[]) character =
    [ol [] [yield! model |> Array.map (fun c ->
        li [] [
            h2 [] [ encodedText c.Name ]
            div [] [yield! c.Achievements |> Array.map (achievementLink2 character) ]
            ol [] [yield! c.Categories |> Array.map (fun cat -> li [] [
                h3 [] [encodedText cat.Name]
                div [] [yield! cat.Achievements |> Array.map (achievementLink3 character) ]
            ])]
        ])]]
    |> layout

let calendar (model: seq<DateTime*seq<int64*XmlNode>>) =
    [
        h1 [] [encodedText "Calendar"]
        div [] [ yield! model |> Seq.map (fun (day,items) -> div [] [
            h2 [] [ encodedText (day.ToLongDateString())]
            ul [] [ yield! items |> Seq.map (fun (_,link) -> li [] [ link ])]]
        )]
    ] |> layout

let wrap earned item =
    li [
        sprintf "StepProgress-item %s" (if earned then "is-done" else "") |> _class
    ] item

let rec storyline = function
    | Step (name,required,slis) ->
        li [ ] [
            strong [] [ encodedText name ]
            div [ _class "wrapper" ] [ ul [] ( required |> List.map storyline) ]
            div [ _class "wrapper" ] [ ol [ _class "StepProgress" ] ( slis |> List.map storyline ) ]
        ]
    | ParallelStep (name,required,slis) ->
        li [ ] [
            strong [] [ encodedText name ]
            ul [] ( required |> List.map storyline)
            div [ _style "display: flex;" ] ( slis |> List.map storyline)
        ]
    | Achievement (id,earned) ->
        wrap earned.IsSome [ a [
                match earned with
                    | Some(who,time) -> sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id who time
                    | None -> sprintf "//wowhead.com/achievement=%i" id
                |> _href
                ] []
        ]
    | Level (required,earned,n) ->
        wrap earned [p [] [ sprintf "Level %i/%i" n required |> encodedText ]]
    | Quest (id,earned) ->
        wrap earned [a [ sprintf "//wowhead.com/quest=%i" id |> _href ] []]
    | Reputation (id,stnading,value,earned) ->
        wrap earned.IsSome [
            a [ ( sprintf "//wowhead.com/faction=%i" id |> _href) ] []
            p [] [
                match earned with
                | Some(e,s,v) -> sprintf "Standing: %i/%i Value: %i/%i" s stnading v value
                | None -> "Reputation with faction not found"
                |> encodedText
            ]
        ]

let storylines (model:ProcessedStorylineItem list) =
    [
        script [] [ rawText "whTooltips.renameLinks= true;" ]
        h1 [] [ encodedText "Storylines" ]
        ol [] [
            yield! model |> Seq.map storyline
        ]
    ] |> layout