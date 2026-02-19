using System;
using System.Collections.Generic;
using EngineCore;

namespace EngineCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1. Setup Starting Board
            string startingPositionId = "4HPwATDgc/ABMA";
            string moneyGameMatchId = "MAAAAAAAAAAE";

            GameState state = PositionId.Decode(startingPositionId);
            MatchState match = MatchId.Decode(moneyGameMatchId);

            for (int i = 0; i <= 24; i++)
            {
                state.Player1Checkers[i] = state.Board[0, i];
                state.Player2Checkers[i] = state.Board[1, i];
            }

            BoardVisualizer.PrintBoard(state, match);

            // 2. Load Neural Networks
            Console.WriteLine("Loading GNUBG Neural Networks...");
            List<NeuralNet> allNets = WeightParser.Load("data/gnubg-nn/gnubg.weights");
            NeuralNet contactNet = allNets[4]; // 250 inputs

            // 3. Test a Specific Roll (3-1)
            state.Dice1 = 3;
            state.Dice2 = 1;
            
            Console.WriteLine($"\n===========================================");
            Console.WriteLine($" Searching for best move: Roll {state.Dice1}-{state.Dice2}");
            Console.WriteLine($"===========================================");

            SearchEngine searchEngine = new SearchEngine(contactNet);
            Turn? bestTurn = searchEngine.GetBestTurn(state);

            if (bestTurn != null && bestTurn.ResultingState != null)
            {
                Console.WriteLine($"\nEngine chooses: {bestTurn}");
                
                // Evaluate the resulting state from Player 1's perspective
                float[] finalInputs = FeatureEncoder.EncodeContact(
                    bestTurn.ResultingState.Player1Checkers, 
                    bestTurn.ResultingState.Player2Checkers);
                
                float[] probabilities = contactNet.Evaluate(finalInputs);

                Console.WriteLine("\n===========================================");
                Console.WriteLine(" Resulting Position Evaluation             ");
                Console.WriteLine("===========================================");
                Console.WriteLine($" Win Game      : {probabilities[0]:P2}");
                Console.WriteLine($" Win Gammon    : {probabilities[1]:P2}");
                Console.WriteLine($" Win Backgammon: {probabilities[2]:P2}");
                Console.WriteLine($" Lose Gammon   : {probabilities[3]:P2}");
                Console.WriteLine($" Lose Backgammon: {probabilities[4]:P2}");
                Console.WriteLine("===========================================\n");
                
                // Print the resulting board using your GameStateExtensions
                Console.WriteLine("Resulting Board:\n");
                bestTurn.ResultingState.PrintBoard();
            }
            else
            {
                Console.WriteLine("No legal moves found.");
            }
        }
    }
}