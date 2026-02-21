using System;
using System.Collections.Generic;
using System.Linq;
using EngineCore;
using Xunit;

namespace EngineTests
{
    /// <summary>
    /// High-level strength tests to ensure the engine plays at grandmaster level.
    /// Uses (1) no-blunder checks (chosen move within equity tolerance of optimal) and
    /// (2) reference positions with rollout-approved best moves.
    /// </summary>
    public class GrandmasterLevelTests
    {
        private readonly SearchEngine _engine;
        private readonly MatchState _match;

        public GrandmasterLevelTests()
        {
            string dataDir = FindDataDirectory();
            var nets = WeightParser.Load(System.IO.Path.Combine(dataDir, "gnubg.weights"));
            var contactNet = nets.First(n => n.InputCount == 250);
            _engine = new SearchEngine(contactNet, null, new BearoffEvaluator(dataDir), new CubeEvaluator());
            _match = new MatchState { MatchLength = 0 }; // Money game
        }

        /// <summary>
        /// For a given position, the engine's 2-ply choice must have equity within this margin of the best move.
        /// Allows for tie-breaking and floating-point variance; catches real blunders.
        /// </summary>
        private const float MaxEquityLossTolerance = 0.018f;

        /// <summary>Decode a position and ensure the player on roll's checkers are in Player1Checkers (MoveGenerator convention).</summary>
        private static GameState DecodePosition(string positionId)
        {
            GameState state = PositionId.Decode(positionId);
            if (state.PlayerOnRoll == 0)
            {
                var t = state.Player1Checkers;
                state.Player1Checkers = state.Player2Checkers;
                state.Player2Checkers = t;
            }
            return state;
        }

        [Fact]
        public void OnePly_Opening31_ChoosesOptimalOrNearOptimalMove()
        {
            // Rollout best for 3-1 is 8/5 6/5. At 1-ply, engine must choose that or a move within equity tolerance.
            GameState state = DecodePosition("4HPwATDgc/ABMA");
            state.Dice1 = 3;
            state.Dice2 = 1;

            _engine.ClearCache();
            Turn? best = _engine.GetBestTurn(state, _match, depth: 1);
            Assert.NotNull(best);

            string normalized = NormalizeMoveString(best!);
            bool isBookMove = normalized == "6/5 8/5" || normalized == "8/5 6/5";
            if (isBookMove) return;

            float bestEquity = GetBestEquityAmongLegalTurns(state, 1);
            float chosenEquity = GetEquityAfterTurn(state, best!);
            Assert.True(
                chosenEquity >= bestEquity - MaxEquityLossTolerance,
                $"3-1: chosen move {best} (equity {chosenEquity:F4}) must be within {MaxEquityLossTolerance} of best {bestEquity:F4}.");
        }

        [Fact]
        public void OnePly_Opening31_NoBlunder_ChosenMoveWithinToleranceOfBest()
        {
            AssertNoBlunder("4HPwATDgc/ABMA", 3, 1, depth: 1);
        }

        [Fact]
        public void OnePly_Opening61_NoBlunder_ChosenMoveWithinToleranceOfBest()
        {
            AssertNoBlunder("4HPwATDgc/ABMA", 6, 1, depth: 1);
        }

        [Fact]
        public void OnePly_Opening52_NoBlunder_ChosenMoveWithinToleranceOfBest()
        {
            AssertNoBlunder("4HPwATDgc/ABMA", 5, 2, depth: 1);
        }

        [Fact]
        public void OnePly_StartingPosition_64_NoBlunder()
        {
            AssertNoBlunder("4HPwATDgc/ABMA", 6, 4, depth: 1);
        }

        /// <summary>The 21 distinct opening rolls (doubles and 15 combinations).</summary>
        public static IEnumerable<object[]> All21OpeningRolls()
        {
            yield return new object[] { 1, 1 };
            yield return new object[] { 2, 2 };
            yield return new object[] { 3, 3 };
            yield return new object[] { 4, 4 };
            yield return new object[] { 5, 5 };
            yield return new object[] { 6, 6 };
            yield return new object[] { 2, 1 };
            yield return new object[] { 3, 1 };
            yield return new object[] { 3, 2 };
            yield return new object[] { 4, 1 };
            yield return new object[] { 4, 2 };
            yield return new object[] { 4, 3 };
            yield return new object[] { 5, 1 };
            yield return new object[] { 5, 2 };
            yield return new object[] { 5, 3 };
            yield return new object[] { 5, 4 };
            yield return new object[] { 6, 1 };
            yield return new object[] { 6, 2 };
            yield return new object[] { 6, 3 };
            yield return new object[] { 6, 4 };
            yield return new object[] { 6, 5 };
        }

        [Theory]
        [MemberData(nameof(All21OpeningRolls))]
        public void OnePly_All21OpeningMoves_NoBlunder(int die1, int die2)
        {
            AssertNoBlunder("4HPwATDgc/ABMA", die1, die2, depth: 1);
        }

        /// <summary>All (opening roll, reply roll) pairs: 21 openings × 21 replies = 441.</summary>
        public static IEnumerable<object[]> AllOpeningReplyRolls()
        {
            foreach (var open in All21OpeningRolls())
            {
                int openD1 = (int)open[0];
                int openD2 = (int)open[1];
                foreach (var reply in All21OpeningRolls())
                {
                    int replyD1 = (int)reply[0];
                    int replyD2 = (int)reply[1];
                    yield return new object[] { openD1, openD2, replyD1, replyD2 };
                }
            }
        }

        [Theory]
        [MemberData(nameof(AllOpeningReplyRolls))]
        public void OnePly_OpeningReplies_NoBlunder(int openingDie1, int openingDie2, int replyDie1, int replyDie2)
        {
            GameState start = DecodePosition("4HPwATDgc/ABMA");
            start.Dice1 = openingDie1;
            start.Dice2 = openingDie2;

            _engine.ClearCache();
            Turn? openingTurn = _engine.GetBestTurn(start, _match, depth: 1);
            Assert.NotNull(openingTurn);

            GameState afterOpening = MoveGenerator.ApplyTurn(start, openingTurn);
            SwapPerspective(afterOpening);

            afterOpening.Dice1 = replyDie1;
            afterOpening.Dice2 = replyDie2;

            AssertNoBlunderState(afterOpening, depth: 1);
        }

        /// <summary>Swap checker arrays so the opponent (now on roll) is in Player1Checkers.</summary>
        private static void SwapPerspective(GameState state)
        {
            var t = state.Player1Checkers;
            state.Player1Checkers = state.Player2Checkers;
            state.Player2Checkers = t;
            state.PlayerOnRoll = 0;
        }

        /// <summary>
        /// Asserts that the engine's chosen move has equity within tolerance of the best possible.
        /// </summary>
        private void AssertNoBlunder(string positionId, int die1, int die2, int depth = 1)
        {
            GameState state = DecodePosition(positionId);
            state.Dice1 = die1;
            state.Dice2 = die2;

            _engine.ClearCache();
            Turn? chosen = _engine.GetBestTurn(state, _match, depth);
            Assert.NotNull(chosen);

            float bestEquity = GetBestEquityAmongLegalTurns(state, depth);
            float chosenEquity = depth <= 1
                ? GetEquityAfterTurn(state, chosen!)
                : GetChosenTurn2PlyEquity(state, chosen!);

            Assert.True(
                chosenEquity >= bestEquity - MaxEquityLossTolerance,
                $"Position {positionId} {die1}-{die2}: chosen equity {chosenEquity:F4} must be >= best {bestEquity:F4} - {MaxEquityLossTolerance}. Move: {chosen}");
        }

        /// <summary>Same as AssertNoBlunder but for an already-built state (e.g. after opening, for reply tests).</summary>
        private void AssertNoBlunderState(GameState state, int depth = 1)
        {
            _engine.ClearCache();
            Turn? chosen = _engine.GetBestTurn(state, _match, depth);
            Assert.NotNull(chosen);

            float bestEquity = GetBestEquityAmongLegalTurns(state, depth);
            float chosenEquity = depth <= 1
                ? GetEquityAfterTurn(state, chosen!)
                : GetChosenTurn2PlyEquity(state, chosen!);

            Assert.True(
                chosenEquity >= bestEquity - MaxEquityLossTolerance,
                $"Dice {state.Dice1}-{state.Dice2}: chosen equity {chosenEquity:F4} must be >= best {bestEquity:F4} - {MaxEquityLossTolerance}. Move: {chosen}");
        }

        private float GetBestEquityAmongLegalTurns(GameState state, int depth)
        {
            var turns = MoveGenerator.GenerateLegalTurns(state);
            float best = float.MinValue;
            foreach (var turn in turns)
            {
                if (turn.ResultingState == null) continue;
                float eq = depth <= 1
                    ? _engine.GetEquity(turn.ResultingState, _match)
                    : GetExpectedEquityForTurn(state, turn);
                if (eq > best) best = eq;
            }
            return best;
        }

        /// <summary>2-ply equity after we play the given turn (expected value over opponent's rolls and best replies).</summary>
        private float GetChosenTurn2PlyEquity(GameState state, Turn turn)
        {
            return GetExpectedEquityForTurn(state, turn);
        }

        private float GetEquityAfterTurn(GameState state, Turn turn)
        {
            var after = MoveGenerator.ApplyTurn(state, turn);
            return _engine.GetEquity(after, _match);
        }

        private float GetExpectedEquityForTurn(GameState state, Turn turn)
        {
            if (turn.ResultingState == null) return float.MinValue;
            // 2-ply: we play this turn; then opponent rolls 21 possibilities and plays best for them.
            float total = 0f;
            foreach (var (d1, d2, prob) in DistinctRolls())
            {
                var oppState = CloneForOpponent(turn.ResultingState, d1, d2);
                var oppTurns = MoveGenerator.GenerateLegalTurns(oppState);
                float worst = float.MaxValue;
                if (oppTurns.Count == 0)
                {
                    worst = _engine.GetEquity(oppState, _match);
                }
                else
                {
                    foreach (var ot in oppTurns)
                    {
                        if (ot.ResultingState == null) continue;
                        float eq = _engine.GetEquity(ot.ResultingState, _match);
                        if (eq < worst) worst = eq;
                    }
                }
                total += worst * prob;
            }
            return total;
        }

        private static (int d1, int d2, float prob)[] DistinctRolls()
        {
            return new[]
            {
                (1, 1, 1f/36f), (2, 2, 1f/36f), (3, 3, 1f/36f), (4, 4, 1f/36f), (5, 5, 1f/36f), (6, 6, 1f/36f),
                (2, 1, 2f/36f), (3, 1, 2f/36f), (3, 2, 2f/36f), (4, 1, 2f/36f), (4, 2, 2f/36f), (4, 3, 2f/36f),
                (5, 1, 2f/36f), (5, 2, 2f/36f), (5, 3, 2f/36f), (5, 4, 2f/36f), (6, 1, 2f/36f), (6, 2, 2f/36f),
                (6, 3, 2f/36f), (6, 4, 2f/36f), (6, 5, 2f/36f)
            };
        }

        private static GameState CloneForOpponent(GameState state, int die1, int die2)
        {
            // Opponent is now on roll; MoveGenerator expects the player on roll in Player1Checkers.
            return new GameState
            {
                Player1Checkers = (int[])state.Player2Checkers.Clone(),
                Player2Checkers = (int[])state.Player1Checkers.Clone(),
                Dice1 = die1,
                Dice2 = die2,
                PlayerOnRoll = 1 - state.PlayerOnRoll,
                CubeValue = state.CubeValue,
                MatchLength = state.MatchLength,
                Player1Score = state.Player1Score,
                Player2Score = state.Player2Score
            };
        }

        private static string NormalizeMoveString(Turn turn)
        {
            var parts = turn.Moves.Select(m => m.ToString()).OrderBy(s => s, StringComparer.Ordinal).ToList();
            return string.Join(" ", parts);
        }

        private static string FindDataDirectory()
        {
            var currentDir = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
            while (currentDir != null)
            {
                string potential = System.IO.Path.Combine(currentDir.FullName, "com.stonesandice.backgammonai", "Runtime", "Data");
                if (System.IO.Directory.Exists(potential)) return potential;
                string rootData = System.IO.Path.Combine(currentDir.FullName, "Data");
                if (System.IO.Directory.Exists(rootData)) return rootData;
                currentDir = currentDir.Parent;
            }
            throw new System.IO.DirectoryNotFoundException("Data folder not found.");
        }
    }
}
