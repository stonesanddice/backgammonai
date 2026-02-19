using System;

namespace EngineCore
{
    public static class MatchId
    {
        private const string Base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        public static string Encode(MatchState state)
        {
            byte[] auchKey = new byte[9];

            SetBits(auchKey, 0, 4, LogCube(state.Cube));
            SetBits(auchKey, 4, 2, state.CubeOwner & 0x3);
            SetBits(auchKey, 6, 1, state.PlayerOnRoll);
            SetBits(auchKey, 7, 1, state.IsCrawford ? 1 : 0);
            SetBits(auchKey, 8, 3, state.GameState);
            SetBits(auchKey, 11, 1, state.Turn);
            SetBits(auchKey, 12, 1, state.Doubled ? 1 : 0);
            SetBits(auchKey, 13, 2, state.Resigned);

            // GNUBG always stores the higher die first
            int d1 = state.Dice[0] >= state.Dice[1] ? state.Dice[0] : state.Dice[1];
            int d2 = state.Dice[0] >= state.Dice[1] ? state.Dice[1] : state.Dice[0];

            SetBits(auchKey, 15, 3, d1 & 0x7);
            SetBits(auchKey, 18, 3, d2 & 0x7);
            SetBits(auchKey, 21, 15, state.MatchLength & 0x7FFF);
            SetBits(auchKey, 36, 15, state.Player0Score & 0x7FFF);
            SetBits(auchKey, 51, 15, state.Player1Score & 0x7FFF);
            SetBits(auchKey, 66, 1, state.JacobyRule ? 0 : 1); // Note: C code stores !fJacobyInUse

            return MatchIDFromKey(auchKey);
        }

        public static MatchState Decode(string matchId)
        {
            if (string.IsNullOrEmpty(matchId) || matchId.Length != 12)
            {
                throw new ArgumentException("GNUBG Match IDs must be exactly 12 characters long.");
            }

            byte[] ach = new byte[12];
            for (int i = 0; i < 12; i++)
            {
                ach[i] = Base64CharToByte(matchId[i]);
            }

            byte[] auchKey = new byte[9];
            int puchIdx = 0;
            int pchIdx = 0;

            for (int i = 0; i < 3; i++)
            {
                auchKey[puchIdx++] = (byte)((ach[pchIdx] << 2) | (ach[pchIdx + 1] >> 4));
                auchKey[puchIdx++] = (byte)((ach[pchIdx + 1] << 4) | (ach[pchIdx + 2] >> 2));
                auchKey[puchIdx++] = (byte)((ach[pchIdx + 2] << 6) | ach[pchIdx + 3]);
                pchIdx += 4;
            }

            MatchState state = new MatchState();

            state.Cube = 1 << GetBits(auchKey, 0, 4);
            
            state.CubeOwner = GetBits(auchKey, 4, 2);
            if (state.CubeOwner != 0 && state.CubeOwner != 1) state.CubeOwner = -1; // -1 is centered

            state.PlayerOnRoll = GetBits(auchKey, 6, 1);
            state.IsCrawford = GetBits(auchKey, 7, 1) == 1;
            state.GameState = GetBits(auchKey, 8, 3);
            state.Turn = GetBits(auchKey, 11, 1);
            state.Doubled = GetBits(auchKey, 12, 1) == 1;
            state.Resigned = GetBits(auchKey, 13, 2);
            state.Dice[0] = GetBits(auchKey, 15, 3);
            state.Dice[1] = GetBits(auchKey, 18, 3);
            state.MatchLength = GetBits(auchKey, 21, 15);
            state.Player0Score = GetBits(auchKey, 36, 15);
            state.Player1Score = GetBits(auchKey, 51, 15);
            state.JacobyRule = GetBits(auchKey, 66, 1) == 0; // Inverted in storage

            return state;
        }

        private static string MatchIDFromKey(byte[] puch)
        {
            char[] szID = new char[12];
            int pchIdx = 0;
            int byteIdx = 0;

            for (int i = 0; i < 3; i++)
            {
                szID[pchIdx++] = Base64Alphabet[puch[byteIdx] >> 2];
                szID[pchIdx++] = Base64Alphabet[((puch[byteIdx] & 0x03) << 4) | (puch[byteIdx + 1] >> 4)];
                szID[pchIdx++] = Base64Alphabet[((puch[byteIdx + 1] & 0x0F) << 2) | (puch[byteIdx + 2] >> 6)];
                szID[pchIdx++] = Base64Alphabet[puch[byteIdx + 2] & 0x3F];
                byteIdx += 3;
            }

            return new string(szID);
        }

        private static void SetBit(byte[] pc, int bitPos, int test, int iBit)
        {
            int k = bitPos / 8;
            byte rbit = (byte)(1 << (bitPos % 8));
            byte c = (test & (1 << iBit)) != 0 ? rbit : (byte)0;
            pc[k] = (byte)((pc[k] & (0xFF ^ rbit)) | c);
        }

        private static void SetBits(byte[] pc, int bitPos, int nBits, int iContent)
        {
            for (int i = 0; i < nBits; i++)
            {
                SetBit(pc, bitPos + i, iContent, i);
            }
        }

        private static int GetBits(byte[] pc, int bitPos, int nBits)
        {
            byte[] c = new byte[2];
            for (int i = 0, j = bitPos; i < nBits; i++, j++)
            {
                int k = j / 8;
                int r = j % 8;
                SetBit(c, i, pc[k], r);
            }
            return c[0] | (c[1] << 8);
        }

        private static int LogCube(int n)
        {
            int i = 0;
            while ((n >>= 1) != 0) i++;
            return i;
        }

        private static byte Base64CharToByte(char ch)
        {
            if (ch >= 'A' && ch <= 'Z') return (byte)(ch - 'A');
            if (ch >= 'a' && ch <= 'z') return (byte)(ch - 'a' + 26);
            if (ch >= '0' && ch <= '9') return (byte)(ch - '0' + 52);
            if (ch == '+') return 62;
            if (ch == '/') return 63;
            throw new FormatException($"Invalid Base64 character: {ch}");
        }
    }
}