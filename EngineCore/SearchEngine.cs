using System;
using System.Collections.Generic;

namespace EngineCore
{
    public class SearchEngine
    {
        private readonly NeuralNet _contactNet;
        private readonly NeuralNet _raceNet; // If you loaded gnubg's race net!
        private readonly BearoffEvaluator _bearoffEval;
        private readonly CubeEvaluator _cubeEval;
        private readonly BoardClassifier _classifier;

        public SearchEngine(
            NeuralNet contactNet, 
            NeuralNet raceNet = null,
            BearoffEvaluator bearoffEval = null, 
            CubeEvaluator cubeEval = null)
        {
            _contactNet = contactNet;
            _raceNet = raceNet ?? contactNet; // Fallback to contact if no race net provided
            _bearoffEval = bearoffEval ?? new BearoffEvaluator("");
            _cubeEval = cubeEval ?? new CubeEvaluator();
            _classifier = new BoardClassifier();
        }

        public Turn? GetBestTurn(GameState currentState, MatchState match, MatchState cube)
        {
            List<Turn> legalTurns = MoveGenerator.GenerateLegalTurns(currentState);
            if (legalTurns.Count == 0) return null;

            Turn? bestTurn = null;
            float bestScore = float.MinValue;
            
            foreach (var turn in legalTurns)
            {
                if (turn.ResultingState == null) continue;

                int[] onRoll = turn.ResultingState.Player2Checkers;
                int[] waiting = turn.ResultingState.Player1Checkers;

                // 1. Classify the resulting board
                PositionClass pc = _classifier.Classify(turn.ResultingState);

                float[] p2Probs;

                // 2. Route to the correct evaluator
                if (pc == PositionClass.Contact || pc == PositionClass.Crashed)
                {
                    float[] features = FeatureEncoder.EncodeContact(onRoll, waiting);
                    p2Probs = _contactNet.Evaluate(features);
                }
                else if (pc == PositionClass.Race)
                {
                    // If you have a dedicated race net, use it. Otherwise, GNUBG falls back to contact/race logic.
                    // float[] features = FeatureEncoder.EncodeRace(onRoll, waiting);
                    // p2Probs = _raceNet.Evaluate(features);
                    
                    // Fallback to contact net for now if Race net isn't wired up
                    float[] features = FeatureEncoder.EncodeContact(onRoll, waiting);
                    p2Probs = _contactNet.Evaluate(features); 
                }
                else // It's a Bearoff!
                {
                    uint id1 = _classifier.GetPositionBearoff(onRoll);
                    uint id2 = _classifier.GetPositionBearoff(waiting);
                    
                    // The bearoff database gives us EXACT mathematical win probabilities!
                    p2Probs = _bearoffEval.Evaluate(id1, id2, pc);
                }

                // 3. Invert Player 2's probabilities to get Player 1's perspective
                Probabilities p1Probs = new Probabilities(
                    Win: 1.0f - p2Probs[0],             
                    WinGammon: p2Probs[3],              
                    WinBackgammon: p2Probs[4],          
                    LoseGammon: p2Probs[1],             
                    LoseBackgammon: p2Probs[2]          
                );

                // 4. Calculate Cubeful Equity
                float score;
                bool isMoneyGame = match.MatchLength == 0 || match.MatchLength == 9999; 

                if (isMoneyGame)
                {
                    score = _cubeEval.CalculateCubefulEquity(p1Probs, match);
                }
                else
                {
                    int player1Away = match.MatchLength - match.Player0Score;
                    int player2Away = match.MatchLength - match.Player1Score;

                    score = _cubeEval.CalculateCubefulMwc(
                        p1Probs, player1Away, player2Away, match.Cube, match.CubeOwner);
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestTurn = turn;
                }
            }

            return bestTurn;
        }
    }
}