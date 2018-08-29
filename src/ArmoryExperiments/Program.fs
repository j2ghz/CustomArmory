// Learn more about F# at http://fsharp.org

open System
open FSharp.Data

type Achievement = {Id:int;Title:string;Description:string}
type Achievements = JsonProvider<"SampleData/AllAchievements.json">
type Character = JsonProvider<"SampleData/Kosiilspaan.json">
//type Quest = JsonProvider<"SampleData/40963.json">
type Criterion = {Id:int;Description:string; Max:int}

let collect (achievements:Achievements.Achievement[]) =
    let a = achievements |> Array.collect (fun a -> a.Achievements) |> Array.map (fun a -> {Id=a.Id;Title=a.Title;Description=a.Description}) |> Array.toList
    let c = achievements |> Array.collect (fun a -> a.Categories) |> Array.collect (fun c -> c.Achievements) |> Array.map (fun a -> {Id=a.Id;Title=a.Title;Description=a.Description}) |> Array.toList
    List.concat [a;c]

let toString (ios:Achievements.IntOrString) =
    let value = (ios.String,ios.Number)
    match value with
    | (Some s,_) -> Some s
    | (_,Some i) -> Some (i.ToString())
    | _ -> None

let collectCriteria (achievements:Achievements.Achievement[]) =
    let a =
        achievements
        |> Array.collect (fun a -> a.Achievements)
        |> Array.collect (fun a -> a.Criteria |> Array.map (fun c -> {Id=c.Id;Description = sprintf "%s: %s" a.Title (c.Description |> Option.defaultValue "None");Max = c.Max}))
        |> Array.toList
    let c =
        achievements
        |> Array.collect (fun a -> a.Categories)
        |> Array.collect (fun c -> c.Achievements)
        |> Array.collect (fun a -> a.Criteria|> Array.map (fun c -> {Id=c.Id;Description = sprintf "%s: %s" a.Title (toString c.Description |> Option.defaultValue "None");Max = c.Max}))
        |> Array.toList
    List.concat [a;c]

let toDate millis =
    DateTimeOffset.FromUnixTimeMilliseconds millis

//let getQuest' id =
//    Quest.Load (sprintf "https://eu.api.battle.net/wow/quest/%i?locale=en_GB&apikey=kwptv272nvrashj83xtxcdysghbkw6ep" id)

//let getQuest = getQuest'

let criterionToString crs (cr,t,q)=
    (toDate t,
        match List.tryFind (fun criterion -> criterion.Id = cr) crs with
        | Some c -> sprintf "%27i: C %s (%i/%i)" c.Id c.Description q c.Max
        | None -> sprintf "%27i: C Unknown (%i/?)" cr q
    )

let achievementToString achs (a,time) =
    (toDate time,
        let ac = achs |> List.find (fun (ac:Achievement) -> a = ac.Id)
        sprintf "%27i: A %s: %s" ac.Id ac.Title ac.Description
    )

[<EntryPoint>]
let main argv =
    let data = Achievements.Load "SampleData/AllAchievements.json"
    let achs = collect data.Achievements
    let character = (Character.Load "SampleData/Kosiilspaan.json")
    let achievementsByTime =
        Array.zip character.Achievements.AchievementsCompleted character.Achievements.AchievementsCompletedTimestamp
                |> Array.map (achievementToString achs)
    let criteria = collectCriteria data.Achievements
    let criteriaByTime =
        Array.zip3 character.Achievements.Criteria character.Achievements.CriteriaTimestamp character.Achievements.CriteriaQuantity
        |> Array.map (criterionToString criteria)
    let combined =
        Array.concat [achievementsByTime;criteriaByTime]
        |> Array.groupBy fst
        |> Array.sortBy fst

    for (t,ss) in combined do
        printfn "[%s]" (t.ToString())
        for (_,s) in ss do
            printfn "%s" s

    0