using System;
using System.Collections.Generic;

namespace EngineCore;

public class SearchEngine
{
    private readonly NeuralNet _brain;

    public SearchEngine(NeuralNet brain)
    {
        _brain = brain;
    }

    public Turn? GetBestTurn(GameState currentState)
    {
        // 1. Generate all legal turns
        List<Turn> legalTurns = MoveGenerator.GenerateLegalTurns(currentState);

        if (legalTurns.Count == 0) return null;

        Turn? bestTurn = null;
        float bestScore = float.MinValue;
        
        foreach (var turn in legalTurns)
        {
            if (turn.ResultingState == null) continue;

            // 2. Perspective: Since Player 1 just moved, Player 2 is "On Roll".
            // Our PositionIdParser already normalizes both arrays (0=Away, 23=Home).
            // We pass (OnRoll, Waiting) -> (Player 2, Player 1).
            int[] onRoll = turn.ResultingState.Player2Checkers;
            int[] waiting = turn.ResultingState.Player1Checkers;

            // 3. Evaluate the board
            float[] features = FeatureEncoder.EncodeContact(onRoll, waiting);
            _brain.Evaluate(features);

            // 4. Score: Maximize Player 1's chance (Minimize Opponent's Win Logit)
            float score = -_brain.LastLogits[0];

            if (score > bestScore)
            {
                bestScore = score;
                bestTurn = turn;
            }
        }

        return bestTurn;
    }
}