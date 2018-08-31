module Views
open Giraffe.GiraffeViewEngine
open System

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

let achievementLink' (id,crs) =
    let earned = Seq.tryFind (fun (i,_) -> i = id) Data.completedAchievements |> Option.bind (snd >> Some)
    a [ _href (sprintf "//wowhead.com/achievement=%i&who=%s&when=%i" id (if earned.IsSome then "Kosiilspaan" else "") (Option.defaultValue 0L earned) );  _rel (Data.filterCriteria crs); _class (if Data.completedAchievements |> Map.ofSeq |>  Map.containsKey id  then "" else "missing") ] []

let achievementLink2 (a:Data.AllAchievements.Achievement2) =
    achievementLink' (a.Id,a.Criteria |> Array.map (fun c -> c.Id))

let achievementLink3 (a:Data.AllAchievements.Achievement3) =
    achievementLink' (a.Id,a.Criteria |> Array.map (fun c -> c.Id))

let index (model : Data.AllAchievements.Achievement[]) =
    [ol [] [yield! model |> Array.map (fun c ->
        li [] [
            h2 [] [ encodedText c.Name ]
            div [] [yield! c.Achievements |> Array.map achievementLink2 ]
            ol [] [yield! c.Categories |> Array.map (fun cat -> li [] [
                h3 [] [encodedText cat.Name]
                div [] [yield! cat.Achievements |> Array.map achievementLink3 ]
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