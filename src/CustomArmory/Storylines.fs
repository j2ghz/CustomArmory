module Storylines

open StorylineData
open BattleNetApi
open System.Threading.Tasks

type Earned = bool
type LevelEarned = int
type RepEarnedStanding = int
type RepEarnedValue = int
type AchievementEarned = string * int64
type Completition = int * int

type ProcessedStorylineItem =
| TextHeading of Title * ProcessedStorylineItem * Completition
| ItemHeading of ProcessedStorylineItem * ProcessedStorylineItem * Completition
| Sequential of ProcessedStorylineItem list * Completition
| Parallel of ProcessedStorylineItem list * Completition
| Achievement of AchievementId * AchievementEarned option
| Quest of QuestId * Earned
| Reputation of RepId * RepRequiredStanding * RepRequiredValue * (Earned * RepEarnedStanding * RepEarnedValue) option
| Level of LevelRequired * Earned * LevelEarned

let eq a b = a = b
let percent (a,b) = a*100/b
let completed (a,b) = a = b
let earned = function
    | Achievement (_,opt) ->
        opt
        |> Option.isSome
    | Quest(_,e) -> e
    | Reputation(_,_,_,opt) ->
        opt
        |> Option.map (fun (e,_,_) -> e)
        |> Option.defaultValue false
    | Level (_,e,_) -> e
    | ItemHeading (_,_,c) ->
        completed c
    | TextHeading (_,_,c) ->
        completed c
    | Sequential (_,c) ->
        completed c
    | Parallel (_,c) ->
        completed c

let completedFolder (state:Completition) (item:ProcessedStorylineItem) =
    let (finished,all) = state
    match (earned item) with
    | true -> (finished+1,all+1)
    | false -> (finished,all+1)

let rec fromData c item =
    let fromData' = fromData c
    match item with
    | StorylineData.Parallel(SLIs) ->
        let pSLIs = SLIs |> List.map fromData'
        Parallel(
            pSLIs,
            pSLIs |> List.fold completedFolder (0,0)
        )
    | StorylineData.Sequential(SLIs) ->
        let pSLIs = SLIs |> List.map fromData'
        Sequential(
            pSLIs,
            (
                pSLIs
                |> List.tryFindIndexBack earned
                |> Option.defaultValue 0,
                List.length pSLIs
            )
        )
    | StorylineData.ItemHeading(head,item) ->
        let pItem = fromData' item
        ItemHeading(
            fromData' head,
            pItem,
            (pItem |> earned |> (function | true -> 1 | false -> 0) ,1)
        )
    | StorylineData.TextHeading (t,item) ->
        let pItem = fromData' item
        TextHeading(
            t,
            pItem,
            (pItem |> earned |> (function | true -> 1 | false -> 0) ,1)
        )
    | StorylineData.Achievement(id) ->
        Achievement(
            id,
            c
                |> achievements
                |> Map.tryFind id
                |> Option.bind (function time -> AchievementEarned(c.Name, time) |> Some )
            )
    | StorylineData.Quest(id) ->
        Quest(
            id,
            c
            |> quests
            |> Array.contains id
        )
    | StorylineData.Reputation(id,standing,value) ->
        let rep = c.Reputation
                |> Array.tryFind (fun rep -> rep.Id = id)
                |> Option.map (fun r -> (r.Standing,r.Value))

        Reputation(
            id,
            standing,
            value,
            rep |> Option.map (fun (s,v) ->
                s >= standing && v >= value,
                s,
                v
                )
        )
    | StorylineData.Level(n) ->
        Level( n, c.Level >= n, c.Level )