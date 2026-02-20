using System;
using Xunit;
using EngineCore;

namespace EngineTests
{
    public class MatchEquityTests
    {
        [Fact]
        public void GetMatchWinningChance_StandardLookups_ReturnsTableValues()
        {
            // Assert against known values from your MatchEquityTable._met array

            // 1-away vs 1-away (Double Match Point) should be exactly 50%
            Assert.Equal(0.500f, MatchEquityTable.GetMatchWinningChance(1, 1));

            // 2-away vs 1-away (The leader has a huge advantage)
            // Player is 2-away, Opponent is 1-away. You should be the underdog (~30%)
            float mwcUnderdog = MatchEquityTable.GetMatchWinningChance(2, 1);
            Assert.Equal(0.303f, mwcUnderdog);

            // 1-away vs 2-away (You are the leader)
            float mwcLeader = MatchEquityTable.GetMatchWinningChance(1, 2);
            Assert.Equal(0.697f, mwcLeader);
        }

        [Fact]
        public void GetMatchWinningChance_IsSymmetric()
        {
            // For any score X-away vs Y-away, the MWC for one player 
            // plus the MWC for the other must equal 100% (1.0).
            int scoreA = 4;
            int scoreB = 7;

            float mwcA = MatchEquityTable.GetMatchWinningChance(scoreA, scoreB);
            float mwcB = MatchEquityTable.GetMatchWinningChance(scoreB, scoreA);

            Assert.Equal(1.0f, mwcA + mwcB, precision: 3);
        }

        [Fact]
        public void GetMatchWinningChance_BoundaryConditions_HandlesMatchOver()
        {
            // 0-away means the match is already finished and won.
            Assert.Equal(1.0f, MatchEquityTable.GetMatchWinningChance(0, 5));
            Assert.Equal(1.0f, MatchEquityTable.GetMatchWinningChance(-1, 5)); // Safety clamp

            // Opponent 0-away means you lost.
            Assert.Equal(0.0f, MatchEquityTable.GetMatchWinningChance(5, 0));
            Assert.Equal(0.0f, MatchEquityTable.GetMatchWinningChance(5, -2)); // Safety clamp
        }

        [Fact]
        public void GetMatchWinningChance_Extrapolation_HandlesDeepMatches()
        {
            // Your table goes up to 15-away. Let's test a 20-point match.
            // If scores are equal, even outside the table, it should return 50%.
            Assert.Equal(0.5f, MatchEquityTable.GetMatchWinningChance(20, 20));

            // If you are 25-away and opponent is 15-away, you are the underdog.
            float mwcBehind = MatchEquityTable.GetMatchWinningChance(25, 15);
            Assert.True(mwcBehind < 0.5f);

            // If you are 15-away and opponent is 30-away, you are the favorite.
            float mwcAhead = MatchEquityTable.GetMatchWinningChance(15, 30);
            Assert.True(mwcAhead > 0.5f);
        }

        [Theory]
        [InlineData(1)] // Win a single game
        [InlineData(2)] // Win a gammon
        [InlineData(3)] // Win a backgammon
        public void GetMwcIfWon_CalculatesCorrectFutureAwayScore(int multiplier)
        {
            // Current score: 5-away vs 5-away. Cube is at 2.
            int playerAway = 5;
            int oppAway = 5;
            int cubeValue = 2;

            // Act
            float result = MatchEquityTable.GetMwcIfWon(playerAway, oppAway, cubeValue, multiplier);

            // Calculation: 5-away - (2 * multiplier)
            int expectedFutureAway = 5 - (cubeValue * multiplier);
            float expectedMwc = MatchEquityTable.GetMatchWinningChance(expectedFutureAway, oppAway);

            Assert.Equal(expectedMwc, result);
        }

        [Fact]
        public void GetMwcIfLost_CalculatesCorrectFutureOpponentAwayScore()
        {
            // Current score: 7-away vs 7-away. Cube is at 1.
            int playerAway = 7;
            int oppAway = 7;
            int cubeValue = 1;

            // Act: Lose a gammon (multiplier 2)
            float result = MatchEquityTable.GetMwcIfLost(playerAway, oppAway, cubeValue, 2);

            // Calculation: Opponent was 7-away, now they are 7 - (1 * 2) = 5-away.
            float expectedMwc = MatchEquityTable.GetMatchWinningChance(7, 5);

            Assert.Equal(expectedMwc, result);
        }
    }
}