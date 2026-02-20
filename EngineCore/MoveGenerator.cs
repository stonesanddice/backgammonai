using System.Collections.Generic;
using System.Linq;

namespace EngineCore;

public class MoveGenerator
{
    public static List<Turn> GenerateLegalTurns(GameState currentState)
    {
        var allTurns = new List<Turn>();

        List<int> dice = currentState.Dice1 == currentState.Dice2
            ? new List<int> { currentState.Dice1, currentState.Dice1, currentState.Dice1, currentState.Dice1 }
            : new List<int> { currentState.Dice1, currentState.Dice2 };

        // 1. Generate the raw tree of every physically possible move combination
        FindMoves(currentState, dice, new List<Move>(), new List<int>(), allTurns);

        // 2. FILTER: Must play the maximum number of dice possible
        int maxDicePlayed = allTurns.Count > 0 ? allTurns.Max(t => t.Moves.Count) : 0;
        var validTurns = allTurns.Where(t => t.Moves.Count == maxDicePlayed).ToList();

        // 3. FILTER: If only 1 die could be played (and it wasn't a double), you MUST play the larger die
        if (maxDicePlayed == 1 && currentState.Dice1 != currentState.Dice2)
        {
            int maxDieRolled = Math.Max(currentState.Dice1, currentState.Dice2);

            // Did any of our 1-die turns manage to use the larger die?
            bool canPlayLarger = validTurns.Any(t => t.DiceUsed.Contains(maxDieRolled));

            if (canPlayLarger)
            {
                // If so, delete the turns that only used the smaller die
                validTurns = validTurns.Where(t => t.DiceUsed.Contains(maxDieRolled)).ToList();
            }
        }

        return validTurns;
    }

    private static void FindMoves(GameState state, List<int> remainingDice, List<Move> currentMoves, List<int> currentDiceUsed, List<Turn> allTurns)
    {
        if (remainingDice.Count == 0)
        {
            allTurns.Add(new Turn
            {
                Moves = new List<Move>(currentMoves),
                DiceUsed = new List<int>(currentDiceUsed),
                ResultingState = state
            });
            return;
        }

        bool madeAnyMove = false;

        // We only need to test distinct dice (to avoid duplicate branches on doubles)
        foreach (int die in remainingDice.Distinct())
        {
            for (int point = 24; point >= 0; point--)
            {
                if (IsLegalMove(state, point, die, out Move move))
                {
                    madeAnyMove = true;

                    GameState nextState = CloneAndApplyMove(state, move);

                    var nextDice = new List<int>(remainingDice);
                    nextDice.Remove(die);

                    currentMoves.Add(move);
                    currentDiceUsed.Add(die);

                    // Recurse deeper
                    FindMoves(nextState, nextDice, currentMoves, currentDiceUsed, allTurns);

                    // Backtrack
                    currentMoves.RemoveAt(currentMoves.Count - 1);
                    currentDiceUsed.RemoveAt(currentDiceUsed.Count - 1);
                }
            }
        }

        // If we have dice left but no legal moves exist, save the partial turn
        if (!madeAnyMove && currentMoves.Count > 0)
        {
            allTurns.Add(new Turn
            {
                Moves = new List<Move>(currentMoves),
                DiceUsed = new List<int>(currentDiceUsed),
                ResultingState = state
            });
        }
    }
    private static bool IsLegalMove(GameState state, int fromPoint, int die, out Move move)
    {
        // Default setup for the out parameter
        move = new Move { From = fromPoint, To = fromPoint - die, IsHit = false };
        int toPoint = move.To;

        // RULE 1: You must actually have a checker at the starting point
        if (state.Player1Checkers[fromPoint] == 0) return false;

        // RULE 2: The Bar Rule
        // If you have checkers on the bar (index 24), you MUST move from the bar.
        if (state.Player1Checkers[24] > 0 && fromPoint != 24) return false;

        // RULE 3: Normal Board Move (Landing on points 0 through 23)
        if (toPoint >= 0)
        {
            // The opponent's board is mirrored. Our point 23 is their point 0.
            int opponentPoint = 23 - toPoint;

            // Is the point blocked by 2 or more opponent checkers?
            if (state.Player2Checkers[opponentPoint] >= 2) return false;

            // Is it a hit? (Exactly 1 opponent checker)
            if (state.Player2Checkers[opponentPoint] == 1)
            {
                move.IsHit = true;
            }

            return true; // The move is completely legal!
        }

        // RULE 4: Bearing Off (toPoint < 0)
        // First, check if ALL of your checkers are in the home board (points 0-5)
        for (int i = 6; i <= 24; i++)
        {
            if (state.Player1Checkers[i] > 0) return false; // Found a checker outside home
        }

        // If it's an exact bearoff (lands exactly on -1), it's legal
        if (toPoint == -1) return true;

        // If it's an "overshoot" (e.g., rolling a 6 to move from the 4-point, which lands on -3),
        // it is ONLY legal if there are absolutely NO checkers on higher points.
        for (int i = fromPoint + 1; i <= 5; i++)
        {
            if (state.Player1Checkers[i] > 0) return false;
        }

        // If we passed the overshoot check, standardize the destination to -1
        move.To = -1;
        return true;
    }

    private static GameState CloneAndApplyMove(GameState state, Move move)
    {
        // Create a new state (deep copy the arrays so we don't mutate the original)
        var nextState = new GameState
        {
            Player1Checkers = (int[])state.Player1Checkers.Clone(),
            Player2Checkers = (int[])state.Player2Checkers.Clone(),

            // Pass along the match context (score, cube, etc.)
            CubeValue = state.CubeValue,
            MatchLength = state.MatchLength,
            Player1Score = state.Player1Score,
            Player2Score = state.Player2Score,
            PlayerOnRoll = state.PlayerOnRoll
        };

        // 1. Pick up the checker from the source point
        nextState.Player1Checkers[move.From]--;

        // 2. If it's not a bearoff, place it on the destination point
        if (move.To >= 0)
        {
            nextState.Player1Checkers[move.To]++;

            // 3. Handle Hits
            if (move.IsHit)
            {
                int opponentPoint = 23 - move.To;
                nextState.Player2Checkers[opponentPoint]--; // Remove opponent from the board
                nextState.Player2Checkers[24]++;            // Put opponent on the bar
            }
        }

        return nextState;
    }
}