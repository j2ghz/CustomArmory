module StorylinesTests

open FsUnit.Xunit
open Xunit


let character = BattleNetApi.Character.GetSample()

[<Fact>]
let ``Storylines can be processed``() =
    StorylineData.storylines
    |> List.map (Storylines.fromData character)
    |> should not' (be Empty)