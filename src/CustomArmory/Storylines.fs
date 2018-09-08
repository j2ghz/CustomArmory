module Storylines

open StorylineData

type Earned = bool
type LevelEarned = int
type RepEarnedStanding = int
type RepEarnedValue = int
type AchievementEarned = string * int64

type ProcessedStorylineItem =
| Step of StepTitle * ProcessedStorylineItem list * ProcessedStorylineItem list
| ParallelStep of StepTitle * ProcessedStorylineItem list * ProcessedStorylineItem list
| Achievement of AchievementId * AchievementEarned option
| Quest of QuestId * Earned
| Reputation of RepId * RepRequiredStanding * RepRequiredValue * Earned * RepEarnedStanding * RepEarnedValue
| Level of LevelRequired * Earned * LevelEarned

let eq a b = a = b

let rec fromData c = function
    | StorylineData.Step(title,required,steps) ->
        Step(
            title,
            required |> List.map (fromData c),
            steps |> List.map (fromData c)
            )
    | StorylineData.ParallelStep(title,required,steps) ->
        ParallelStep(
            title,
            required |> List.map (fromData c),
            steps |> List.map (fromData c)
            )
    | StorylineData.Achievement(id) ->
        Achievement(
            id,
            c
                |> Character.achievements
                |> Character.completedAchievements
                |> Seq.tryFind (fst >> eq id)
                |> Option.bind (function time -> AchievementEarned(c.Name,snd time) |> Some )
            )