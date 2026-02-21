using System;
using Xunit;
using EngineCore; // Adjust if your namespace is different

namespace EngineTests
{
    public class PositionIdTests
    {
        // The universally recognized GNUBG starting position
        private const string StartingPositionId = "4HPwATDgc/ABMA";

        [Fact]
        public void Parse_StartingPosition_CreatesCorrectBoardState()
        {
            // Arrange & Act
            GameState state = PositionId.Decode(StartingPositionId);

            // Assert
            // In GNUBG, the board is encoded from the perspective of the player on roll.
            // In our engine, Player 0 (On Roll) is mapped to Player2Checkers.
            // Remember: Point N corresponds to Array Index N - 1.

            // Assert Player 0's checker counts (state.Player2Checkers)
            Assert.Equal(5, state.Player2Checkers[5]);  // 6-point
            Assert.Equal(3, state.Player2Checkers[7]);  // 8-point
            Assert.Equal(5, state.Player2Checkers[12]); // 13-point
            Assert.Equal(2, state.Player2Checkers[23]); // 24-point

            // Verify the bar (Index 24) is empty
            Assert.Equal(0, state.Player2Checkers[24]);

            // Assert Player 1's checker counts (state.Player1Checkers)
            Assert.Equal(5, state.Player1Checkers[5]);  // 6-point
            Assert.Equal(3, state.Player1Checkers[7]);  // 8-point
            Assert.Equal(5, state.Player1Checkers[12]); // 13-point
            Assert.Equal(2, state.Player1Checkers[23]); // 24-point

            // Verify the opponent's bar is empty
            Assert.Equal(0, state.Player1Checkers[24]);
        }

        [Theory]
        [InlineData("4HPwATDgc/ABMA")] // Standard Starting Position
        [InlineData("m2PwATDgc/ABMA")] // Example: After an opening 5-2 (24/22 13/8)
        [InlineData("4HPhASjgc/ABMA")] // Example: After an opening 6-3 (24/21 13/8)
        [InlineData("AAAAAAAAAAAAAA")] // Extreme edge case: All 15 checkers borne off (game over)
        public void RoundTrip_DecodeThenEncode_MatchesOriginalString(string positionId)
        {
            // Arrange
            GameState decodedState = PositionId.Decode(positionId);

            // Act
            string reEncodedId = PositionId.Encode(decodedState);

            // Assert
            Assert.Equal(positionId, reEncodedId);
        }

        [Fact]
        public void Parse_InvalidLengthString_ThrowsArgumentException()
        {
            // Arrange
            string invalidId = "4HPwATD"; // GNUBG position IDs are exactly 14 base64 chars

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => PositionId.Decode(invalidId));

            // Optional: Assert the exception message is helpful
            Assert.Contains("14 characters", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Parse_InvalidBase64Characters_ThrowsFormatException()
        {
            // Arrange
            // Contains invalid characters like '$' and '%'
            string invalidId = "4HPwA$Dgc/%BMA";

            // Act & Assert
            Assert.Throws<FormatException>(() => PositionId.Decode(invalidId));
        }

        [Fact]
        public void Parse_StartingPosition_PipCountIs167PerSide()
        {
            GameState state = PositionId.Decode(StartingPositionId);
            int pipCountP1 = PipCount(state.Player1Checkers);
            int pipCountP2 = PipCount(state.Player2Checkers);
            Assert.Equal(167, pipCountP1);
            Assert.Equal(167, pipCountP2);
        }

        private static int PipCount(int[] checkers)
        {
            int pips = 0;
            for (int i = 0; i < 24; i++) pips += checkers[i] * (i + 1);
            pips += checkers[24] * 25;
            return pips;
        }
    }
}