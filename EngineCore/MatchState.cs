namespace EngineCore
{
    public class MatchState
    {
        public int Cube { get; set; } = 1; // Actual value (1, 2, 4, 8...)
        public int CubeOwner { get; set; } = -1; // 0 = Player 0, 1 = Player 1, -1 = Centered
        public int PlayerOnRoll { get; set; } // fMove in C
        public bool IsCrawford { get; set; }
        public int GameState { get; set; } // gs in C (0=playing, 1=game over, etc.)
        public int Turn { get; set; } // fTurn in C
        public bool Doubled { get; set; }
        public int Resigned { get; set; } // 0=none, 1=single, 2=gammon, 3=backgammon
        public int[] Dice { get; set; } = new int[2];
        public int MatchLength { get; set; } // 0 means Money Game
        public int Player0Score { get; set; }
        public int Player1Score { get; set; }
        public bool JacobyRule { get; set; }
        public bool BeaversAllowed { get; set; }
    }
}