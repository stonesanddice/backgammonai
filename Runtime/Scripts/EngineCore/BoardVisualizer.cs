using System;
using System.Linq;
using EngineCore;

namespace EngineCLI
{
    public static class BoardVisualizer
    {
        public static void PrintBoard(GameState state, MatchState? match = null)
        {
            // The Fix: Dynamically lock the absolute P0 and P1 arrays based on whose turn it is
            // so the visual board doesn't flip upside down every turn!
            int[] p0Checkers = state.PlayerOnRoll == 0 ? state.Player1Checkers : state.Player2Checkers;
            int[] p1Checkers = state.PlayerOnRoll == 0 ? state.Player2Checkers : state.Player1Checkers;

            Console.WriteLine("=============================================");
            if (match != null)
            {
                string matchType = match.MatchLength == 0 ? "Money Game" : $"{match.MatchLength}-Point Match";
                Console.WriteLine($"{matchType,-18} | Turn: Player {state.PlayerOnRoll}");
                Console.WriteLine($"Score: P0 ({match.Player0Score}) - P1 ({match.Player1Score}) | Dice: {state.Dice1}-{state.Dice2}");
                Console.WriteLine($"Cube : {match.Cube} {GetCubeOwnerStr(match.CubeOwner)}");
            }

            // Fallback in case PositionId is still wired to an old 2D array
            try { Console.WriteLine($"Position ID : {PositionId.Encode(state)}"); }
            catch { Console.WriteLine($"Position ID : N/A (Migrate to 1D arrays)"); }

            Console.WriteLine("=============================================");

            // Top Header
            Console.WriteLine("+13-14-15-16-17-18--------19-20-21-22-23-24-+");

            // Top Half (Points 13-24 -> array indices 12-23)
            for (int row = 0; row < 5; row++)
            {
                string left = $" {C(p0Checkers, p1Checkers, 12, row)}  {C(p0Checkers, p1Checkers, 13, row)}  {C(p0Checkers, p1Checkers, 14, row)}  {C(p0Checkers, p1Checkers, 15, row)}  {C(p0Checkers, p1Checkers, 16, row)}  {C(p0Checkers, p1Checkers, 17, row)}";
                string right = $" {C(p0Checkers, p1Checkers, 18, row)}  {C(p0Checkers, p1Checkers, 19, row)}  {C(p0Checkers, p1Checkers, 20, row)}  {C(p0Checkers, p1Checkers, 21, row)}  {C(p0Checkers, p1Checkers, 22, row)}  {C(p0Checkers, p1Checkers, 23, row)}";
                Console.WriteLine($"|{left} |     |{right} |");
            }

            // The Bar
            Console.WriteLine("|                  | BAR |                  |");

            // Bottom Half (Points 12-1 -> array indices 11-0)
            for (int row = 4; row >= 0; row--)
            {
                string left = $" {C(p0Checkers, p1Checkers, 11, row)}  {C(p0Checkers, p1Checkers, 10, row)}  {C(p0Checkers, p1Checkers, 9, row)}  {C(p0Checkers, p1Checkers, 8, row)}  {C(p0Checkers, p1Checkers, 7, row)}  {C(p0Checkers, p1Checkers, 6, row)}";
                string right = $" {C(p0Checkers, p1Checkers, 5, row)}  {C(p0Checkers, p1Checkers, 4, row)}  {C(p0Checkers, p1Checkers, 3, row)}  {C(p0Checkers, p1Checkers, 2, row)}  {C(p0Checkers, p1Checkers, 1, row)}  {C(p0Checkers, p1Checkers, 0, row)}";
                Console.WriteLine($"|{left} |     |{right} |");
            }

            // Bottom Header
            Console.WriteLine("+12-11-10--9--8--7---------6--5--4--3--2--1-+");

            int p0Bar = p0Checkers[24];
            int p1Bar = p1Checkers[24];
            int p0Off = 15 - p0Checkers.Sum();
            int p1Off = 15 - p1Checkers.Sum();

            Console.WriteLine($"  Player 0 (O):  On Bar: {p0Bar,2}  |  Borne Off: {p0Off,2}");
            Console.WriteLine($"  Player 1 (X):  On Bar: {p1Bar,2}  |  Borne Off: {p1Off,2}");
            Console.WriteLine();
        }

        private static string C(int[] p0Checkers, int[] p1Checkers, int pointIndex, int row)
        {
            int p0Count = p0Checkers[pointIndex];

            // P1's view is mirrored. P0's 24-point (index 23) is P1's 1-point (index 0).
            int p1Count = p1Checkers[23 - pointIndex];

            int totalCheckers = p0Count > 0 ? p0Count : p1Count;
            char token = p0Count > 0 ? 'O' : (p1Count > 0 ? 'X' : ' ');

            if (row >= totalCheckers) return " ";

            if (row == 4 && totalCheckers > 5)
            {
                return totalCheckers > 9 ? "+" : totalCheckers.ToString();
            }

            return token.ToString();
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