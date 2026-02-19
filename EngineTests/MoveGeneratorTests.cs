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
            // Arrange: Player has one checker on the bar and one on the 20-pt.
            // Dice: 1, 2. If they can't enter the 1, they can't move the 20-pt checker.
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 2
            };
            state.Player1Checkers[24] = 1; // Bar
            state.Player1Checkers[19] = 1; // 20-point
            
            // Block the 1-point (their destination from bar with a 1)
            // Note: Opponent point 22 is player 1's 1-point (23-1=22)
            state.Player2Checkers[22] = 2; 

            // Act
            var turns = MoveGenerator.GenerateLegalTurns(state);

            // Assert: The only legal moves must start from the Bar (index 24)
            // If they can't enter with the 1, they must enter with the 2.
            Assert.All(turns, t => Assert.Equal(24, t.Moves[0].From));
            Assert.DoesNotContain(turns, t => t.Moves.Any(m => m.From == 19));
        }

        [Fact]
        public void GenerateLegalTurns_BearingOff_RequiresAllInHome()
        {
            // Arrange: Checkers on 6-pt and 1-pt. Rolling a 1.
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 3 // Use a 3 to try to bear off
            };
            state.Player1Checkers[5] = 1; // 6-point (outside home is 6+, but 0-5 is home)
            state.Player1Checkers[0] = 1; // 1-point

            // Act
            var turns = MoveGenerator.GenerateLegalTurns(state);

            // Assert: Cannot bear off because the 6-point is not in home board (0-5)
            Assert.All(turns, t => Assert.All(t.Moves, m => Assert.NotEqual(-1, m.To)));
        }

        [Fact]
        public void GenerateLegalTurns_ForcedToPlayMaxDice()
        {
            // Arrange: Can play a 2 or a 5, but playing the 2 blocks the 5.
            // Dice: 2, 5. 
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 2,
                Dice2 = 5
            };
            state.Player1Checkers[10] = 1;
            // Block the destination of (10-2)-5 but allow (10-5)-2
            state.Player2Checkers[23 - 3] = 2; // Blocks the 3-point

            // Act
            var turns = MoveGenerator.GenerateLegalTurns(state);

            // Assert: Should only contain turns with 2 moves if possible
            int maxMoves = turns.Max(t => t.Moves.Count);
            Assert.All(turns, t => Assert.Equal(maxMoves, t.Moves.Count));
        }
    }
}