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
            
            string dataDir = FindDataDirectory();
            string weightsPath = System.IO.Path.Combine(dataDir, "gnubg.weights");
            
            Console.WriteLine("Parsing Neural Net weights (this takes a second)...");
            var networks = WeightParser.Load(weightsPath);
            
            // FIX: Securely find the 250-input Contact Network
            NeuralNet contactNet = System.Linq.Enumerable.First(networks, n => n.InputCount == 250);
            
            BearoffEvaluator bearoffEval = new BearoffEvaluator(dataDir); 
            CubeEvaluator cubeEval = new CubeEvaluator();
            
            // We pass 'null' for the race net for now, so SearchEngine safely falls back 
            // to using the 250-input ContactNet for all non-bearoff evaluations.
            SearchEngine ai = new SearchEngine(contactNet, null, bearoffEval, cubeEval);
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
        
        private static string FindDataDirectory()
        {
            // Start at the directory where the test DLL is running
            var currentDir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);

            // Search upward until we find a directory containing the "Data" folder
            while (currentDir != null)
            {
                string potentialDataDir = System.IO.Path.Combine(currentDir.FullName, "Data");
                // Check if the directory exists AND contains our specific file
                if (System.IO.Directory.Exists(potentialDataDir) &&
                    System.IO.File.Exists(System.IO.Path.Combine(potentialDataDir, "gnubg_ts0.bd")))
                {
                    return potentialDataDir;
                }
                currentDir = currentDir.Parent; // Move up one folder
            }

            throw new System.IO.DirectoryNotFoundException("Could not find the 'Data' directory containing gnubg_ts0.bd.");
        }
    }
}