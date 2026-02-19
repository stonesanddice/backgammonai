using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EngineCore
{
    public class GameState
    {
        // Board State
        public int[] Player1Checkers { get; set; } = new int[25]; // Index 24 is Bar
        public int[] Player2Checkers { get; set; } = new int[25]; // Index 24 is Bar

        // Match State
        public int CubeValue { get; set; } = 1;
        public int CubeOwner { get; set; } = 3; // 0=P1, 1=P2, 3=Centered
        public int PlayerOnRoll { get; set; }
        public int PlayerToDecide { get; set; }

        // Dice
        public int Dice1 { get; set; }
        public int Dice2 { get; set; }

        // Score
        public int MatchLength { get; set; }
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
        
        // [Player, Point]
        // Player 0 is on roll, Player 1 is the opponent.
        // Points 0-23 are the board, index 24 is the Bar.
        public int[,] Board { get; } = new int[2, 25];

        // The test uses 1-24 for board points for readability
        public int GetCheckers(int player, int point)
        {
            // Convert 1-based point to 0-based array index
            return Board[player, point - 1];
        }

        public int GetCheckersOnBar(int player)
        {
            return Board[player, 24];
        }

        public int GetCheckersOffBoard(int player)
        {
            int totalOnBoardAndBar = 0;
            for (int i = 0; i <= 24; i++)
            {
                totalOnBoardAndBar += Board[player, i];
            }
            return 15 - totalOnBoardAndBar;
        }
    }
}
