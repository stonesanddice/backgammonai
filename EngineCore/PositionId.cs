using System;

namespace EngineCore
{
    public static class PositionId
    {
        private const string Base64Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        /// <summary>
        /// Translates a GameState into a 14-character GNUBG Position ID.
        /// (Equivalent to oldPositionIDFromKey and oldPositionKey in GNUBG)
        /// </summary>
        public static string Encode(GameState state)
        {
            byte[] auch = new byte[10];
            int iBit = 0;

            // 1. Pack the board into 80 bits (10 bytes)
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 25; j++)
                {
                    int nc = state.Board[i, j];
                    if (nc > 0)
                    {
                        AddBits(auch, iBit, nc);
                        iBit += nc + 1;
                    }
                    else
                    {
                        iBit++;
                    }
                }
            }

            // 2. Convert the 10 bytes into 14 Base64 characters
            char[] szID = new char[14];
            int charIdx = 0;
            int byteIdx = 0;

            for (int i = 0; i < 3; i++)
            {
                szID[charIdx++] = Base64Alphabet[auch[byteIdx] >> 2];
                szID[charIdx++] = Base64Alphabet[((auch[byteIdx] & 0x03) << 4) | (auch[byteIdx + 1] >> 4)];
                szID[charIdx++] = Base64Alphabet[((auch[byteIdx + 1] & 0x0F) << 2) | (auch[byteIdx + 2] >> 6)];
                szID[charIdx++] = Base64Alphabet[auch[byteIdx + 2] & 0x3F];

                byteIdx += 3;
            }

            // Handle the final byte
            szID[charIdx++] = Base64Alphabet[auch[byteIdx] >> 2];
            szID[charIdx++] = Base64Alphabet[(auch[byteIdx] & 0x03) << 4];

            return new string(szID);
        }

        /// <summary>
        /// Translates a 14-character GNUBG Position ID into a GameState.
        /// (Equivalent to PositionFromID and oldPositionFromKey in GNUBG)
        /// </summary>
        public static GameState Decode(string positionId)
        {
            if (string.IsNullOrEmpty(positionId) || positionId.Length != 14)
            {
                throw new ArgumentException("GNUBG Position IDs must be exactly 14 characters long.");
            }

            byte[] auch = new byte[10];
            byte[] pch = new byte[14];

            // 1. Map Base64 chars to 6-bit values
            for (int i = 0; i < 14; i++)
            {
                pch[i] = Base64CharToByte(positionId[i]);
            }

            // 2. Unpack the 14 Base64 values into 10 bytes
            int puchIdx = 0;
            int pchIdx = 0;
            for (int i = 0; i < 3; i++)
            {
                auch[puchIdx++] = (byte)((pch[pchIdx] << 2) | (pch[pchIdx + 1] >> 4));
                auch[puchIdx++] = (byte)((pch[pchIdx + 1] << 4) | (pch[pchIdx + 2] >> 2));
                auch[puchIdx++] = (byte)((pch[pchIdx + 2] << 6) | pch[pchIdx + 3]);

                pchIdx += 4;
            }
            auch[puchIdx] = (byte)((pch[pchIdx] << 2) | (pch[pchIdx + 1] >> 4));

            // 3. Decode the 10 bytes into the board
            GameState state = new GameState();
            int player = 0;
            int point = 0;

            for (int i = 0; i < 10; i++)
            {
                byte cur = auch[i];

                for (int k = 0; k < 8; k++)
                {
                    if ((cur & 0x01) != 0)
                    {
                        if (player >= 2 || point >= 25)
                        {
                            // GNUBG error guard: string is malformed
                            return state; 
                        }
                        state.Board[player, point]++;
                    }
                    else
                    {
                        point++;
                        if (point == 25)
                        {
                            player++;
                            point = 0;
                        }
                    }
                    cur >>= 1;
                }
            }

            return state;
        }

        /// <summary>
        /// Replicates GNUBG's bitwise shifting injection for generating keys.
        /// </summary>
        private static void AddBits(byte[] auchKey, int bitPos, int nBits)
        {
            int k = bitPos / 8;
            int r = bitPos & 0x7;
            int b = ((1 << nBits) - 1) << r;

            auchKey[k] |= (byte)b;

            if (k < 8)
            {
                auchKey[k + 1] |= (byte)(b >> 8);
                auchKey[k + 2] |= (byte)(b >> 16);
            }
            else if (k == 8)
            {
                auchKey[k + 1] |= (byte)(b >> 8);
            }
        }

        /// <summary>
        /// Custom GNUBG Base64 decoder function.
        /// </summary>
        private static byte Base64CharToByte(char ch)
        {
            if (ch >= 'A' && ch <= 'Z') return (byte)(ch - 'A');
            if (ch >= 'a' && ch <= 'z') return (byte)(ch - 'a' + 26);
            if (ch >= '0' && ch <= '9') return (byte)(ch - '0' + 52);
            if (ch == '+') return 62;
            if (ch == '/') return 63;
    
            // Throw standard FormatException to satisfy our tests for invalid characters
            throw new FormatException($"Invalid Base64 character encountered: {ch}");
        }
    }
}