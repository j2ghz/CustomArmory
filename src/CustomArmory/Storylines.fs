module Storylines

open StorylineData

type Earned = bool
type LevelEarned = int
type RepEarnedStanding = int
type RepEarnedValue = int
type AchievementEarned = string*int64

type ProcessedStorylineItem =
| Step of StepTitle * StorylineItem list * StorylineItem list * Earned
| ParallelStep of StepTitle * StorylineItem list * StorylineItem list * Earned
| Achievement of AchievementId * AchievementEarned option
| Quest of QuestId * Earned
| Reputation of RepId * RepRequiredStanding * RepRequiredValue * Earned * RepEarnedStanding * RepEarnedValue
| Level of LevelRequired * Earned * LevelEarned