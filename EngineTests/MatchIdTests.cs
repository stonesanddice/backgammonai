using System;
using EngineCore;
using Xunit;

namespace EngineTests
{
    public class MatchIdTests
    {
        // Standard starting money game in GNUBG (1-cube, centered, score 0-0)
        private const string StandardMoneyGameId = "MAAAAAAAAAAE";

        [Fact]
        public void Parse_StandardMoneyGame_DecodesCorrectly()
        {
            MatchState state = MatchId.Decode(StandardMoneyGameId);

            Assert.Equal(1, state.Cube);
            Assert.Equal(-1, state.CubeOwner);
            Assert.Equal(0, state.PlayerOnRoll); // Back to 0!
            Assert.False(state.IsCrawford);
            Assert.Equal(0, state.MatchLength);
            Assert.Equal(0, state.Player0Score);
            Assert.Equal(0, state.Player1Score);
            Assert.Equal(0, state.Dice[0]);
            Assert.Equal(0, state.Dice[1]);
        }

        [Theory]
        [InlineData("MAAAAAAAAAAE")] // Baseline
        [InlineData("QYngASAAIAAE")] // Mid-game example, 7-point match
        [InlineData("MBkiAAAAAAAE")] // Player holding a 4-cube
        public void RoundTrip_DecodeThenEncode_MatchesOriginalString(string matchId)
        {
            // Arrange
            MatchState decodedState = MatchId.Decode(matchId);

            // Act
            string reEncodedId = MatchId.Encode(decodedState);

            // Assert
            Assert.Equal(matchId, reEncodedId);
        }

        [Fact]
        public void Encode_SortsDiceCorrectly_MatchesGnuBgSpec()
        {
            // Arrange: GNUBG spec mandates the higher die is stored in Dice[0] during encoding.
            MatchState state = new MatchState
            {
                Dice = new[] { 2, 5 } // Pass lower die first
            };

            // Act
            string encoded = MatchId.Encode(state);
            MatchState reDecoded = MatchId.Decode(encoded);

            // Assert
            Assert.Equal(5, reDecoded.Dice[0]); // Should be swapped
            Assert.Equal(2, reDecoded.Dice[1]);
        }

        [Fact]
        public void Parse_InvalidLengthString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => MatchId.Decode("cAgqAAAAA"));
            Assert.Contains("12 characters", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}