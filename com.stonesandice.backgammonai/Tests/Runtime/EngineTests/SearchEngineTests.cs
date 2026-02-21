using Xunit;
using EngineCore;
using System.Linq;

namespace EngineTests
{
    public class SearchEngineTests
    {
        /// <summary>
        /// A minimal contact net (250->128->5) with zero weights so every position
        /// evaluates to the same equity; used to test search behavior without real weights.
        /// </summary>
        private static NeuralNet CreateDummyContactNet()
        {
            return new NeuralNet(250, 128, 5, 0, 0.1f, 1.0f);
        }

        [Fact]
        public void GetBestTurn_NoLegalMoves_ReturnsNull()
        {
            var engine = new SearchEngine(CreateDummyContactNet(), null, null, new CubeEvaluator());
            var match = new MatchState { MatchLength = 0 };

            // Player on roll has one on bar; opponent blocks all 6 landing points (our 18-23; opponent indices 0-5).
            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 2
            };
            state.Player1Checkers[24] = 1;
            for (int i = 0; i <= 5; i++) state.Player2Checkers[i] = 2;

            var turn = engine.GetBestTurn(state, match, 1);

            Assert.Null(turn);
        }

        [Fact]
        public void GetBestTurn_SingleLegalTurn_ReturnsThatTurn()
        {
            var engine = new SearchEngine(CreateDummyContactNet(), null, null, new CubeEvaluator());
            var match = new MatchState { MatchLength = 0 };

            var state = new GameState
            {
                Player1Checkers = new int[25],
                Player2Checkers = new int[25],
                Dice1 = 1,
                Dice2 = 2
            };
            state.Player1Checkers[23] = 1; // One checker on 24-point; 1 and 2 are legal

            var turn = engine.GetBestTurn(state, match, 1);

            Assert.NotNull(turn);
            Assert.True(turn!.Moves.Count >= 1);
        }

        [Fact]
        public void GetBestTurn_StartingPosition_ReturnsNonNullTurn()
        {
            var engine = new SearchEngine(CreateDummyContactNet(), null, null, new CubeEvaluator());
            var match = new MatchState { MatchLength = 0 };
            var state = PositionId.Decode("4HPwATDgc/ABMA");
            state.Dice1 = 3;
            state.Dice2 = 1;

            var turn = engine.GetBestTurn(state, match, 1);

            Assert.NotNull(turn);
            Assert.True(turn!.Moves.Count >= 1);
        }

        [Fact]
        public void ClearCache_DoesNotThrow()
        {
            var engine = new SearchEngine(CreateDummyContactNet(), null, null, new CubeEvaluator());
            engine.ClearCache();
        }
    }
}
