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

        [Fact]
        public void GenerateLegalTurns_NoLegalMoves_ReturnsEmptyList()
        {
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 2
            };
            state.Player1Checkers[24] = 1; // One on bar
            // From bar we land on our points 18-23; opponent point = 23 - ourPoint, so block opponent 0-5
            for (int i = 0; i <= 5; i++) state.Player2Checkers[i] = 2;

            var turns = MoveGenerator.GenerateLegalTurns(state);

            Assert.Empty(turns);
        }

        [Fact]
        public void GenerateLegalTurns_Doubles_FourMovesOfSameDie()
        {
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 6,
                Dice2 = 6
            };
            state.Player1Checkers[23] = 4; // Four on 24-point; can use 6-6 four times

            var turns = MoveGenerator.GenerateLegalTurns(state);

            Assert.NotEmpty(turns);
            Assert.Contains(turns, t => t.Moves.Count == 4 && t.DiceUsed.Count == 4);
        }

        [Fact]
        public void GenerateLegalTurns_AllInHome_CanBearOff()
        {
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 6
            };
            state.Player1Checkers[0] = 1;  // 1-point
            state.Player1Checkers[5] = 14; // 6-point

            var turns = MoveGenerator.GenerateLegalTurns(state);

            Assert.NotEmpty(turns);
            Assert.Contains(turns, t => t.Moves.Any(m => m.To == -1));
        }

        [Fact]
        public void ApplyTurn_ProducesStateWithMovesApplied()
        {
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 3,
                Dice2 = 1
            };
            state.Player1Checkers[23] = 2;
            state.Player1Checkers[12] = 5;
            var turn = new Turn();
            turn.Moves.Add(new Move { From = 23, To = 20 }); // one checker 24->21
            turn.Moves.Add(new Move { From = 20, To = 19 }); // that checker 21->20
            turn.DiceUsed.AddRange(new[] { 3, 1 });

            var result = MoveGenerator.ApplyTurn(state, turn);

            Assert.Equal(1, result.Player1Checkers[23]); // one left on 24-point
            Assert.Equal(0, result.Player1Checkers[20]);
            Assert.Equal(1, result.Player1Checkers[19]); // one moved to 20-point
        }
    }
}