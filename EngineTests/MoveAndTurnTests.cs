using Xunit;
using EngineCore;
using System.Collections.Generic;

namespace EngineTests
{
    public class MoveAndTurnTests
    {
        [Theory]
        [InlineData(23, 19, false, "24/20")]   // Standard move (Adjusted for 0-indexing + 1)
        [InlineData(24, 17, true, "Bar/18*")] // Hit from the Bar
        [InlineData(5, -1, false, "6/Off")]    // Bearing off
        public void Move_ToString_ShouldFormatCorrectly(int from, int to, bool isHit, string expected)
        {
            // Arrange
            var move = new Move { From = from, To = to, IsHit = isHit };

            // Act
            var result = move.ToString();

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Turn_ToString_ShouldJoinMultipleMoves()
        {
            // Arrange
            var turn = new Turn();
            turn.Moves.Add(new Move { From = 23, To = 19 }); // 24/20
            turn.Moves.Add(new Move { From = 19, To = 14 }); // 20/15

            // Act
            var result = turn.ToString();

            // Assert
            Assert.Equal("24/20 20/15", result);
        }

        [Fact]
        public void Turn_ShouldTrackDiceUsed()
        {
            // Arrange
            var turn = new Turn();
            
            // Act
            turn.DiceUsed.AddRange(new[] { 6, 6, 6, 6 }); // A "doubles" turn

            // Assert
            Assert.Equal(4, turn.DiceUsed.Count);
            Assert.All(turn.DiceUsed, d => Assert.Equal(6, d));
        }

        [Fact]
        public void Turn_ShouldAllowNullResultingStateInitially()
        {
            // Testing the nullable reference type handling
            var turn = new Turn();
            Assert.Null(turn.ResultingState);
        }
    }
}