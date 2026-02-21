using Xunit;
using EngineCore;

namespace EngineTests
{
    /// <summary>
    /// Tests for CubeEvaluator match play functionality to achieve 100% coverage.
    /// </summary>
    public class CubeEvaluatorMatchPlayTests
    {
        private readonly CubeEvaluator _evaluator = new();

        [Fact]
        public void GetMoneyCubeAction_WithCanWinGammon_DoubleTake()
        {
            // DP > DT >= ND with canWinGammon = true
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.3f,
                eqDoubleTake: 0.5f,
                eqDoublePass: 0.7f,
                canWinGammon: true);
            Assert.Equal(CubeAction.DoubleTake, action);
        }

        [Fact]
        public void GetMoneyCubeAction_WithCanWinGammon_DoublePass()
        {
            // DT >= DP >= ND with canWinGammon = true
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.2f,
                eqDoubleTake: 0.6f,
                eqDoublePass: 0.5f,
                canWinGammon: true);
            Assert.Equal(CubeAction.DoublePass, action);
        }

        [Fact]
        public void GetMoneyCubeAction_WithCanWinGammon_TooGoodToDoublePass()
        {
            // ND > DT > DP with canWinGammon = true
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.8f,
                eqDoubleTake: 0.6f,
                eqDoublePass: 0.4f,
                canWinGammon: true);
            Assert.Equal(CubeAction.TooGoodToDoublePass, action);
        }

        [Fact]
        public void GetMoneyCubeAction_WithCanWinGammon_TooGoodToDouble()
        {
            // ND > DP > DT with canWinGammon = true
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.8f,
                eqDoubleTake: 0.3f,
                eqDoublePass: 0.5f,
                canWinGammon: true);
            Assert.Equal(CubeAction.TooGoodToDouble, action);
        }

        [Fact]
        public void GetMoneyCubeAction_WithCanWinGammon_NoDouble()
        {
            // DP > ND > DT with canWinGammon = true
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.5f,
                eqDoubleTake: 0.3f,
                eqDoublePass: 0.7f,
                canWinGammon: true);
            Assert.Equal(CubeAction.NoDouble, action);
        }

        [Fact]
        public void GetMoneyCubeAction_WithoutCanWinGammon_TooGoodToDoublePass_BecomesDoublePass()
        {
            // ND > DT > DP with canWinGammon = false
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.8f,
                eqDoubleTake: 0.6f,
                eqDoublePass: 0.4f,
                canWinGammon: false);
            Assert.Equal(CubeAction.DoublePass, action);
        }

        [Fact]
        public void GetMoneyCubeAction_WithoutCanWinGammon_TooGoodToDouble_BecomesNoDouble()
        {
            // ND > DP > DT with canWinGammon = false
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.8f,
                eqDoubleTake: 0.3f,
                eqDoublePass: 0.5f,
                canWinGammon: false);
            Assert.Equal(CubeAction.NoDouble, action);
        }

        [Fact]
        public void GetMoneyCubeAction_WithoutCanWinGammon_DT_GE_ND_GT_DP()
        {
            // DT >= ND > DP with canWinGammon = false
            CubeAction action = _evaluator.GetMoneyCubeAction(
                eqNoDouble: 0.5f,
                eqDoubleTake: 0.6f,
                eqDoublePass: 0.3f,
                canWinGammon: false);
            Assert.Equal(CubeAction.DoublePass, action);
        }

        [Fact]
        public void CalculateCubelessMwc_EvenPosition_Returns50Percent()
        {
            var p = new Probabilities(Win: 0.5f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            float mwc = _evaluator.CalculateCubelessMwc(p, playerAway: 5, oppAway: 5, cubeValue: 1);
            Assert.InRange(mwc, 0.45f, 0.55f); // Should be close to 50%
        }

        [Fact]
        public void CalculateCubelessMwc_StrongPosition_ReturnsHighMwc()
        {
            var p = new Probabilities(Win: 0.8f, WinGammon: 0.1f, WinBackgammon: 0f, LoseGammon: 0f, LoseBackgammon: 0f);
            float mwc = _evaluator.CalculateCubelessMwc(p, playerAway: 5, oppAway: 5, cubeValue: 1);
            Assert.InRange(mwc, 0.5f, 1.0f); // Strong position should have MWC above 50%
        }

        [Fact]
        public void CalculateCubelessMwc_WeakPosition_ReturnsLowMwc()
        {
            var p = new Probabilities(Win: 0.2f, WinGammon: 0f, WinBackgammon: 0f, LoseGammon: 0.1f, LoseBackgammon: 0f);
            float mwc = _evaluator.CalculateCubelessMwc(p, playerAway: 5, oppAway: 5, cubeValue: 1);
            Assert.InRange(mwc, 0.0f, 0.5f); // Weak position should have MWC below 50%
        }

        [Fact]
        public void GetMatchCubeAction_CubeNotLive_ReturnsNoDouble()
        {
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.5f,
                mwcDoubleTake: 0.6f,
                mwcDoublePass: 0.7f,
                canWinGammon: true,
                isCubeLive: false);
            Assert.Equal(CubeAction.NoDouble, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_DoubleTake()
        {
            // DP > DT >= ND
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.5f,
                mwcDoubleTake: 0.6f,
                mwcDoublePass: 0.7f,
                canWinGammon: true,
                isCubeLive: true);
            Assert.Equal(CubeAction.DoubleTake, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_DoublePass()
        {
            // DT >= DP >= ND
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.4f,
                mwcDoubleTake: 0.7f,
                mwcDoublePass: 0.6f,
                canWinGammon: true,
                isCubeLive: true);
            Assert.Equal(CubeAction.DoublePass, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_TooGoodToDoublePass_WithGammon()
        {
            // ND > DT > DP with canWinGammon = true
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.8f,
                mwcDoubleTake: 0.6f,
                mwcDoublePass: 0.4f,
                canWinGammon: true,
                isCubeLive: true);
            Assert.Equal(CubeAction.TooGoodToDoublePass, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_DoublePass_WithoutGammon()
        {
            // ND > DT > DP with canWinGammon = false
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.8f,
                mwcDoubleTake: 0.6f,
                mwcDoublePass: 0.4f,
                canWinGammon: false,
                isCubeLive: true);
            Assert.Equal(CubeAction.DoublePass, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_TooGoodToDouble_WithGammon()
        {
            // ND > DP > DT with canWinGammon = true
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.8f,
                mwcDoubleTake: 0.3f,
                mwcDoublePass: 0.5f,
                canWinGammon: true,
                isCubeLive: true);
            Assert.Equal(CubeAction.TooGoodToDouble, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_NoDouble_WithoutGammon()
        {
            // ND > DP > DT with canWinGammon = false
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.8f,
                mwcDoubleTake: 0.3f,
                mwcDoublePass: 0.5f,
                canWinGammon: false,
                isCubeLive: true);
            Assert.Equal(CubeAction.NoDouble, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_NoDouble_DP_GT_ND_GT_DT()
        {
            // DP > ND > DT
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.5f,
                mwcDoubleTake: 0.3f,
                mwcDoublePass: 0.7f,
                canWinGammon: true,
                isCubeLive: true);
            Assert.Equal(CubeAction.NoDouble, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_TooGoodToDoublePass_DT_GE_ND_GT_DP_WithGammon()
        {
            // DT >= ND > DP with canWinGammon = true
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.6f,
                mwcDoubleTake: 0.7f,
                mwcDoublePass: 0.4f,
                canWinGammon: true,
                isCubeLive: true);
            Assert.Equal(CubeAction.TooGoodToDoublePass, action);
        }

        [Fact]
        public void GetMatchCubeAction_CubeLive_DoublePass_DT_GE_ND_GT_DP_WithoutGammon()
        {
            // DT >= ND > DP with canWinGammon = false
            CubeAction action = _evaluator.GetMatchCubeAction(
                mwcNoDouble: 0.6f,
                mwcDoubleTake: 0.7f,
                mwcDoublePass: 0.4f,
                canWinGammon: false,
                isCubeLive: true);
            Assert.Equal(CubeAction.DoublePass, action);
        }

        [Fact]
        public void CalculateCubefulMwc_CubeDead_ReturnsCubelessMwc()
        {
            var p = new Probabilities(Win: 0.6f, WinGammon: 0.05f, WinBackgammon: 0f, LoseGammon: 0.03f, LoseBackgammon: 0f);
            // Both players 1-away with cube at 1 means cube is dead
            float mwc = _evaluator.CalculateCubefulMwc(p, playerAway: 1, oppAway: 1, cubeValue: 1, cubeOwner: -1);
            float cubelessMwc = _evaluator.CalculateCubelessMwc(p, playerAway: 1, oppAway: 1, cubeValue: 1);
            Assert.Equal(cubelessMwc, mwc, precision: 5);
        }

        [Fact]
        public void CalculateCubefulMwc_CenteredCube_ReturnsReasonableValue()
        {
            var p = new Probabilities(Win: 0.6f, WinGammon: 0.05f, WinBackgammon: 0f, LoseGammon: 0.03f, LoseBackgammon: 0f);
            float mwc = _evaluator.CalculateCubefulMwc(p, playerAway: 5, oppAway: 5, cubeValue: 1, cubeOwner: -1);
            Assert.InRange(mwc, 0.0f, 1.0f);
        }

        [Fact]
        public void CalculateCubefulMwc_OwnedCube_ReturnsReasonableValue()
        {
            var p = new Probabilities(Win: 0.6f, WinGammon: 0.05f, WinBackgammon: 0f, LoseGammon: 0.03f, LoseBackgammon: 0f);
            float mwc = _evaluator.CalculateCubefulMwc(p, playerAway: 5, oppAway: 5, cubeValue: 1, cubeOwner: 0);
            Assert.InRange(mwc, 0.0f, 1.0f);
        }

        [Fact]
        public void CalculateCubefulMwc_UnavailableCube_ReturnsReasonableValue()
        {
            var p = new Probabilities(Win: 0.6f, WinGammon: 0.05f, WinBackgammon: 0f, LoseGammon: 0.03f, LoseBackgammon: 0f);
            float mwc = _evaluator.CalculateCubefulMwc(p, playerAway: 5, oppAway: 5, cubeValue: 1, cubeOwner: 1);
            Assert.InRange(mwc, 0.0f, 1.0f);
        }
    }
}
