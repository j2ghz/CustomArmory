module BattleNetApi

open FSharp.Data
open System.IO

type Character = FSharp.Data.JsonProvider<"SampleData/Character.json">
let characterUrl key server realm character = sprintf "https://%s.api.battle.net/wow/character/%s/%s?fields=achievements,quests,reputation&apikey=%s" server realm character key

let character url =
    async {
        let! content = Http.AsyncRequest url
        printfn "Blizzard API: %s/%s %s/%s Reset: %s" content.Headers.["X-Plan-QPS-Current"] content.Headers.["X-Plan-QPS-Allotted"] content.Headers.["X-Plan-Quota-Current"] content.Headers.["X-Plan-Quota-Allotted"] content.Headers.["X-Plan-Quota-Reset"]
        return match content.Body with
               | Text t -> Character.Parse t
               | Binary b -> (new StreamReader(new MemoryStream(b))).ReadToEnd() |> Character.Parse
    }
    
let achievements (c:Character.Root) = 
    Array.zip c.Achievements.AchievementsCompleted c.Achievements.AchievementsCompletedTimestamp 
    |> Map.ofArray
let quests (c:Character.Root) = c.Quests
let reputations (c:Character.Root) = c.Reputation
