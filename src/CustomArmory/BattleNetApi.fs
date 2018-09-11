module BattleNetApi

type Character = FSharp.Data.JsonProvider<"SampleData/Character.json">
let characterUrl key server realm character = sprintf "https://%s.api.battle.net/wow/character/%s/%s?fields=achievements,quests,reputation&apikey=%s" server realm character key

let character = Character.AsyncLoad
let achievements (c:Character.Root) = 
    Array.zip c.Achievements.AchievementsCompleted c.Achievements.AchievementsCompletedTimestamp 
    |> Map.ofArray
let quests (c:Character.Root) = c.Quests
let reputations (c:Character.Root) = c.Reputation
