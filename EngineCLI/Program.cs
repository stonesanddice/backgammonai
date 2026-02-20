using System;
using EngineCore;

namespace EngineCLI
{
    class Program
    {
        static void Main(string[] args)
        {
            CubeEvaluator cubeEval = new CubeEvaluator();

            // We'll use the same "Borderline Advantage" probabilities
            // Win: 72%, WinG: 10%, WinBG: 1% | Lose: 28%, LoseG: 10%, LoseBG: 1%
            Probabilities probs = new Probabilities(0.72f, 0.10f, 0.01f, 0.10f, 0.01f);

            // Three wildly different match scenarios
            var testCases = new[]
            {
                new {
                    Name = "Early Match (4-away / 4-away)",
                    PlayerAway = 4, OppAway = 4, Crawford = false
                },
                new {
                    Name = "2-away / 2-away (The 2-away trick)",
                    PlayerAway = 2, OppAway = 2, Crawford = false
                },
                new {
                    Name = "Crawford Game (1-away / 3-away)",
                    PlayerAway = 1, OppAway = 3, Crawford = true
                }
            };

            foreach (var test in testCases)
            {
                Console.WriteLine($"\n==================================================");
                Console.WriteLine($" CASE: {test.Name}");
                Console.WriteLine($"==================================================");

                int currentCube = 1;
                int centeredCubeOwner = -1; // -1 = centered
                int opponentCubeOwner = 1;  // 1 = opponent owns it
                bool canWinGammon = probs.WinGammon > 0.0f;

                // Check if the rules even allow a double
                bool isLive = cubeEval.IsCubeLiveInMatch(test.PlayerAway, test.OppAway, currentCube, test.Crawford);

                // 1. MWC if we DO NOT double (Cube stays at 1, Centered)
                // We use the new Cubeful MWC calculation!
                float mwcNoDouble = cubeEval.CalculateCubefulMwc(probs, test.PlayerAway, test.OppAway, currentCube, centeredCubeOwner);

                // 2. MWC if we DOUBLE and opponent TAKES (Cube becomes 2, Opponent owns it)
                float mwcDoubleTake = cubeEval.CalculateCubefulMwc(probs, test.PlayerAway, test.OppAway, currentCube * 2, opponentCubeOwner);

                // 3. MWC if we DOUBLE and opponent PASSES 
                // (We win 1 point, so our away score drops by 1)
                float mwcDoublePass = MatchEquityTable.GetMatchWinningChance(test.PlayerAway - 1, test.OppAway);

                // 4. Determine Match Action based purely on MWC
                CubeAction action = cubeEval.GetMatchCubeAction(mwcNoDouble, mwcDoubleTake, mwcDoublePass, canWinGammon, isLive);

                Console.WriteLine($" Match Score  : You {test.PlayerAway}-away, Opp {test.OppAway}-away");
                Console.WriteLine($" Crawford Rule: {test.Crawford}");
                Console.WriteLine($" Cube is Live : {isLive}");
                Console.WriteLine($"\n Match Winning Chances (MWC):");
                Console.WriteLine($"   No Double    : {mwcNoDouble:P2}");
                Console.WriteLine($"   Double / Take: {mwcDoubleTake:P2}");
                Console.WriteLine($"   Double / Pass: {mwcDoublePass:P2}");

                Console.WriteLine($"\n -> RECOMMENDED ACTION: {action}");
                Console.WriteLine("==================================================\n");
            }
        }
    }
}