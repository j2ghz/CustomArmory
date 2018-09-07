module Views
open Giraffe.GiraffeViewEngine
open System
open StorylineData

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
    let earned = Seq.tryFind (fun (i,_) -> i = id) (character |> Data.completedAchievements) |> Option.bind (snd >> Some)
    let who = if earned.IsSome then "Kosiilspaan" else ""
    let time = Option.defaultValue 0L earned
    let criteria = character |> Data.completedCriteria |> Data.filterCriteria crs
    a [ _href (sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id who time);  _rel ( criteria ); _class (if Option.isSome earned then "" else "missing") ] []

let achievementLink2 character (a:Data.AllAchievements.Achievement2) =
    achievementLink' character (a.Id,a.Criteria |> Array.map (fun c -> c.Id))

let achievementLink3 character (a:Data.AllAchievements.Achievement3) =
    achievementLink' character (a.Id,a.Criteria |> Array.map (fun c -> c.Id) |> Array.toSeq)

let index (model : Data.AllAchievements.Achievement[]) character =
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

let wrap item = li [ _class "StepProgress-item" ] [ item ]

let quest quests id =
    li [
        sprintf "StepProgress-item %s" (if Array.contains id quests then "is-done" else "") |> _class
        ] [ a [
            sprintf "//wowhead.com/quest=%i" id |> _href
            ] []
    ]

let achievement character achievements id =
    let earned = Seq.tryFind (fun (i,_) -> i = id) achievements |> Option.bind (snd >> Some)
    let who = if earned.IsSome then character else ""
    let time = Option.defaultValue 0L earned
    li [
        sprintf "StepProgress-item %s" (if earned.IsSome then "is-done" else "") |> _class
        ] [ a [
            sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id who time |> _href
            ] []
    ]

let rec storyline (character:Data.Character.Root) (sli:StorylineItem) =
    match sli with
    | Step (name,required,slis) ->
        li [ ] [
            strong [] [ encodedText name ]
            div [ _class "wrapper" ] [ ul [] ( required |> List.map (storyline character)) ]
            div [ _class "wrapper" ] [ ul [ _class "StepProgress" ] ( slis |> List.map (storyline character) ) ]
        ]
    | ParallelStep (name,required,slis) ->
        li [ ] [
            strong [] [ encodedText name ]
            ul [] ( required |> List.map (storyline character))
            ol [] [ div [ _style "display: flex;" ] ( slis |> List.map (storyline character)) ]
        ]
    | Achievement id -> achievement character.Name (character.Achievements |> Data.completedAchievements) id
    | Level n -> span [] [ sprintf "Required level %i" n |> encodedText ] |> wrap
    | Quest id -> quest character.Quests id
    | Reputation (id,level,points) -> a [ ( sprintf "//wowhead.com/faction=%i" id |> _href) ] [] |> wrap

let storylines (model:StorylineData.StorylineItem list) (character:Data.Character.Root) =
    [
        script [] [ rawText "whTooltips.renameLinks= true;" ]
        h1 [] [ encodedText "Storylines" ]
        ol [] [
            yield! model |> Seq.map (storyline character)
        ]
    ] |> layout