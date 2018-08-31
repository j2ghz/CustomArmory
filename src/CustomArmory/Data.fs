module Data

open FSharp.Data
open System

type AllAchievements = JsonProvider<"Data/AllAchievements.json">
type Character = JsonProvider<"Data/Kosiilspaan.json">

let categories = (AllAchievements.Load "Data/AllAchievements.json").Achievements
let character = sprintf "Data/%s.json" >> Character.Load >> fun c -> c.Achievements

let completedAchievements (achievements:Character.Achievements)=
    Seq.zip (achievements.AchievementsCompleted |> Array.toSeq) (achievements.AchievementsCompletedTimestamp |> Array.toSeq)

let completedCriteria (achievements:Character.Achievements) =
    achievements.Criteria
    |> Seq.ofArray

let filterCriteria (completedCriteria:seq<int>) (available) =
    available
    |> Seq.where (fun id -> Seq.contains id completedCriteria)
    |> Seq.map string
    |> String.concat ":"
    |> sprintf "cri=%s"

let criteriaDate (achievements:Character.Achievements) =
    Seq.zip achievements.Criteria achievements.CriteriaTimestamp