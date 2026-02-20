using System;

namespace EngineCore
{
    public static class FeatureEncoder
    {
        private struct OneWayToHit
        {
            public bool fAll;
            public int[] anIntermediate;
            public int nFaces;
            public int nPips;
            public OneWayToHit(bool f, int i0, int i1, int i2, int nf, int np)
            {
                fAll = f; anIntermediate = new[] { i0, i1, i2 }; nFaces = nf; nPips = np;
            }
        }

        private struct RollInfo { public int nPips; public int nChequers; }

        private static readonly int[] anEscapes = new int[4096];
        private static readonly int[] anEscapes1 = new int[4096];

        private static readonly int[,] aaRoll = {
            {0,2,5,9}, {0,1,4,-1}, {1,8,17,24}, {0,3,7,-1}, {1,3,12,-1}, {3,16,27,33},
            {0,6,11,-1}, {1,6,15,-1}, {3,6,20,-1}, {6,23,32,35}, {0,10,14,-1}, {1,10,19,-1},
            {3,10,22,-1}, {6,10,26,-1}, {10,29,34,37}, {0,13,18,-1}, {1,13,21,-1},
            {3,13,25,-1}, {6,13,28,-1}, {10,13,30,-1}, {13,31,36,38}
        };

        private static readonly int[,] aanCombination = {
            {0,-1,-1,-1,-1}, {1,2,-1,-1,-1}, {3,4,5,-1,-1}, {6,7,8,9,-1}, {10,11,12,-1,-1}, {13,14,15,16,17},
            {18,19,20,-1,-1}, {21,22,23,24,-1}, {25,26,27,-1,-1}, {28,29,-1,-1,-1}, {30,-1,-1,-1,-1}, {31,32,33,-1,-1},
            {-1,-1,-1,-1,-1}, {-1,-1,-1,-1,-1}, {34,-1,-1,-1,-1}, {35,-1,-1,-1,-1}, {-1,-1,-1,-1,-1}, {36,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1}, {37,-1,-1,-1,-1}, {-1,-1,-1,-1,-1}, {-1,-1,-1,-1,-1}, {-1,-1,-1,-1,-1}, {38,-1,-1,-1,-1}
        };

        private static readonly OneWayToHit[] aIntermediate = {
            new(true,0,0,0,1,1), new(true,0,0,0,1,2), new(true,1,0,0,2,2), new(true,0,0,0,1,3),
            new(false,1,2,0,2,3), new(true,1,2,0,3,3), new(true,0,0,0,1,4), new(false,1,3,0,2,4),
            new(true,2,0,0,2,4), new(true,1,2,3,4,4), new(true,0,0,0,1,5), new(false,1,4,0,2,5),
            new(false,2,3,0,2,5), new(true,0,0,0,1,6), new(false,1,5,0,2,6), new(false,2,4,0,2,6),
            new(true,3,0,0,2,6), new(true,2,4,0,3,6), new(false,1,6,0,2,7), new(false,2,5,0,2,7),
            new(false,3,4,0,2,7), new(false,2,6,0,2,8), new(false,3,5,0,2,8), new(true,4,0,0,2,8),
            new(true,2,4,6,4,8), new(false,3,6,0,2,9), new(false,4,5,0,2,9), new(true,3,6,0,3,9),
            new(false,4,6,0,2,10), new(true,5,0,0,2,10), new(false,5,6,0,2,11), new(true,6,0,0,2,12),
            new(true,4,8,0,3,12), new(true,3,6,9,4,12), new(true,5,10,0,3,15), new(true,4,8,12,4,16),
            new(true,6,12,0,3,18), new(true,5,10,15,4,20), new(true,6,12,18,4,24)
        };

        static FeatureEncoder()
        {
            for (int i = 0; i < 4096; ++i)
            {
                int c = 0;
                for (int n0 = 0; n0 <= 5; n0++)
                    for (int n1 = 0; n1 <= n0; n1++)
                        if ((i & (1 << (n0 + n1 + 1))) == 0 && !((i & (1 << n0)) != 0 && (i & (1 << n1)) != 0))
                            c += (n0 == n1) ? 1 : 2;
                anEscapes[i] = c;
            }
            anEscapes1[0] = 0;
            for (int i = 1; i < 4096; i++)
            {
                int c = 0, low = 0;
                while ((i & (1 << low)) == 0) ++low;
                for (int n0 = 0; n0 <= 5; n0++)
                    for (int n1 = 0; n1 <= n0; n1++)
                        if ((n0 + n1 + 1 > low) && (i & (1 << (n0 + n1 + 1))) == 0 && !((i & (1 << n0)) != 0 && (i & (1 << n1)) != 0))
                            c += (n0 == n1) ? 1 : 2;
                anEscapes1[i] = c;
            }
        }

        public static float[] EncodeContact(int[] onRoll, int[] waiting)
        {
            float[] inputs = new float[250];

            // 1. Base Inputs
            // GNUBG index 0 is Opponent (waiting), index 1 is To Play (onRoll)
            EncodeBase(inputs, 0, waiting);
            EncodeBase(inputs, 100, onRoll);

            // 2. Advanced Heuristic Inputs
            EncodeHalf(inputs, 200, onRoll, waiting);
            EncodeHalf(inputs, 225, waiting, onRoll);

            return inputs;
        }

        private static void EncodeBase(float[] inputs, int offset, int[] board)
        {
            for (int i = 0; i < 24; i++)
            {
                int nc = board[i];
                inputs[offset++] = nc == 1 ? 1f : 0f;
                inputs[offset++] = nc == 2 ? 1f : 0f;
                inputs[offset++] = nc >= 3 ? 1f : 0f;
                inputs[offset++] = nc > 3 ? (nc - 3) / 2.0f : 0f;
            }
            int bar = board[24];
            inputs[offset++] = bar >= 1 ? 1f : 0f;
            inputs[offset++] = bar >= 2 ? 1f : 0f;  // GNUBG explicitly uses >=1, >=2, >=3 for the Bar (unlike points)
            inputs[offset++] = bar >= 3 ? 1f : 0f;
            inputs[offset++] = bar > 3 ? (bar - 3) / 2.0f : 0f;
        }

        private static void EncodeHalf(float[] inputs, int offset, int[] board, int[] opp)
        {
            int oppBack = GetOppBack(opp);

            // OFF1, OFF2, OFF3
            int menOff = 15;
            for (int i = 0; i < 25; i++) menOff -= board[i];
            if (menOff > 5) { inputs[offset + 0] = 1f; inputs[offset + 1] = 1f; inputs[offset + 2] = (menOff - 6) / 3f; }
            else if (menOff > 2) { inputs[offset + 0] = 1f; inputs[offset + 1] = (menOff - 3) / 3f; inputs[offset + 2] = 0f; }
            else { inputs[offset + 0] = menOff > 0 ? menOff / 3f : 0f; inputs[offset + 1] = 0f; inputs[offset + 2] = 0f; }

            // BREAK_CONTACT
            int bc = 0;
            for (int i = oppBack + 1; i < 25; i++) if (board[i] > 0) bc += (i + 1 - oppBack) * board[i];
            inputs[offset + 3] = bc / (15f + 152f);

            // BACK_CHEQUER
            int backCheq = 0;
            for (int n = 24; n >= 0; --n) if (board[n] > 0) { backCheq = n; break; }
            inputs[offset + 4] = backCheq / 24f;

            // BACK_ANCHOR
            int backAnch = 0;
            for (int n = (backCheq == 24 ? 23 : backCheq); n >= 0; --n) if (board[n] >= 2) { backAnch = n; break; }
            inputs[offset + 5] = backAnch / 24f;

            // FORWARD_ANCHOR
            int nForw = 0;
            for (int j = 18; j <= backAnch; ++j) if (board[j] >= 2) { nForw = 24 - j; break; }
            if (nForw == 0) for (int j = 17; j >= 12; --j) if (board[j] >= 2) { nForw = 24 - j; break; }
            inputs[offset + 6] = nForw == 0 ? 2f : nForw / 6f;

            // PIPLOSS, P1, P2
            CalculatePipLoss(board, opp, out float pipLoss, out float p1, out float p2);
            inputs[offset + 7] = pipLoss; inputs[offset + 8] = p1; inputs[offset + 9] = p2;

            // BACKESCAPES & BACKRESCAPES
            inputs[offset + 10] = Escapes(board, 23 - oppBack, false) / 36f;
            inputs[offset + 24] = Escapes(board, 23 - oppBack, true) / 36f;

            // ACONTAIN & CONTAIN
            int minACont = 36, minCont = 36;
            for (int i = 15; i < 24 - oppBack; i++) minACont = Math.Min(minACont, Escapes(board, i, false));
            for (int i = 15; i < 24; i++) minCont = Math.Min(minCont, Escapes(board, i, false));
            inputs[offset + 11] = (36f - minACont) / 36f; inputs[offset + 12] = inputs[offset + 11] * inputs[offset + 11];
            inputs[offset + 13] = (36f - minCont) / 36f; inputs[offset + 14] = inputs[offset + 13] * inputs[offset + 13];

            // MOBILITY
            int mob = 0;
            for (int i = 6; i < 25; ++i) if (board[i] > 0) mob += (i - 5) * board[i] * Escapes(opp, i, false);
            inputs[offset + 15] = mob / 3600f;

            // MOMENT2
            int mj = 0, mn = 0;
            for (int i = 0; i < 25; i++) if (board[i] > 0) { mj += board[i]; mn += i * board[i]; }
            if (mj > 0) mn = (mn + mj - 1) / mj;
            mj = 0; int mk = 0;
            for (int i = mn + 1; i < 25; i++) if (board[i] > 0) { mj += board[i]; mk += board[i] * (i - mn) * (i - mn); }
            if (mj > 0) mk = (mk + mj - 1) / mj;
            inputs[offset + 16] = mk / 400f;

            // ENTER & ENTER2
            inputs[offset + 17] = board[24] > 0 ? EnterLoss(opp, board[24] > 1) / (36f * (49f / 6f)) : 0f;
            int numClosed = 0;
            for (int i = 0; i < 6; i++) if (opp[i] > 1) numClosed++;
            inputs[offset + 18] = (36f - (numClosed - 6) * (numClosed - 6)) / 36f;

            // TIMING
            int t = 0, no = 0;
            t += 24 * board[24]; no += board[24];
            int ti = 23;
            for (; ti >= 12 && ti > oppBack; --ti) if (board[ti] > 0 && board[ti] != 2) { int n = board[ti] > 2 ? board[ti] - 2 : 1; no += n; t += ti * n; }
            for (; ti >= 6; --ti) if (board[ti] > 0) { int n = board[ti]; no += n; t += ti * n; }
            for (ti = 5; ti >= 0; --ti)
            {
                if (board[ti] > 2) { t += ti * (board[ti] - 2); no += board[ti] - 2; }
                else if (board[ti] < 2) { int n = 2 - board[ti]; if (no >= n) { t -= ti * n; no -= n; } }
            }
            inputs[offset + 19] = Math.Max(0, t) / 100f;

            // BACKBONE
            int pa = -1, bbw = 0, bbtot = 0;
            for (int np = 23; np > 0; --np)
            {
                if (board[np] >= 2)
                {
                    if (pa == -1) { pa = np; continue; }
                    int d = pa - np;
                    bbw += (d <= 6 ? 11 : (d <= 11 ? 13 - d : 0)) * board[pa];
                    bbtot += board[pa];
                }
            }
            inputs[offset + 20] = bbtot > 0 ? 1f - (bbw / (bbtot * 11f)) : 0f;

            // BACKG & BACKG1
            int nAc = 0, totBack = board[24];
            for (int i = 18; i < 24; ++i) { totBack += board[i]; if (board[i] > 1) nAc++; }
            inputs[offset + 21] = nAc > 1 ? (totBack - 3) / 4f : 0f;
            inputs[offset + 22] = nAc == 1 ? totBack / 8f : 0f;

            // FREEPIP
            int fp = 0;
            for (int i = 0; i < oppBack; i++) if (board[i] > 0) fp += (i + 1) * board[i];
            inputs[offset + 23] = fp / 100f;
        }

        private static void CalculatePipLoss(int[] board, int[] opp, out float pipLoss, out float p1, out float p2)
        {
            int nBoard = 0;
            for (int i = 0; i < 6; i++) if (board[i] >= 2) nBoard++;

            int[] aHit = new int[39];
            for (int i = nBoard > 2 ? 23 : 21; i >= 0; i--)
            {
                if (opp[i] == 1)
                {
                    for (int j = 24 - i; j < 25; j++)
                    {
                        if (board[j] > 0 && !(j < 6 && board[j] == 2))
                        {
                            for (int n = 0; n < 5; n++)
                            {
                                int comb = aanCombination[j - 24 + i, n];
                                if (comb == -1) break;
                                OneWayToHit pi = aIntermediate[comb];
                                if (pi.fAll)
                                {
                                    if (pi.nFaces > 1)
                                    {
                                        bool blocked = false;
                                        for (int k = 0; k < 3 && pi.anIntermediate[k] > 0; k++)
                                            if (opp[i - pi.anIntermediate[k]] > 1) { blocked = true; break; }
                                        if (blocked) continue;
                                    }
                                }
                                else
                                {
                                    if (opp[i - pi.anIntermediate[0]] > 1 && opp[i - pi.anIntermediate[1]] > 1) continue;
                                }
                                aHit[comb] |= 1 << j;
                            }
                        }
                    }
                }
            }

            RollInfo[] aRoll = new RollInfo[21];
            if (board[24] == 0)
            {
                for (int i = 0; i < 21; i++)
                {
                    int lastK = -1;
                    for (int j = 0; j < 4; j++)
                    {
                        int r = aaRoll[i, j];
                        if (r < 0) break;
                        if (aHit[r] == 0) continue;
                        OneWayToHit pi = aIntermediate[r];
                        if (pi.nFaces == 1)
                        {
                            for (int k = 23; k > 0; k--)
                            {
                                if ((aHit[r] & (1 << k)) != 0)
                                {
                                    if (lastK != k || board[k] > 1) aRoll[i].nChequers++;
                                    lastK = k;
                                    if (k - pi.nPips + 1 > aRoll[i].nPips) aRoll[i].nPips = k - pi.nPips + 1;
                                    if (aaRoll[i, 3] >= 0 && (aHit[r] & ~(1 << k)) != 0) aRoll[i].nChequers++;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (aRoll[i].nChequers == 0) aRoll[i].nChequers = 1;
                            int maxK = -1;
                            for (int k = 23; k >= 0; k--) if ((aHit[r] & (1 << k)) != 0) { maxK = k; break; }
                            if (maxK - pi.nPips + 1 > aRoll[i].nPips) aRoll[i].nPips = maxK - pi.nPips + 1;
                            for (int l = 0; l < 3 && pi.anIntermediate[l] > 0; l++)
                                if (opp[23 - maxK + pi.anIntermediate[l]] == 1) { aRoll[i].nChequers++; break; }
                        }
                    }
                }
            }
            else if (board[24] == 1)
            {
                for (int i = 0; i < 21; i++)
                {
                    int lastN = 0;
                    for (int j = 0; j < 4; j++)
                    {
                        int r = aaRoll[i, j];
                        if (r < 0) break;
                        if (aHit[r] == 0) continue;
                        OneWayToHit pi = aIntermediate[r];
                        if (pi.nFaces == 1)
                        {
                            for (int k = 24; k > 0; k--)
                            {
                                if ((aHit[r] & (1 << k)) != 0)
                                {
                                    if (lastN != 0 && k != 24) break;
                                    if (k != 24)
                                    {
                                        int npip = aIntermediate[aaRoll[i, 1 - j]].nPips;
                                        if (opp[npip - 1] > 1) break;
                                        lastN = 1;
                                    }
                                    aRoll[i].nChequers++;
                                    if (k - pi.nPips + 1 > aRoll[i].nPips) aRoll[i].nPips = k - pi.nPips + 1;
                                }
                            }
                        }
                        else
                        {
                            if ((aHit[r] & (1 << 24)) == 0) continue;
                            if (aRoll[i].nChequers == 0) aRoll[i].nChequers = 1;
                            if (25 - pi.nPips > aRoll[i].nPips) aRoll[i].nPips = 25 - pi.nPips;
                            for (int k = 0; k < 3 && pi.anIntermediate[k] > 0; k++)
                                if (opp[pi.anIntermediate[k] + 1] == 1) { aRoll[i].nChequers++; break; }
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < 21; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int r = aaRoll[i, j];
                        if ((aHit[r] & (1 << 24)) == 0) continue;
                        OneWayToHit pi = aIntermediate[r];
                        if (pi.nFaces != 1) continue;
                        aRoll[i].nChequers++;
                        if (25 - pi.nPips > aRoll[i].nPips) aRoll[i].nPips = 25 - pi.nPips;
                    }
                }
            }

            int np = 0, n1 = 0, n2 = 0;
            for (int i = 0; i < 21; i++)
            {
                int w = aaRoll[i, 3] >= 0 ? 1 : 2;
                int nc = aRoll[i].nChequers;
                np += aRoll[i].nPips * w;
                if (nc > 0) { n1 += w; if (nc > 1) n2 += w; }
            }
            pipLoss = np / (12f * 36f); p1 = n1 / 36f; p2 = n2 / 36f;
        }

        private static int GetOppBack(int[] opp)
        {
            for (int n = 24; n >= 0; --n) if (opp[n] > 0) return 23 - n;
            return 23;
        }

        private static int Escapes(int[] board, int n, bool useR)
        {
            int af = 0;
            for (int i = 0; i < 12 && i < n; ++i) if (board[24 - n + i] > 1) af |= (1 << i);
            return useR ? anEscapes1[af] : anEscapes[af];
        }

        private static int EnterLoss(int[] op, bool two)
        {
            int loss = 0;
            for (int i = 0; i < 6; ++i)
            {
                if (op[i] > 1)
                {
                    loss += 4 * (i + 1);
                    for (int j = i + 1; j < 6; ++j)
                    {
                        if (op[j] > 1) loss += 2 * (i + j + 2);
                        else if (two) loss += 2 * (i + 1);
                    }
                }
                else if (two)
                {
                    for (int j = i + 1; j < 6; ++j) if (op[j] > 1) loss += 2 * (j + 1);
                }
            }
            return loss;
        }
    }
}