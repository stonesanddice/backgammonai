using Xunit;
using EngineCore;

namespace EngineTests
{
    public class CubeEvaluatorTests
    {
        private readonly CubeEvaluator _evaluator = new();

        [Fact]
        public void CalculateCubelessEquity_50PercentWin_ReturnsZero()
        {
            var p = new Probabilities(Win: 0.5f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubelessEquity(p);
            Assert.Equal(0f, eq);
        }

        [Fact]
        public void CalculateCubelessEquity_100PercentWin_ReturnsOne()
        {
            var p = new Probabilities(Win: 1f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubelessEquity(p);
            Assert.Equal(1f, eq);
        }

        [Fact]
        public void CalculateCubelessEquity_100PercentLoss_ReturnsMinusOne()
        {
            var p = new Probabilities(Win: 0f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubelessEquity(p);
            Assert.Equal(-1f, eq);
        }

        [Fact]
        public void CalculateCubelessEquity_GammonsAffectEquity()
        {
            var pNoGammon = new Probabilities(Win: 0.6f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            var pWithGammon = new Probabilities(Win: 0.6f, WinGammon: 0.1f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            float eqNo = _evaluator.CalculateCubelessEquity(pNoGammon);
            float eqWith = _evaluator.CalculateCubelessEquity(pWithGammon);
            Assert.True(eqWith > eqNo);
        }

        [Fact]
        public void GetMoneyCubeAction_DoubleTake_WhenDoubleAndTakeIsBest()
        {
            // eqDoublePass > eqDoubleTake >= eqNoDouble => opponent passes, so we double (DoubleTake)
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.3f,
                eqDoubleTake: 0.5f,
                eqDoublePass: 0.7f);
            Assert.Equal(CubeAction.DoubleTake, action);
        }

        [Fact]
        public void GetMoneyCubeAction_DoublePass_WhenDoubleAndPassIsBest()
        {
            // eqDoubleTake >= eqDoublePass >= eqNoDouble => opponent takes, we double (DoublePass)
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.2f,
                eqDoubleTake: 0.6f,
                eqDoublePass: 0.5f);
            Assert.Equal(CubeAction.DoublePass, action);
        }

        [Fact]
        public void GetMoneyCubeAction_NoDouble_WhenNoDoubleIsBest()
        {
            // DP > ND > DT: passing is best for us, so we should not double
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.5f,
                eqDoubleTake: 0.3f,
                eqDoublePass: 0.6f);
            Assert.Equal(CubeAction.NoDouble, action);
        }

        [Fact]
        public void IsCubeLiveInMatch_CrawfordGame_ReturnsFalse()
        {
            bool live = _evaluator.IsCubeLiveInMatch(playerAway: 1, oppAway: 1, cubeValue: 1, isCrawfordGame: true);
            Assert.False(live);
        }

        [Fact]
        public void IsCubeLiveInMatch_BothAwayAboveCube_ReturnsTrue()
        {
            bool live = _evaluator.IsCubeLiveInMatch(playerAway: 5, oppAway: 5, cubeValue: 1, isCrawfordGame: false);
            Assert.True(live);
        }

        [Fact]
        public void IsCubeLiveInMatch_PlayerAwayEqualsCube_ReturnsFalse()
        {
            // 1-away vs 1-away, cube 1: winning this game wins the match for either
            bool live = _evaluator.IsCubeLiveInMatch(playerAway: 1, oppAway: 1, cubeValue: 1, isCrawfordGame: false);
            Assert.False(live);
        }

        [Fact]
        public void Probabilities_Lose_IsOneMinusWin()
        {
            var p = new Probabilities(Win: 0.65f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            Assert.Equal(0.35f, p.Lose, precision: 5);
        }

        [Fact]
        public void CalculateCubefulEquity_MoneyGame_ReturnsReasonableValue()
        {
            var match = new MatchState { MatchLength = 0 };
            var p = new Probabilities(Win: 0.6f, WinGammon: 0.05f, WinBackgammon: 0f, LoseGammon: 0.03f, LoseBackgammon: 0f);
            float eq = _evaluator.CalculateCubefulEquity(p, match);
            Assert.True(eq > -1.01f && eq < 1.01f);
        }
    }
}
