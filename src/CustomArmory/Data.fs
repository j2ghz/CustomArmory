module Data

open FSharp.Data

type AllAchievements = JsonProvider<"Data/AllAchievements.json">
type Character = JsonProvider<"Data/Kosiilspaan.json">

let categories = (AllAchievements.Load "Data/AllAchievements.json").Achievements
let character = (Character.Load "Data/Kosiilspaan.json")

let completedAchievements =
    Seq.zip (character.Achievements.AchievementsCompleted |> Array.toSeq) (character.Achievements.AchievementsCompletedTimestamp |> Array.toSeq)
    |> Map.ofSeq

let completedCriteria =
    character.Achievements.Criteria
    |> Seq.ofArray

let filterCriteria available =
    available
    |> Seq.where (fun id -> Seq.contains id completedCriteria)
    |> Seq.map string
    |> String.concat ":"
    |> sprintf "cri=%s"