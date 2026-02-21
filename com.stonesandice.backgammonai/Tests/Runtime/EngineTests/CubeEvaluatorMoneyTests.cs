using Xunit;
using EngineCore;

namespace EngineTests
{
    /// <summary>
    /// Additional tests for CubeEvaluator money play to cover edge cases and branches.
    /// </summary>
    public class CubeEvaluatorMoneyTests
    {
        private readonly CubeEvaluator _evaluator = new();

        [Fact]
        public void CalculateCubelessEquity_WithJacobyRule_CenteredCube_IgnoresGammons()
        {
            var match = new MatchState
            {
                MatchLength = 0, // Money game
                JacobyRule = true,
                CubeOwner = -1 // Centered
            };

            var pWithGammon = new Probabilities(Win: 0.6f, WinGammon: 0.2f, WinBackgammon: 0f, LoseGammon: 0.1f, LoseBackgammon: 0f);
            var pNoGammon = new Probabilities(Win: 0.6f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);

            float eqWith = _evaluator.CalculateCubelessEquity(pWithGammon, match);
            float eqNo = _evaluator.CalculateCubelessEquity(pNoGammon, match);

            // With Jacoby rule and centered cube, gammons should not count
            Assert.Equal(eqNo, eqWith, precision: 5);
        }

        [Fact]
        public void CalculateCubelessEquity_WithJacobyRule_OwnedCube_CountsGammons()
        {
            var match = new MatchState
            {
                MatchLength = 0, // Money game
                JacobyRule = true,
                CubeOwner = 0 // Owned
            };

            var pWithGammon = new Probabilities(Win: 0.6f, WinGammon: 0.2f, WinBackgammon: 0f, LoseGammon: 0.1f, LoseBackgammon: 0f);
            var pNoGammon = new Probabilities(Win: 0.6f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);

            float eqWith = _evaluator.CalculateCubelessEquity(pWithGammon, match);
            float eqNo = _evaluator.CalculateCubelessEquity(pNoGammon, match);

            // With owned cube, gammons should count even with Jacoby rule
            Assert.True(eqWith > eqNo);
        }

        [Fact]
        public void CalculateCubefulEquity_VeryLowWinProb_ReturnsEarlyWithCubeless()
        {
            var match = new MatchState { MatchLength = 0, CubeOwner = -1 };
            var p = new Probabilities(Win: 0.0000001f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0.5f, LoseBackgammon: 0f);

            float cubeful = _evaluator.CalculateCubefulEquity(p, match);
            float cubeless = _evaluator.CalculateCubelessEquity(p, match);

            // Should return cubeless equity when win prob is too low
            Assert.Equal(cubeless, cubeful, precision: 5);
        }

        [Fact]
        public void CalculateCubefulEquity_VeryHighWinProb_ReturnsEarlyWithCubeless()
        {
            var match = new MatchState { MatchLength = 0, CubeOwner = -1 };
            var p = new Probabilities(Win: 0.9999999f, WinGammon: 0.5f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);

            float cubeful = _evaluator.CalculateCubefulEquity(p, match);
            float cubeless = _evaluator.CalculateCubelessEquity(p, match);

            // Should return cubeless equity when win prob is too high
            Assert.Equal(cubeless, cubeful, precision: 5);
        }

        [Fact]
        public void CalculateCubefulEquity_CenteredCube_WithJacobyRule_LowWinProb()
        {
            var match = new MatchState
            {
                MatchLength = 0,
                JacobyRule = true,
                CubeOwner = -1,
                PlayerOnRoll = 0
            };

            var p = new Probabilities(Win: 0.2f, WinGammon: 0.02f, WinBackgammon: 0f, LoseGammon: 0.1f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubefulEquity(p, match);

            Assert.InRange(eq, -2.0f, 0.0f);
        }

        [Fact]
        public void CalculateCubefulEquity_CenteredCube_WithJacobyRule_HighWinProb()
        {
            var match = new MatchState
            {
                MatchLength = 0,
                JacobyRule = true,
                CubeOwner = -1,
                PlayerOnRoll = 0
            };

            var p = new Probabilities(Win: 0.8f, WinGammon: 0.1f, WinBackgammon: 0f, LoseGammon: 0.02f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubefulEquity(p, match);

            Assert.InRange(eq, 0.0f, 2.0f);
        }

        [Fact]
        public void CalculateCubefulEquity_OwnedCube_LowWinProb()
        {
            var match = new MatchState
            {
                MatchLength = 0,
                CubeOwner = 0,
                PlayerOnRoll = 0
            };

            var p = new Probabilities(Win: 0.3f, WinGammon: 0.03f, WinBackgammon: 0f, LoseGammon: 0.1f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubefulEquity(p, match);

            Assert.InRange(eq, -2.0f, 1.0f);
        }

        [Fact]
        public void CalculateCubefulEquity_OwnedCube_HighWinProb()
        {
            var match = new MatchState
            {
                MatchLength = 0,
                CubeOwner = 0,
                PlayerOnRoll = 0
            };

            var p = new Probabilities(Win: 0.75f, WinGammon: 0.1f, WinBackgammon: 0f, LoseGammon: 0.03f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubefulEquity(p, match);

            Assert.InRange(eq, 0.0f, 2.0f);
        }

        [Fact]
        public void CalculateCubefulEquity_UnavailableCube_LowWinProb()
        {
            var match = new MatchState
            {
                MatchLength = 0,
                CubeOwner = 1, // Opponent owns it
                PlayerOnRoll = 0
            };

            var p = new Probabilities(Win: 0.25f, WinGammon: 0.02f, WinBackgammon: 0f, LoseGammon: 0.1f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubefulEquity(p, match);

            Assert.InRange(eq, -2.0f, 0.0f);
        }

        [Fact]
        public void CalculateCubefulEquity_UnavailableCube_HighWinProb()
        {
            var match = new MatchState
            {
                MatchLength = 0,
                CubeOwner = 1, // Opponent owns it
                PlayerOnRoll = 0
            };

            var p = new Probabilities(Win: 0.7f, WinGammon: 0.08f, WinBackgammon: 0f, LoseGammon: 0.04f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubefulEquity(p, match);

            Assert.InRange(eq, -1.0f, 2.0f);
        }

        [Fact]
        public void CalculateCubefulEquity_CustomCubeEfficiency()
        {
            var match = new MatchState { MatchLength = 0, CubeOwner = -1 };
            var p = new Probabilities(Win: 0.6f, WinGammon: 0.05f, WinBackgammon: 0f, LoseGammon: 0.03f, LoseBackgammon: 0f);

            float eqDefault = _evaluator.CalculateCubefulEquity(p, match);
            float eqLowEfficiency = _evaluator.CalculateCubefulEquity(p, match, cubeEfficiency: 0.3f);
            float eqHighEfficiency = _evaluator.CalculateCubefulEquity(p, match, cubeEfficiency: 0.9f);

            // Different cube efficiencies should produce different results
            Assert.NotEqual(eqDefault, eqLowEfficiency);
            Assert.NotEqual(eqDefault, eqHighEfficiency);
        }
    }
}
