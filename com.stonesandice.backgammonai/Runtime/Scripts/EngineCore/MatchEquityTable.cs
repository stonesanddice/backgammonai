using System;

namespace EngineCore
{
    public static class MatchEquityTable
    {
        // Standard Match Equity Table (Values in Match Winning Chances: 0.0f to 1.0f)
        // Indices are [PlayerAway, OpponentAway]
        // 0-away means the match is won.
        private static readonly float[,] _met = new float[,]
        {
            // Opponent Away ->
            // 0      1      2      3      4      5      6      7      8      9      10     11     12     13     14     15
            { 0.5f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f,  1.0f  }, // 0 Away (Won)
            { 0.0f,  0.500f,0.697f,0.824f,0.893f,0.936f,0.961f,0.976f,0.985f,0.991f,0.995f,0.997f,0.998f,0.999f,0.999f,0.999f}, // 1 Away
            { 0.0f,  0.303f,0.500f,0.598f,0.678f,0.745f,0.796f,0.838f,0.870f,0.896f,0.917f,0.934f,0.948f,0.958f,0.967f,0.974f}, // 2 Away
            { 0.0f,  0.176f,0.402f,0.500f,0.585f,0.655f,0.716f,0.766f,0.809f,0.843f,0.872f,0.896f,0.916f,0.932f,0.945f,0.956f}, // 3 Away
            { 0.0f,  0.107f,0.322f,0.415f,0.500f,0.575f,0.640f,0.696f,0.745f,0.787f,0.823f,0.853f,0.879f,0.901f,0.919f,0.934f}, // 4 Away
            { 0.0f,  0.064f,0.255f,0.345f,0.425f,0.500f,0.568f,0.628f,0.682f,0.729f,0.771f,0.807f,0.838f,0.865f,0.888f,0.907f}, // 5 Away
            { 0.0f,  0.039f,0.204f,0.284f,0.360f,0.432f,0.500f,0.563f,0.620f,0.672f,0.718f,0.759f,0.795f,0.826f,0.853f,0.877f}, // 6 Away
            { 0.0f,  0.024f,0.162f,0.234f,0.304f,0.372f,0.437f,0.500f,0.560f,0.615f,0.665f,0.709f,0.749f,0.785f,0.816f,0.844f}, // 7 Away
            { 0.0f,  0.015f,0.130f,0.191f,0.255f,0.318f,0.380f,0.440f,0.500f,0.557f,0.610f,0.658f,0.702f,0.741f,0.776f,0.808f}, // 8 Away
            { 0.0f,  0.009f,0.104f,0.157f,0.213f,0.271f,0.328f,0.385f,0.443f,0.500f,0.554f,0.605f,0.652f,0.695f,0.734f,0.769f}, // 9 Away
            { 0.0f,  0.005f,0.083f,0.128f,0.177f,0.229f,0.282f,0.335f,0.390f,0.446f,0.500f,0.552f,0.601f,0.647f,0.689f,0.727f}, // 10 Away
            { 0.0f,  0.003f,0.066f,0.104f,0.147f,0.193f,0.241f,0.291f,0.342f,0.395f,0.448f,0.500f,0.550f,0.597f,0.642f,0.683f}, // 11 Away
            { 0.0f,  0.002f,0.052f,0.084f,0.121f,0.162f,0.205f,0.251f,0.298f,0.348f,0.399f,0.450f,0.500f,0.548f,0.594f,0.637f}, // 12 Away
            { 0.0f,  0.001f,0.042f,0.068f,0.099f,0.135f,0.174f,0.215f,0.259f,0.305f,0.353f,0.403f,0.452f,0.500f,0.547f,0.591f}, // 13 Away
            { 0.0f,  0.001f,0.033f,0.055f,0.081f,0.112f,0.147f,0.184f,0.224f,0.266f,0.311f,0.358f,0.406f,0.453f,0.500f,0.545f}, // 14 Away
            { 0.0f,  0.001f,0.026f,0.044f,0.066f,0.093f,0.123f,0.156f,0.192f,0.231f,0.273f,0.317f,0.363f,0.409f,0.455f,0.500f}  // 15 Away
        };

        /// <summary>
        /// Gets the Match Winning Chance (MWC) for a player given the current score.
        /// </summary>
        public static float GetMatchWinningChance(int playerAway, int opponentAway)
        {
            // Clamp negative or zero values to 0 (won)
            playerAway = Math.Max(0, playerAway);
            opponentAway = Math.Max(0, opponentAway);

            if (playerAway == 0) return 1.0f; // Match is already won
            if (opponentAway == 0) return 0.0f; // Match is already lost

            // If the score goes deeper than 15-away, we extrapolate based on the score difference.
            if (playerAway >= _met.GetLength(0) || opponentAway >= _met.GetLength(1))
            {
                // A standard heuristic for deep matches: 50% + (Difference * ~3%)
                float diff = opponentAway - playerAway;
                float mwc = 0.5f + (diff * 0.03f);
                return Math.Clamp(mwc, 0.0f, 1.0f);
            }

            return _met[playerAway, opponentAway];
        }

        /// <summary>
        /// Calculates MWC if the current game is won with a specific multiplier (1=Normal, 2=Gammon, 3=Backgammon).
        /// </summary>
        public static float GetMwcIfWon(int playerAway, int opponentAway, int cubeValue, int gameMultiplier = 1)
        {
            int pointsWon = cubeValue * gameMultiplier;
            return GetMatchWinningChance(playerAway - pointsWon, opponentAway);
        }

        /// <summary>
        /// Calculates MWC if the current game is lost with a specific multiplier.
        /// </summary>
        public static float GetMwcIfLost(int playerAway, int opponentAway, int cubeValue, int gameMultiplier = 1)
        {
            int pointsLost = cubeValue * gameMultiplier;
            return GetMatchWinningChance(playerAway, opponentAway - pointsLost);
        }
    }
}