module Character

open FSharp.Data
open System

type AllAchievements = JsonProvider<"Data/AllAchievements.json">
type Character = JsonProvider<"Data/Kosiilspaan.json">

let categories = (AllAchievements.Load "Data/AllAchievements.json").Achievements
let fromString = sprintf "Data/%s.json" >> Character.Load
let achievements (c:Character.Root) = c.Achievements
let name (c:Character.Root) = c.Name

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

let questCompleted (c:Character.Root) id =
    Array.contains id c.Quests