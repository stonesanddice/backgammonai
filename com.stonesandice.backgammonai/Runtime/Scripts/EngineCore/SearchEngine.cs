using System;
using System.Collections.Generic;

namespace EngineCore
{
    public class SearchEngine
    {
        private readonly NeuralNet _contactNet;
        private readonly NeuralNet _raceNet;
        private readonly BearoffEvaluator _bearoffEval;
        private readonly CubeEvaluator _cubeEval;
        private readonly BoardClassifier _classifier;

        // ADDED: The Transposition Table Cache
        private readonly EvaluationCache _cache;

        public SearchEngine(
            NeuralNet contactNet,
            NeuralNet? raceNet = null,
            BearoffEvaluator? bearoffEval = null,
            CubeEvaluator? cubeEval = null)
        {
            _contactNet = contactNet;
            _raceNet = raceNet ?? contactNet;
            _bearoffEval = bearoffEval ?? new BearoffEvaluator("");
            _cubeEval = cubeEval ?? new CubeEvaluator();
            _classifier = new BoardClassifier();

            // Initialize the cache to prevent out-of-memory errors during deep search
            _cache = new EvaluationCache(500000);
        }

        // The 21 distinct dice rolls and their probability weightings
        private static readonly (int die1, int die2, float probability)[] DistinctRolls = new[]
        {
            (1, 1, 1f/36f), (2, 2, 1f/36f), (3, 3, 1f/36f), (4, 4, 1f/36f), (5, 5, 1f/36f), (6, 6, 1f/36f),
            (2, 1, 2f/36f), (3, 1, 2f/36f), (3, 2, 2f/36f), (4, 1, 2f/36f), (4, 2, 2f/36f), (4, 3, 2f/36f),
            (5, 1, 2f/36f), (5, 2, 2f/36f), (5, 3, 2f/36f), (5, 4, 2f/36f), (6, 1, 2f/36f), (6, 2, 2f/36f),
            (6, 3, 2f/36f), (6, 4, 2f/36f), (6, 5, 2f/36f)
        };

        /// <summary>
        /// Analyzes the board using Expectiminimax.
        /// depth = 1 is standard evaluation. depth = 2 looks ahead to the opponent's best responses.
        /// </summary>
        public Turn? GetBestTurn(GameState currentState, MatchState match, int depth = 1)
        {
            List<Turn> legalTurns = MoveGenerator.GenerateLegalTurns(currentState);
            if (legalTurns.Count == 0) return null;

            Turn? bestTurn = null;
            float bestScore = float.MinValue; // We want to MAXIMIZE our equity

            foreach (var turn in legalTurns)
            {
                if (turn.ResultingState == null) continue;

                float score;
                if (depth <= 1)
                {
                    // 1-Ply: Just evaluate the board right now
                    score = EvaluateStatic(turn.ResultingState, match);
                }
                else
                {
                    // 2-Ply: Ask the Chance Node for the expected equity after the opponent rolls
                    score = GetExpectedEquity(turn.ResultingState, match, depth - 1);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTurn = turn;
                }
            }

            return bestTurn;
        }

        /// <summary>
        /// The CHANCE Node: Averages the opponent's best responses across all 21 dice rolls.
        /// </summary>
        private float GetExpectedEquity(GameState state, MatchState match, int depth)
        {
            float totalExpectedEquity = 0.0f;

            // Loop through all 21 possible dice rolls
            foreach (var roll in DistinctRolls)
            {
                // Create a hypothetical state where the opponent rolled these dice
                GameState oppState = CloneStateForOpponent(state, roll.die1, roll.die2);

                List<Turn> oppLegalTurns = MoveGenerator.GenerateLegalTurns(oppState);

                // If the opponent dances (no legal moves), the state doesn't change
                if (oppLegalTurns.Count == 0)
                {
                    totalExpectedEquity += EvaluateStatic(oppState, match) * roll.probability;
                    continue;
                }

                float worstScoreForUs = float.MaxValue; // Opponent wants to MINIMIZE our equity

                // Find the opponent's best move (the one that minimizes our score)
                foreach (var oppTurn in oppLegalTurns)
                {
                    if (oppTurn.ResultingState == null) continue;

                    float score;
                    if (depth <= 1)
                    {
                        score = EvaluateStatic(oppTurn.ResultingState, match);
                    }
                    else
                    {
                        // If we ever go to 3-ply, we recurse back to our turn here
                        score = GetExpectedEquity(oppTurn.ResultingState, match, depth - 1);
                    }

                    if (score < worstScoreForUs)
                    {
                        worstScoreForUs = score;
                    }
                }

                // Add the probability-weighted equity to the total expected value
                totalExpectedEquity += worstScoreForUs * roll.probability;
            }

            return totalExpectedEquity;
        }

        // Helper to swap the turn for the opponent's simulation
        private GameState CloneStateForOpponent(GameState state, int die1, int die2)
        {
            return new GameState
            {
                Player1Checkers = (int[])state.Player1Checkers.Clone(),
                Player2Checkers = (int[])state.Player2Checkers.Clone(),
                Dice1 = die1,
                Dice2 = die2,
                PlayerOnRoll = 1 - state.PlayerOnRoll, // Swap turn
                CubeValue = state.CubeValue,
                MatchLength = state.MatchLength,
                Player1Score = state.Player1Score,
                Player2Score = state.Player2Score
            };
        }

        /// <summary>
        /// Returns the evaluated equity of a board state from Player 1's perspective.
        /// </summary>
        private float EvaluateStatic(GameState state, MatchState match)
        {
            // 1. Generate unique Position ID to check the cache
            string posId = PositionId.Encode(state);
            float[]? p2Probs;

            // 2. CHECK CACHE FIRST!
            if (!_cache.TryGet(posId, out p2Probs) || p2Probs == null)
            {
                int[] onRoll = state.Player2Checkers;
                int[] waiting = state.Player1Checkers;

                PositionClass pc = _classifier.Classify(state);

                if (pc == PositionClass.BearoffTwoSided || pc == PositionClass.BearoffOneSided)
                {
                    uint id1 = _classifier.GetPositionBearoff(onRoll);
                    uint id2 = _classifier.GetPositionBearoff(waiting);
                    p2Probs = _bearoffEval.Evaluate(id1, id2, pc);
                }

                if (p2Probs == null)
                {
                    float[] features = FeatureEncoder.EncodeContact(onRoll, waiting);
                    p2Probs = _contactNet.Evaluate(features);
                }

                // STORE IN CACHE
                _cache.Store(posId, p2Probs);
            }

            // Invert to Player 1's perspective
            Probabilities p1Probs = new Probabilities(
                Win: 1.0f - p2Probs[0],
                WinGammon: p2Probs[3],
                WinBackgammon: p2Probs[4],
                LoseGammon: p2Probs[1],
                LoseBackgammon: p2Probs[2]
            );

            bool isMoneyGame = match.MatchLength == 0 || match.MatchLength == 9999;
            if (isMoneyGame)
                return _cubeEval.CalculateCubefulEquity(p1Probs, match);
            else
            {
                int p1Away = match.MatchLength - match.Player0Score;
                int p2Away = match.MatchLength - match.Player1Score;
                return _cubeEval.CalculateCubefulMwc(p1Probs, p1Away, p2Away, match.Cube, match.CubeOwner);
            }
        }

        public void ClearCache() => _cache.Clear();

        /// <summary>
        /// Returns the cubeful equity of a position from the perspective of the player on roll (Player 1).
        /// Used for analysis, CLI hints, and strength tests (no-blunder assertions).
        /// </summary>
        public float GetEquity(GameState state, MatchState match)
        {
            return EvaluateStatic(state, match);
        }
    }
}