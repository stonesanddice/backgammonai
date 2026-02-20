using System;

namespace EngineCore
{
    public record Probabilities(
        float Win,
        float WinGammon,
        float WinBackgammon,
        float LoseGammon,
        float LoseBackgammon)
    {
        public float Lose => 1.0f - Win;
    }

    public enum CubeAction
    {
        NoDouble,
        DoubleTake,
        DoublePass,
        TooGoodToDouble,
        TooGoodToDoublePass
    }

    public enum CubeResponse
    {
        Pass,
        Take,
        Beaver,
        Raccoon
    }

    public class CubeEvaluator
    {
        // Equivalent to rContactX in eval.c - Cube efficiency for standard contact positions.
        private const float DefaultCubeEfficiency = 0.68f;

        // Epsilon to avoid divide-by-zero errors (used in GNUBG's Cl2CfMoney)
        private const float Epsilon = 0.0000001f;
        private const float OmEpsilon = 0.9999999f;

        /// <summary>
        /// Ported from GNUBG's `Utility` / `UtilityME`.
        /// </summary>
        public float CalculateCubelessEquity(Probabilities p)
        {
            // Note: Gammon values are 1.0 for money play.
            // Formula: Win*2 - 1 + (WinG - LoseG) + (WinBG - LoseBG)
            return (p.Win * 2.0f - 1.0f) +
                   (p.WinGammon - p.LoseGammon) +
                   (p.WinBackgammon - p.LoseBackgammon);
        }

        /// <summary>
        /// Ported from GNUBG's `Utility` / `UtilityME`.
        /// </summary>
        public float CalculateCubelessEquity(Probabilities p, MatchState match)
        {
            // Under the Jacoby rule, gammons don't count if the cube is centered.
            float gammonPrice = (match.JacobyRule && match.CubeOwner == -1) ? 0.0f : 1.0f;

            return (p.Win * 2.0f - 1.0f) +
                   ((p.WinGammon - p.LoseGammon) * gammonPrice) +
                   ((p.WinBackgammon - p.LoseBackgammon) * gammonPrice);
        }

        /// <summary>
        /// Ported from GNUBG's `MoneyLive`.
        /// </summary>
        private float GetMoneyLive(float rW, float rL, float winProb, MatchState match)
        {
            if (match.CubeOwner == -1) // Centered cube
            {
                float rTP = (rL - 0.5f) / (rW + rL + 0.5f);
                float rCP = (rL + 1.0f) / (rW + rL + 0.5f);

                if (winProb < rTP)
                    return match.JacobyRule ? -1.0f : (-rL + (-1.0f + rL) * winProb / rTP);
                if (winProb < rCP)
                    return -1.0f + 2.0f * (winProb - rTP) / (rCP - rTP);

                return match.JacobyRule ? 1.0f : (+1.0f + (rW - 1.0f) * (winProb - rCP) / (1.0f - rCP));
            }
            else if (match.CubeOwner == match.PlayerOnRoll) // Owned cube
            {
                float rCP = (rL + 1.0f) / (rW + rL + 0.5f);

                if (winProb < rCP)
                    return -rL + (1.0f + rL) * winProb / rCP;

                return +1.0f + (rW - 1.0f) * (winProb - rCP) / (1.0f - rCP);
            }
            else // Unavailable cube (Opponent owns it)
            {
                float rTP = (rL - 0.5f) / (rW + rL + 0.5f);

                if (winProb < rTP)
                    return -rL + (-1.0f + rL) * winProb / rTP;

                return -1.0f + (rW + 1.0f) * (winProb - rTP) / (1.0f - rTP);
            }
        }

        /// <summary>
        /// Ported from GNUBG's `Cl2CfMoney`.
        /// </summary>
        public float CalculateCubefulEquity(Probabilities p, MatchState match, float cubeEfficiency = DefaultCubeEfficiency)
        {
            float rW, rL;

            if (p.Win > Epsilon)
                rW = 1.0f + (p.WinGammon + p.WinBackgammon) / p.Win;
            else
                return CalculateCubelessEquity(p, match);

            if (p.Win < OmEpsilon)
                rL = 1.0f + (p.LoseGammon + p.LoseBackgammon) / (1.0f - p.Win);
            else
                return CalculateCubelessEquity(p, match);

            float rEqDead = CalculateCubelessEquity(p, match);
            float rEqLive = GetMoneyLive(rW, rL, p.Win, match);

            return rEqDead * (1.0f - cubeEfficiency) + rEqLive * cubeEfficiency;
        }

        /// <summary>
        /// Ported from GNUBG's `FindBestCubeDecision`.
        /// </summary>
        public CubeAction GetMoneyCubeAction(float eqNoDouble, float eqDoubleTake, float eqDoublePass)
        {
            // 1. Is Double/Take and Double/Pass better than No Double?
            if (eqDoubleTake >= eqNoDouble && eqDoublePass >= eqNoDouble)
            {
                // We have a valid double. Now, does the opponent take or drop?
                if (eqDoublePass > eqDoubleTake)
                    return CubeAction.DoubleTake;
                else
                    return CubeAction.DoublePass;
            }
            else // No double is better than doubling
            {
                if (eqNoDouble > eqDoubleTake)
                {
                    if (eqDoubleTake > eqDoublePass)
                        return CubeAction.TooGoodToDoublePass; // ND > DT > DP
                    if (eqNoDouble > eqDoublePass)
                        return CubeAction.TooGoodToDouble;     // ND > DP > DT

                    return CubeAction.NoDouble;                // DP > ND > DT
                }
                else
                {
                    return CubeAction.DoublePass;              // DT >= ND > DP
                }
            }
        }

        /// <summary>
        /// Ported from GNUBG's `FindBestCubeDecision`.
        /// </summary>
        public CubeAction GetMoneyCubeAction(float eqNoDouble, float eqDoubleTake, float eqDoublePass, bool canWinGammon)
        {
            // 1. Is Double/Take and Double/Pass better than No Double?
            if (eqDoubleTake >= eqNoDouble && eqDoublePass >= eqNoDouble)
            {
                // We have a valid double. Does the opponent take or drop?
                if (eqDoublePass > eqDoubleTake)
                    return CubeAction.DoubleTake;  // DP > DT >= ND
                else
                    return CubeAction.DoublePass;  // DT >= DP >= ND
            }
            else // No double is better than doubling
            {
                if (eqNoDouble > eqDoubleTake)
                {
                    if (eqDoubleTake > eqDoublePass)
                    {
                        // ND > DT > DP
                        return canWinGammon ? CubeAction.TooGoodToDoublePass : CubeAction.DoublePass;
                    }
                    else if (eqNoDouble > eqDoublePass)
                    {
                        // ND > DP > DT
                        return canWinGammon ? CubeAction.TooGoodToDouble : CubeAction.NoDouble;
                    }
                    else
                    {
                        // DP > ND > DT
                        return CubeAction.NoDouble;
                    }
                }
                else
                {
                    // 3. DT >= ND > DP
                    return canWinGammon ? CubeAction.TooGoodToDoublePass : CubeAction.DoublePass;
                }
            }
        }

        /// <summary>
        /// Converts the 5 cumulative network probabilities into Cubeless Match Winning Chance (MWC).
        /// Replaces GNUBG's Utility() for Match Play.
        /// </summary>
        public float CalculateCubelessMwc(Probabilities p, int playerAway, int oppAway, int cubeValue)
        {
            // 1. Break cumulative probabilities into mutually exclusive buckets
            float pWinNormal = p.Win - p.WinGammon;
            float pWinGammonOnly = p.WinGammon - p.WinBackgammon;
            float pWinBackgammon = p.WinBackgammon;

            float pLoseNormal = p.Lose - p.LoseGammon;
            float pLoseGammonOnly = p.LoseGammon - p.LoseBackgammon;
            float pLoseBackgammon = p.LoseBackgammon;

            // 2. Fetch the MWC from the MET for each possible outcome
            float mwcW = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 1);
            float mwcWg = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 2);
            float mwcWbg = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 3);

            float mwcL = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 1);
            float mwcLg = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 2);
            float mwcLbg = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 3);

            // 3. Multiply and sum to get the expected MWC (Ranges from 0.0f to 1.0f)
            return (pWinNormal * mwcW) + (pWinGammonOnly * mwcWg) + (pWinBackgammon * mwcWbg) +
                   (pLoseNormal * mwcL) + (pLoseGammonOnly * mwcLg) + (pLoseBackgammon * mwcLbg);
        }

        /// <summary>
        /// Ported from GNUBG's fDoCubeful check.
        /// Checks if the cube is actually "live" and available to be turned.
        /// </summary>
        public bool IsCubeLiveInMatch(int playerAway, int oppAway, int cubeValue, bool isCrawfordGame)
        {
            if (isCrawfordGame)
                return false; // Doubling is illegal in Crawford

            if (playerAway <= cubeValue && oppAway <= cubeValue)
                return false; // Cube is dead (winning the current game wins the match for either player)

            return true;
        }

        /// <summary>
        /// Ported from GNUBG's FindBestCubeDecision for Match Play.
        /// The logic tree is identical to Money play, but operates strictly on MWC floats (0.0 to 1.0).
        /// </summary>
        public CubeAction GetMatchCubeAction(float mwcNoDouble, float mwcDoubleTake, float mwcDoublePass,
                                             bool canWinGammon, bool isCubeLive)
        {
            // If the rules or score dictate the cube cannot be turned, return NoDouble.
            if (!isCubeLive)
                return CubeAction.NoDouble;

            // 1. Is Double/Take and Double/Pass better than No Double?
            if (mwcDoubleTake >= mwcNoDouble && mwcDoublePass >= mwcNoDouble)
            {
                if (mwcDoublePass > mwcDoubleTake)
                    return CubeAction.DoubleTake;  // DP > DT >= ND
                else
                    return CubeAction.DoublePass;  // DT >= DP >= ND
            }
            else // No double is better than doubling
            {
                if (mwcNoDouble > mwcDoubleTake)
                {
                    if (mwcDoubleTake > mwcDoublePass)
                        return canWinGammon ? CubeAction.TooGoodToDoublePass : CubeAction.DoublePass;
                    else if (mwcNoDouble > mwcDoublePass)
                        return canWinGammon ? CubeAction.TooGoodToDouble : CubeAction.NoDouble;
                    else
                        return CubeAction.NoDouble;
                }
                else
                {
                    return canWinGammon ? CubeAction.TooGoodToDoublePass : CubeAction.DoublePass;
                }
            }
        }

        /// <summary>
        /// Ported from GNUBG's `Cl2CfMatch`.
        /// Determines which cubeful match formula to use based on cube ownership.
        /// </summary>
        public float CalculateCubefulMwc(Probabilities p, int playerAway, int oppAway, int cubeValue, int cubeOwner, float cubeEfficiency = DefaultCubeEfficiency)
        {
            // If the cube is dead (score <= cubeValue), future cube value is 0. 
            // Return raw cubeless MWC.
            if (playerAway <= cubeValue && oppAway <= cubeValue)
                return CalculateCubelessMwc(p, playerAway, oppAway, cubeValue);

            if (cubeOwner == -1) // Centered
                return Cl2CfMatchCentered(p, playerAway, oppAway, cubeValue, cubeEfficiency);
            else if (cubeOwner == 0) // We own it (Assuming Player 0 is the engine)
                return Cl2CfMatchOwned(p, playerAway, oppAway, cubeValue, cubeEfficiency);
            else // Opponent owns it (Unavailable)
                return Cl2CfMatchUnavailable(p, playerAway, oppAway, cubeValue, cubeEfficiency);
        }

        /// <summary>
        /// Ported from GNUBG's `Cl2CfMatchCentered`.
        /// </summary>
        private float Cl2CfMatchCentered(Probabilities p, int playerAway, int oppAway, int cubeValue, float rCubeX)
        {
            float rMWCDead = CalculateCubelessMwc(p, playerAway, oppAway, cubeValue);
            if (p.Win <= Epsilon || p.Win >= OmEpsilon) return rMWCDead;

            // Calculate conditional gammon rates
            float rG0 = (p.WinGammon - p.WinBackgammon) / p.Win;
            float rBG0 = p.WinBackgammon / p.Win;
            float rG1 = (p.LoseGammon - p.LoseBackgammon) / (1.0f - p.Win);
            float rBG1 = p.LoseBackgammon / (1.0f - p.Win);

            // Fetch base MWC values at the *current* cube
            float mwcWin = (1.0f - rG0 - rBG0) * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 1) +
                           rG0 * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 2) +
                           rBG0 * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 3);

            float mwcLose = (1.0f - rG1 - rBG1) * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 1) +
                            rG1 * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 2) +
                            rBG1 * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 3);

            // Fetch Cash Points (Opponent drops)
            float rMWCCash = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 1);
            float rMWCOppCash = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 1);

            // To get Cash Point (rTG) and Opponent Cash Point (rOppTG), we need the Take Points at the *doubled* cube.
            float mwcTakeWin = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue * 2, 1);
            float mwcTakeLose = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue * 2, 1);

            float rOppTP = (mwcTakeWin - rMWCCash) / (mwcTakeWin - mwcTakeLose);
            float rTG = 1.0f - rOppTP; // Our Cash Point

            float oppMwcTakeWin = 1.0f - mwcTakeLose;
            float oppMwcTakeLose = 1.0f - mwcTakeWin;
            float oppMwcCash = 1.0f - rMWCOppCash;
            float rTP = (oppMwcTakeWin - oppMwcCash) / (oppMwcTakeWin - oppMwcTakeLose);
            float rOppTG = 1.0f - rTP; // Opponent's Cash Point

            float rMWCLive;

            // Piecewise linear interpolation based on Win Probability
            if (p.Win <= rOppTG) // Opponent is too good to double
            {
                // FIX: Changed rMWCLose to mwcLose
                rMWCLive = mwcLose + (rMWCOppCash - mwcLose) * p.Win / rOppTG;
            }
            else if (p.Win < rTG) // Doubling window
            {
                rMWCLive = rMWCOppCash + (rMWCCash - rMWCOppCash) * (p.Win - rOppTG) / (rTG - rOppTG);
            }
            else // We are too good to double
            {
                rMWCLive = rMWCCash + (mwcWin - rMWCCash) * (p.Win - rTG) / (1.0f - rTG);
            }

            return rMWCDead * (1.0f - rCubeX) + rMWCLive * rCubeX;
        }

        /// <summary>
        /// Ported from GNUBG's `Cl2CfMatchOwned`.
        /// </summary>
        private float Cl2CfMatchOwned(Probabilities p, int playerAway, int oppAway, int cubeValue, float rCubeX)
        {
            float rMWCDead = CalculateCubelessMwc(p, playerAway, oppAway, cubeValue);
            if (p.Win <= Epsilon || p.Win >= OmEpsilon) return rMWCDead;

            float rG0 = (p.WinGammon - p.WinBackgammon) / p.Win;
            float rBG0 = p.WinBackgammon / p.Win;
            float rG1 = (p.LoseGammon - p.LoseBackgammon) / (1.0f - p.Win);
            float rBG1 = p.LoseBackgammon / (1.0f - p.Win);

            float mwcWin = (1.0f - rG0 - rBG0) * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 1) +
                           rG0 * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 2) +
                           rBG0 * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 3);

            float mwcLose = (1.0f - rG1 - rBG1) * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 1) +
                            rG1 * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 2) +
                            rBG1 * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 3);

            float rMWCCash = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 1);

            float mwcTakeWin = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue * 2, 1);
            float mwcTakeLose = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue * 2, 1);
            float rOppTP = (mwcTakeWin - rMWCCash) / (mwcTakeWin - mwcTakeLose);
            float rTG = 1.0f - rOppTP;

            float rMWCLive;
            if (p.Win <= rTG)
            {
                rMWCLive = mwcLose + (rMWCCash - mwcLose) * p.Win / rTG;
            }
            else // Too good to double
            {
                rMWCLive = rMWCCash + (mwcWin - rMWCCash) * (p.Win - rTG) / (1.0f - rTG);
            }

            return rMWCDead * (1.0f - rCubeX) + rMWCLive * rCubeX;
        }

        /// <summary>
        /// Ported from GNUBG's `Cl2CfMatchUnavailable`.
        /// </summary>
        private float Cl2CfMatchUnavailable(Probabilities p, int playerAway, int oppAway, int cubeValue, float rCubeX)
        {
            float rMWCDead = CalculateCubelessMwc(p, playerAway, oppAway, cubeValue);
            if (p.Win <= Epsilon || p.Win >= OmEpsilon) return rMWCDead;

            float rG0 = (p.WinGammon - p.WinBackgammon) / p.Win;
            float rBG0 = p.WinBackgammon / p.Win;
            float rG1 = (p.LoseGammon - p.LoseBackgammon) / (1.0f - p.Win);
            float rBG1 = p.LoseBackgammon / (1.0f - p.Win);

            float mwcWin = (1.0f - rG0 - rBG0) * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 1) +
                           rG0 * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 2) +
                           rBG0 * MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, 3);

            float mwcLose = (1.0f - rG1 - rBG1) * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 1) +
                            rG1 * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 2) +
                            rBG1 * MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 3);

            float rMWCOppCash = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 1);

            float oppMwcTakeWin = 1.0f - MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue * 2, 1);
            float oppMwcTakeLose = 1.0f - MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue * 2, 1);
            float oppMwcCash = 1.0f - rMWCOppCash;
            float rTP = (oppMwcTakeWin - oppMwcCash) / (oppMwcTakeWin - oppMwcTakeLose);
            float rOppTG = 1.0f - rTP;

            float rMWCLive;
            if (p.Win <= rOppTG) // Opponent is too good to double
            {
                rMWCLive = mwcLose + (rMWCOppCash - mwcLose) * p.Win / rOppTG;
            }
            else
            {
                rMWCLive = rMWCOppCash + (mwcWin - rMWCOppCash) * (p.Win - rOppTG) / (1.0f - rOppTG);
            }

            return rMWCDead * (1.0f - rCubeX) + rMWCLive * rCubeX;
        }
    }
}