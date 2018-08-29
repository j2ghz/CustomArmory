module Data

open FSharp.Data

type AllAchievements = JsonProvider<"Data/AllAchievements.json">

let categories = (AllAchievements.Load "Data/AllAchievements.json").Achievements
