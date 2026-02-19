// using EngineCore;
// using Xunit;
//
// namespace EngineTests;
//
// public class BoardClassifierTests
// {
//     private readonly BoardClassifier _classifier = new();
//
//     [Fact]
//     public void Classify_StartingPosition_IsContact()
//     {
//         // The standard starting position is the ultimate "Contact" state.
//         var state = PositionIdParser.Parse("4HPwATDbt/AABA");
//         Assert.Equal(PositionClass.Contact, _classifier.Classify(state));
//     }
//
//     [Fact]
//     public void Classify_DeepRace_IsRace()
//     {
//         // A late-game race where players have passed each other but aren't in home boards yet.
//         var state = new GameState();
//         // Player 1 furthest back is point 10 (index 9)
//         state.Player1Checkers[9] = 15;
//         // Player 2 furthest back is point 10 (index 9)
//         state.Player2Checkers[9] = 15;
//         
//         // Sum of back points (9 + 9 = 18) is <= 22, so no contact.
//         // Back points > 5, so it is a Race.
//         Assert.Equal(PositionClass.Race, _classifier.Classify(state));
//     }
//
//     [Fact]
//     public void Classify_CrunchedBoard_IsCrashed()
//     {
//         // GNUbg classifies positions as "Crashed" if a player is deeply crunched.
//         var state = new GameState();
//         
//         // Player 2 has 12 checkers on the 1-point and 3 elsewhere (total 15).
//         state.Player2Checkers[0] = 12; 
//         state.Player2Checkers[1] = 3;
//         
//         // Player 1 is still far back, so they are technically in "Contact".
//         state.Player1Checkers[20] = 15;
//
//         // IsCrashed logic: (Total 15 - Checkers on 1-point 12) = 3. 3 <= Threshold 6.
//         Assert.Equal(PositionClass.Crashed, _classifier.Classify(state));
//     }
//
//     [Fact]
//     public void Classify_SimpleBearoff_IsBearoffTwoSided()
//     {
//         // A position where both players are deep in their home boards.
//         var state = new GameState();
//         state.Player1Checkers[0] = 5; // 5 checkers on the 1-point
//         state.Player2Checkers[1] = 5; // 5 checkers on the 2-point
//
//         // PositionBearoff for these will be < 923.
//         Assert.Equal(PositionClass.BearoffTwoSided, _classifier.Classify(state));
//     }
//
//     [Fact]
//     public void Classify_ComplexBearoff_IsBearoffOneSided()
//     {
//         // A position with many checkers high up in the home board.
//         var state = new GameState();
//         state.Player1Checkers[5] = 15; // All 15 checkers on the 6-point
//         state.Player2Checkers[0] = 15;
//
//         // The Position ID for 15 checkers on the 6-point is 15503, which is > 923.
//         Assert.Equal(PositionClass.BearoffOneSided, _classifier.Classify(state));
//     }
//
//     [Fact]
//     public void Classify_OnePlayerFinished_IsOver()
//     {
//         var state = new GameState();
//         // Player 1 has borne everything off (all zeros).
//         state.Player2Checkers[0] = 15;
//
//         Assert.Equal(PositionClass.Over, _classifier.Classify(state));
//     }
// }