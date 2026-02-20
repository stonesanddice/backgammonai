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
            // Assuming Parse returns your GameState or GnuBgPosition object
            GameState state = PositionId.Decode(StartingPositionId);

            // Assert
            // In GNUBG, the board is encoded from the perspective of the player on roll.
            // Player 0 is the player whose turn it is.

            // Assert Player 0's checker counts
            Assert.Equal(5, state.GetCheckers(player: 0, point: 6));
            Assert.Equal(3, state.GetCheckers(player: 0, point: 8));
            Assert.Equal(5, state.GetCheckers(player: 0, point: 13));
            Assert.Equal(2, state.GetCheckers(player: 0, point: 24));

            // Verify the bar and off-board are empty for the starting position
            Assert.Equal(0, state.GetCheckersOnBar(player: 0));
            Assert.Equal(0, state.GetCheckersOffBoard(player: 0));

            // Assert Player 1's checker counts (Opponent)
            // Depending on your implementation, this might be accessed via a 2D array 
            // or by looking at the same points from the opponent's perspective.
            Assert.Equal(5, state.GetCheckers(player: 1, point: 6));
            Assert.Equal(3, state.GetCheckers(player: 1, point: 8));
            Assert.Equal(5, state.GetCheckers(player: 1, point: 13));
            Assert.Equal(2, state.GetCheckers(player: 1, point: 24));
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
    }
}