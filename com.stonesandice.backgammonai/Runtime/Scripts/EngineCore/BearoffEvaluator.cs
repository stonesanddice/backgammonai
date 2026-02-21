using System;
using System.IO;

namespace EngineCore
{
    public class BearoffEvaluator
    {
        private readonly byte[]? _tsDatabase;
        private readonly bool _isTsLoaded;

        private readonly byte[]? _osDatabase;
        private readonly bool _isOsLoaded;

        // Exact size of the standard GNUBG 6-checker TS database
        private const int ExpectedTsSize = 6830248;
        private const int TsHeaderSize = 40;
        private const int TsRecordSize = 8;
        private const int TsMaxId = 923; // 6 checkers on 6 points = 924 IDs (0-923)

        // One-sided: 15 checkers on 6 points = C(20,5) = 15504 positions (matches BoardClassifier.GetPositionBearoff)
        private const int OsMaxId = 15503;
        private const int OsNumPositions = 15504;
        private int _osHeaderSize = 0;
        private int _osRecordSize = 0;

        /// <summary>True if the two-sided bearoff database (gnubg_ts0.bd) was found and loaded successfully.</summary>
        public bool IsTwoSidedLoaded => _isTsLoaded;

        /// <summary>True if the one-sided bearoff database (gnubg_os0.bd) was found and loaded successfully.</summary>
        public bool IsOneSidedLoaded => _isOsLoaded;

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

            // 2. Load One-Sided Database when present (6 points, 15 checkers = 15504 positions)
            if (File.Exists(osDbPath))
            {
                try
                {
                    byte[] osBytes = File.ReadAllBytes(osDbPath);
                    if (osBytes.Length > 40)
                    {
                        int dataSize40 = osBytes.Length - 40;
                        int dataSize0 = osBytes.Length;
                        if (dataSize40 % OsNumPositions == 0 && dataSize40 / OsNumPositions >= 2)
                        {
                            _osHeaderSize = 40;
                            _osRecordSize = dataSize40 / OsNumPositions;
                            _osDatabase = osBytes;
                            _isOsLoaded = true;
                            Console.WriteLine("Successfully loaded One-Sided Bearoff Database (gnubg_os0.bd).");
                        }
                        else if (dataSize0 % OsNumPositions == 0 && dataSize0 / OsNumPositions >= 2)
                        {
                            _osHeaderSize = 0;
                            _osRecordSize = dataSize0 / OsNumPositions;
                            _osDatabase = osBytes;
                            _isOsLoaded = true;
                            Console.WriteLine("Successfully loaded One-Sided Bearoff Database (gnubg_os0.bd, no header).");
                        }
                        else
                        {
                            // Some builds use a larger header; infer (fileSize - header) % 15504 == 0
                            for (int recordSize = 2; recordSize <= 256; recordSize += 2)
                            {
                                int dataSize = osBytes.Length - OsNumPositions * recordSize;
                                if (dataSize >= 0 && dataSize <= 16384)
                                {
                                    _osHeaderSize = dataSize;
                                    _osRecordSize = recordSize;
                                    _osDatabase = osBytes;
                                    _isOsLoaded = true;
                                    Console.WriteLine("Successfully loaded One-Sided Bearoff Database (gnubg_os0.bd).");
                                    break;
                                }
                            }
                            if (!_isOsLoaded)
                                Console.WriteLine($"Warning: gnubg_os0.bd size {osBytes.Length} not compatible with 15504 positions.");
                        }
                    }
                    else if (osBytes.Length > 0)
                    {
                        Console.WriteLine("Warning: gnubg_os0.bd too small (header only or truncated).");
                    }
                    else
                    {
                        Console.WriteLine("Warning: gnubg_os0.bd is empty.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: failed to load gnubg_os0.bd: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("One-Sided Bearoff Database (gnubg_os0.bd) not found; one-sided bearoff will use neural net.");
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

            // One-Sided bearoff: one or both players have 7+ checkers in the home board
            if (pc == PositionClass.BearoffOneSided && _isOsLoaded && _osDatabase != null)
            {
                if (idOnRoll <= OsMaxId && idWaiting <= OsMaxId)
                {
                    float[]? distOnRoll = GetOneSidedDistribution(idOnRoll);
                    float[]? distWaiting = GetOneSidedDistribution(idWaiting);
                    if (distOnRoll != null && distWaiting != null)
                    {
                        float winProb = CalculateOneSidedWinProb(distOnRoll, distWaiting);
                        winProb = Math.Clamp(winProb, 0.0f, 1.0f);
                        return new float[]
                        {
                            winProb,
                            0.0f, // Lose Gammon
                            0.0f, // Lose Backgammon
                            0.0f, // Win Gammon
                            0.0f  // Win Backgammon
                        };
                    }
                }
            }

            // DB missing or position out of range: fall back to Neural Net
            return null;
        }

        /// <summary>
        /// Reads the "number of rolls to bear off" distribution for a one-sided position from the OS database.
        /// Each record is stored as a sequence of 16-bit values (probability * 65535); index 0 = prob of finishing in 1 roll, etc.
        /// </summary>
        private float[]? GetOneSidedDistribution(uint positionId)
        {
            if (_osDatabase == null || positionId >= OsNumPositions) return null;
            int offset = _osHeaderSize + (int)positionId * _osRecordSize;
            int numSteps = _osRecordSize / 2; // ushorts
            if (offset + _osRecordSize > _osDatabase.Length) return null;

            // GNUBG one-sided record: may have leading fields (e.g. 1 ushort = nRolls or checksum); then distribution of prob per "number of rolls"
            int distStart = offset;
            int distLength = numSteps;
            // If record has an extra leading ushort (e.g. "max rolls" or reserved), skip it
            if (_osRecordSize >= 2 && (numSteps * 2) < _osRecordSize)
            {
                distStart = offset + 2;
                distLength = (_osRecordSize - 2) / 2;
            }

            float[] dist = new float[distLength];
            float sum = 0f;
            for (int i = 0; i < distLength; i++)
            {
                ushort raw = BitConverter.ToUInt16(_osDatabase, distStart + i * 2);
                dist[i] = raw / 65535.0f;
                sum += dist[i];
            }
            if (sum <= 0f) return null;
            for (int i = 0; i < distLength; i++)
                dist[i] /= sum;
            return dist;
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