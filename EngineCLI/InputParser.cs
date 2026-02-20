using System;
using System.Collections.Generic;
using EngineCore;

namespace EngineCLI
{
    public static class InputParser
    {
        public static Turn? ParseHumanTurn(string input, GameState state)
        {
            try
            {
                var turn = new Turn();
                // Split "24/20 13/9" into ["24/20", "13/9"]
                string[] moveStrings = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                foreach (var s in moveStrings)
                {
                    // Split "24/20" into ["24", "20"]
                    string[] parts = s.Split('/');

                    int from = ParsePoint(parts[0]);
                    int to = ParsePoint(parts[1]);

                    // Create the move object
                    var move = new Move { From = from, To = to };

                    // Basic Hit Detection for the human move
                    if (to >= 0)
                    {
                        int oppPoint = 23 - to;
                        if (state.Player1Checkers[oppPoint] == 1) move.IsHit = true;
                    }

                    turn.Moves.Add(move);
                }
                return turn;
            }
            catch { return null; }
        }

        private static int ParsePoint(string p)
        {
            p = p.ToLower();
            if (p == "bar") return 24;
            if (p == "off") return -1;

            // Convert human 1-24 to array 0-23
            return int.Parse(p) - 1;
        }
    }
}