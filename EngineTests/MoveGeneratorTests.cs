using Xunit;
using EngineCore;
using System.Linq;

namespace EngineCore.Tests
{
    public class MoveGeneratorTests
    {
        [Fact]
        public void GenerateLegalTurns_MustMoveFromBarFirst()
        {
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 2
            };
            state.Player1Checkers[24] = 2; // TWO on the Bar
            state.Player1Checkers[19] = 1; // 20-point

            // FIX: To block a '1' from the bar (landing on Index 23), 
            // we must block the opponent's Index 0 (23 - 23 = 0).
            state.Player2Checkers[0] = 2; 

            var turns = MoveGenerator.GenerateLegalTurns(state);

            // Assert: They can only play the 2 from the bar. 
            Assert.All(turns, t => Assert.Single(t.Moves)); 
            Assert.All(turns, t => Assert.Equal(24, t.Moves[0].From)); 
            Assert.DoesNotContain(turns, t => t.Moves.Any(m => m.From == 19)); 
        }

        [Fact]
        public void GenerateLegalTurns_BearingOff_RequiresAllInHome()
        {
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 3 
            };
            
            // FIX: Placed on Index 9 (The 10-point). 
            // Moving a 3 will land it on the 7-point, which is still outside the home board.
            state.Player1Checkers[9] = 1; 
            state.Player1Checkers[0] = 1; 

            var turns = MoveGenerator.GenerateLegalTurns(state);

            // Assert: Cannot bear off because the 10-point checker will never reach home this turn.
            Assert.All(turns, t => Assert.All(t.Moves, m => Assert.NotEqual(-1, m.To)));
        }

        [Fact]
        public void GenerateLegalTurns_ForcedToPlayMaxDice()
        {
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 2,
                Dice2 = 5
            };
            state.Player1Checkers[10] = 1;
            state.Player2Checkers[20] = 2; // Blocks the 3-point (23 - 3 = 20)

            var turns = MoveGenerator.GenerateLegalTurns(state);

            int maxMoves = turns.Max(t => t.Moves.Count);
            Assert.All(turns, t => Assert.Equal(maxMoves, t.Moves.Count));
        }
    }
}