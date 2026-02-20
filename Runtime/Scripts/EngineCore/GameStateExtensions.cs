using System;
using System.Text;

namespace EngineCore
{
    public static class GameStateExtensions
    {
        // Dimensions and constants based on the 'board.py' ASCII standard
        private const int ASCII_BOARD_HEIGHT = 11;
        private const int ASCII_MAX_CHECKERS = 5;
        private const string ASCII_12_01 = "+12-11-10--9--8--7-------6--5--4--3--2--1-+";
        private const string ASCII_13_24 = "+13-14-15-16-17-18------19-20-21-22-23-24-+";

        public static void PrintBoard(this GameState state)
        {
            // Sync the 1D arrays to the 2D array so the PositionIdEncoder can read them
            for (int i = 0; i <= 24; i++)
            {
                state.Board[0, i] = state.Player1Checkers[i];
                state.Board[1, i] = state.Player2Checkers[i];
            }

            StringBuilder sb = new StringBuilder();

            sb.AppendLine($" Stones+Dice C#     Position ID: {PositionId.Encode(state)}");
            sb.AppendLine(" " + ASCII_12_01);

            string[,] matrix = GenerateCheckerMatrix(state);

            for (int row = 0; row < ASCII_BOARD_HEIGHT; row++)
            {
                sb.Append(row == ASCII_BOARD_HEIGHT / 2 ? "v|" : " |");

                for (int col = 0; col < 6; col++) sb.Append(matrix[row, col]);

                sb.Append("|");

                if (row == ASCII_BOARD_HEIGHT / 2)
                {
                    sb.Append("BAR");
                }
                else
                {
                    int p2Bar = state.Player2Checkers[24];
                    int p1Bar = state.Player1Checkers[24];

                    if (row < 5 && p2Bar > row) sb.Append(" X ");
                    else if (row > 5 && p1Bar > (10 - row)) sb.Append(" O ");
                    else sb.Append("   ");
                }

                sb.Append("|");

                for (int col = 6; col < 12; col++) sb.Append(matrix[row, col]);

                sb.Append("|");
                AppendMetadata(sb, state, row);
                sb.AppendLine();
            }

            sb.AppendLine(" " + ASCII_13_24);
            Console.Write(sb.ToString());
        }

        private static void AppendMetadata(StringBuilder sb, GameState state, int row)
        {
            if (row == 0) sb.Append("     P2: X (Opponent)");
            if (row == 1) sb.Append($"     pips: {CalculatePipCount(state.Player2Checkers)}");
            if (row == 9) sb.Append($"     pips: {CalculatePipCount(state.Player1Checkers)}");
            if (row == 10) sb.Append("     P1: O (You)");
        }

        private static string[,] GenerateCheckerMatrix(GameState state)
        {
            string[,] matrix = new string[ASCII_BOARD_HEIGHT, 12];
            for (int r = 0; r < ASCII_BOARD_HEIGHT; r++)
                for (int c = 0; c < 12; c++) matrix[r, c] = "   ";

            for (int i = 0; i < 6; i++)
            {
                // TOP HALF: Points 12-7 (Left) and 6-1 (Right)
                int topIdxLeft = 11 - i;
                int topIdxRight = 5 - i;
                FillColumn(matrix, i, state.Player1Checkers[topIdxLeft], state.Player2Checkers[23 - topIdxLeft], true);
                FillColumn(matrix, i + 6, state.Player1Checkers[topIdxRight], state.Player2Checkers[23 - topIdxRight], true);

                // BOTTOM HALF: Points 13-18 (Left) and 19-24 (Right)
                int botIdxLeft = 12 + i;
                int botIdxRight = 18 + i;
                FillColumn(matrix, i, state.Player1Checkers[botIdxLeft], state.Player2Checkers[23 - botIdxLeft], false);
                FillColumn(matrix, i + 6, state.Player1Checkers[botIdxRight], state.Player2Checkers[23 - botIdxRight], false);
            }
            return matrix;
        }

        private static void FillColumn(string[,] matrix, int col, int p1, int p2, bool isTop)
        {
            int count = p1 > 0 ? p1 : p2;
            if (count <= 0) return;

            string symbol = p1 > 0 ? " O " : " X ";

            for (int i = 0; i < Math.Min(count, ASCII_MAX_CHECKERS); i++)
            {
                int row = isTop ? i : (ASCII_BOARD_HEIGHT - 1 - i);

                if (count > ASCII_MAX_CHECKERS && i == ASCII_MAX_CHECKERS - 1)
                    matrix[row, col] = count < 10 ? $" {count} " : $"{count} ";
                else
                    matrix[row, col] = symbol;
            }
        }

        private static int CalculatePipCount(int[] checkers)
        {
            int pips = 0;
            for (int i = 0; i < 24; i++) pips += checkers[i] * (i + 1);
            pips += checkers[24] * 25;
            return pips;
        }
    }
}