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