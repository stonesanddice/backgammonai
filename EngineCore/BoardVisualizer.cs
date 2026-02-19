using System;
using EngineCore;

namespace EngineCLI
{
    public static class BoardVisualizer
    {
        public static void PrintBoard(GameState state, MatchState? match = null)
        {
            Console.WriteLine("=============================================");
            if (match != null)
            {
                string matchType = match.MatchLength == 0 ? "Money Game" : $"{match.MatchLength}-Point Match";
                Console.WriteLine($"{matchType,-18} | Turn: Player {match.PlayerOnRoll}");
                Console.WriteLine($"Score: P0 ({match.Player0Score}) - P1 ({match.Player1Score}) | Dice: {match.Dice[0]}-{match.Dice[1]}");
                Console.WriteLine($"Cube : {match.Cube} {GetCubeOwnerStr(match.CubeOwner)}");
                Console.WriteLine($"Match ID    : {MatchId.Encode(match)}");
            }
            Console.WriteLine($"Position ID : {PositionId.Encode(state)}");
            Console.WriteLine("=============================================");

            // Top Header
            Console.WriteLine("+13-14-15-16-17-18--------19-20-21-22-23-24-+");

            // Top Half (Points 13-24 -> array indices 12-23)
            for (int row = 0; row < 5; row++)
            {
                string left = $" {C(state, 12, row)}  {C(state, 13, row)}  {C(state, 14, row)}  {C(state, 15, row)}  {C(state, 16, row)}  {C(state, 17, row)}";
                string right = $" {C(state, 18, row)}  {C(state, 19, row)}  {C(state, 20, row)}  {C(state, 21, row)}  {C(state, 22, row)}  {C(state, 23, row)}";
                Console.WriteLine($"|{left} |     |{right} |");
            }

            // The Bar
            Console.WriteLine("|                  | BAR |                  |");

            // Bottom Half (Points 12-1 -> array indices 11-0)
            for (int row = 4; row >= 0; row--)
            {
                string left = $" {C(state, 11, row)}  {C(state, 10, row)}  {C(state, 9, row)}  {C(state, 8, row)}  {C(state, 7, row)}  {C(state, 6, row)}";
                string right = $" {C(state, 5, row)}  {C(state, 4, row)}  {C(state, 3, row)}  {C(state, 2, row)}  {C(state, 1, row)}  {C(state, 0, row)}";
                Console.WriteLine($"|{left} |     |{right} |");
            }

            // Bottom Header
            Console.WriteLine("+12-11-10--9--8--7---------6--5--4--3--2--1-+");

            // Print Borne Off and Bar counts
            int p0Bar = state.Board[0, 24];
            int p1Bar = state.Board[1, 24];
            int p0Off = GetCheckersOffBoard(state, 0);
            int p1Off = GetCheckersOffBoard(state, 1);
            
            Console.WriteLine($"  Player 0 (O):  On Bar: {p0Bar,2}  |  Borne Off: {p0Off,2}");
            Console.WriteLine($"  Player 1 (X):  On Bar: {p1Bar,2}  |  Borne Off: {p1Off,2}");
            Console.WriteLine();
        }

        // Shortened method name to keep the interpolated strings clean
        private static string C(GameState state, int pointIndex, int row)
        {
            int p0Count = state.Board[0, pointIndex];
            int p1Count = state.Board[1, 23 - pointIndex]; // P1 view is mirrored

            int totalCheckers = p0Count > 0 ? p0Count : p1Count;
            char token = p0Count > 0 ? 'O' : (p1Count > 0 ? 'X' : ' ');

            if (row >= totalCheckers) return " ";

            // GNUBG logic for stacks > 5
            if (row == 4 && totalCheckers > 5)
            {
                return totalCheckers > 9 ? "+" : totalCheckers.ToString();
            }

            return token.ToString();
        }

        private static int GetCheckersOffBoard(GameState state, int player)
        {
            int totalOnBoardAndBar = 0;
            for (int i = 0; i <= 24; i++) 
            {
                totalOnBoardAndBar += state.Board[player, i];
            }
            return 15 - totalOnBoardAndBar;
        }

        private static string GetCubeOwnerStr(int owner) => owner switch
        {
            -1 => "(Centered)",
            0 => "(Player 0)",
            1 => "(Player 1)",
            _ => "(Unknown)"
        };
    }
}