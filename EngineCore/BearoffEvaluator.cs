using System;
using System.IO;

namespace EngineCore
{
    public class BearoffEvaluator
    {
        private readonly byte[]? _tsDatabase;
        private readonly bool _isTsLoaded;

        // Exact size of the standard GNUBG 6-checker TS database
        private const int ExpectedTsSize = 6830248;
        private const int TsHeaderSize = 40;
        private const int TsRecordSize = 8;
        private const int TsMaxId = 923; // 6 checkers on 6 points = 924 IDs (0-923)

        public BearoffEvaluator(string dataDirectory)
        {
            string tsDbPath = Path.Combine(dataDirectory, "gnubg_ts0.bd");
            string osDbPath = Path.Combine(dataDirectory, "gnubg_os0.bd");

            // 1. Load Two-Sided Database
            if (File.Exists(tsDbPath))
            {
                _tsDatabase = File.ReadAllBytes(tsDbPath);

                if (_tsDatabase.Length == ExpectedTsSize)
                {
                    _isTsLoaded = true;
                    Console.WriteLine("Successfully loaded Two-Sided Bearoff Database (gnubg_ts0.bd).");
                }
                else
                {
                    Console.WriteLine($"Warning: gnubg_ts0.bd size mismatch. Expected {ExpectedTsSize}, got {_tsDatabase.Length}.");
                }
            }
            else
            {
                Console.WriteLine("Warning: gnubg_ts0.bd not found. Deep endgames will fallback to Neural Net.");
            }

            // 2. We'll verify OS database exists for future implementation
            if (File.Exists(osDbPath))
            {
                Console.WriteLine("Found One-Sided Bearoff Database (gnubg_os0.bd) for future use.");
                // We will load OS bytes here when we implement the convolution math!
            }
        }

        public float[]? Evaluate(uint idOnRoll, uint idWaiting, PositionClass pc)
        {
            // Handle Two-Sided Bearoffs (Both players have <= 6 checkers)
            if (pc == PositionClass.BearoffTwoSided && _isTsLoaded && _tsDatabase != null)
            {
                // Safety check to ensure we don't read out of bounds
                if (idOnRoll <= TsMaxId && idWaiting <= TsMaxId)
                {
                    // Calculate the exact byte offset for this specific matchup
                    // It's a 2D grid: (Row * TotalColumns) + Column
                    int matchupIndex = (int)((idOnRoll * (TsMaxId + 1)) + idWaiting);

                    int byteOffset = TsHeaderSize + (matchupIndex * TsRecordSize);

                    // GNUBG stores the base cubeless equity in the first 2 bytes of the record
                    ushort rawEquity = BitConverter.ToUInt16(_tsDatabase, byteOffset);

                    // Convert GNUBG's short format to a standard float (-1.0 to 1.0)
                    float cubelessEquity = rawEquity / 32768.0f;

                    // Convert Equity back into a raw Win Probability (0.0 to 1.0)
                    // Since gammons are mathematically impossible with <= 6 checkers:
                    // Equity = (Win * 2) - 1  --->  Win = (Equity + 1) / 2
                    float winProb = (cubelessEquity + 1.0f) / 2.0f;

                    // Clamp to strictly [0.0, 1.0] just in case of rounding artifacts
                    winProb = Math.Clamp(winProb, 0.0f, 1.0f);

                    return new float[]
                    {
                        winProb,
                        0.0f, // Win Gammon
                        0.0f, // Win Backgammon
                        0.0f, // Lose Gammon
                        0.0f  // Lose Backgammon
                    };
                }
            }

            // If it's a One-Sided bearoff (one player has 7+ checkers), or DB is missing,
            // returning null tells the Search Engine to safely fall back to the Neural Net.
            return null;
        }

        /// <summary>
        /// Calculates the exact win probability by cross-multiplying two One-Sided roll distributions.
        /// </summary>
        /// <param name="onRollDist">The probability distribution of rolls required for the player to bear off.</param>
        /// <param name="waitingDist">The probability distribution of rolls required for the opponent to bear off.</param>
        public float CalculateOneSidedWinProb(float[] onRollDist, float[] waitingDist)
        {
            float totalWinProb = 0.0f;
            int maxRolls = Math.Max(onRollDist.Length, waitingDist.Length);

            // i represents the number of rolls it takes the "On Roll" player to finish
            for (int i = 0; i < onRollDist.Length; i++)
            {
                if (onRollDist[i] <= 0.0f) continue;

                float opponentSlowerProb = 0.0f;

                // j represents the number of rolls it takes the "Waiting" player to finish.
                // Because Player 1 rolled first, if Player 2 takes the same number of rolls (j = i), Player 1 wins.
                for (int j = i; j < waitingDist.Length; j++)
                {
                    opponentSlowerProb += waitingDist[j];
                }

                // If Player 2's distribution array is shorter than Player 1's required rolls,
                // the opponent finishes before Player 1, meaning opponentSlowerProb remains 0.0f for this 'i'.

                totalWinProb += onRollDist[i] * opponentSlowerProb;
            }

            return Math.Clamp(totalWinProb, 0.0f, 1.0f);
        }
    }
}