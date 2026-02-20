using System;
using System.Threading;
using EngineCore;

namespace EngineCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("        BACKGAMMON GRANDMASTER AI ARENA           ");
            Console.WriteLine("==================================================");

            // 1. Initialize the Brains
            Console.WriteLine("Loading Engine Components...");

            // NOTE: We are passing a dummy NeuralNet for now until you parse gnubg.weights
            NeuralNet dummyNet = new NeuralNet(inputs: 250, hidden: 128, outputs: 5, trainedIters: 0, betaH: 1f, betaO: 1f);

            string dataDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\Data"));
            BearoffEvaluator bearoffEval = new BearoffEvaluator(dataDir); // Will load the TS database perfectly!
            CubeEvaluator cubeEval = new CubeEvaluator();

            SearchEngine ai = new SearchEngine(dummyNet, null, bearoffEval, cubeEval);
            Console.WriteLine("Engine Ready!\n");

            // 2. Setup the Match and Board
            MatchState match = new MatchState
            {
                MatchLength = 0, // Money game
                JacobyRule = true,
                BeaversAllowed = true
            };

            GameState state = CreateStartingPosition();
            Random rng = new Random();
            int turnCount = 1;
            bool gameIsOver = false;

            // 3. The Game Loop
            while (!gameIsOver)
            {
                // Roll Dice
                state.Dice1 = rng.Next(1, 7);
                state.Dice2 = rng.Next(1, 7);

                // Draw the board!
                BoardVisualizer.PrintBoard(state, match);

                // Ask the AI for the best move (Using 1-ply. Change to 2-ply once weights are loaded!)
                Turn? bestTurn = ai.GetBestTurn(state, match, depth: 1);

                if (bestTurn == null || bestTurn.Moves.Count == 0)
                {
                    Console.WriteLine(">> AI Dances! (No legal moves)\n");
                }
                else
                {
                    Console.WriteLine($">> AI Plays: {bestTurn}\n");
                    state = bestTurn.ResultingState;
                }

                // Check for Game Over 
                if (HasWon(state.Player1Checkers) || HasWon(state.Player2Checkers))
                {
                    Console.WriteLine("\n==================================================");
                    Console.WriteLine($" GAME OVER! Player {(HasWon(state.Player1Checkers) ? 1 : 0)} Wins!");
                    Console.WriteLine("==================================================");
                    BoardVisualizer.PrintBoard(state, match);
                    gameIsOver = true;
                    break;
                }

                // Swap turns
                state.PlayerOnRoll = 1 - state.PlayerOnRoll;

                // When we swap turns, we must physically swap the arrays so the engine 
                // always evaluates from the perspective of the player "On Roll"
                int[] temp = state.Player1Checkers;
                state.Player1Checkers = state.Player2Checkers;
                state.Player2Checkers = temp;

                ai.ClearCache();
                turnCount++;

                // Pause for 1.5 seconds so you can watch the game unfold like a movie
                Thread.Sleep(1500);
            }
        }

        static GameState CreateStartingPosition()
        {
            var state = new GameState();

            // Player 1 (Opponent)
            state.Player1Checkers[5] = 5;
            state.Player1Checkers[7] = 3;
            state.Player1Checkers[12] = 5;
            state.Player1Checkers[23] = 2;

            // Player 0 (AI)
            state.Player2Checkers[5] = 5;
            state.Player2Checkers[7] = 3;
            state.Player2Checkers[12] = 5;
            state.Player2Checkers[23] = 2;

            state.PlayerOnRoll = 0;
            return state;
        }

        static bool HasWon(int[] checkers)
        {
            int totalCheckers = 0;
            for (int i = 0; i < 25; i++) totalCheckers += checkers[i];
            return totalCheckers == 0;
        }
    }
}