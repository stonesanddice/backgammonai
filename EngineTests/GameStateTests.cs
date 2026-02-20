using Xunit;
using EngineCore;

namespace EngineTests
{
    public class GameStateTests
    {
        [Fact]
        public void InitialState_ArraysShouldBeCorrectSize()
        {
            // Arrange
            var state = new GameState();

            // Assert - 25 slots (0-23 for points, 24 for the bar/off-board)
            Assert.NotNull(state.Player1Checkers);
            Assert.Equal(25, state.Player1Checkers.Length);
            
            Assert.NotNull(state.Player2Checkers);
            Assert.Equal(25, state.Player2Checkers.Length);
        }

        [Fact]
        public void InitialState_CheckersShouldBeEmpty()
        {
            var state = new GameState();

            // Verify all 25 slots are 0 by default
            foreach (var count in state.Player1Checkers)
            {
                Assert.Equal(0, count);
            }
        }

        [Fact]
        public void Dice_ShouldStoreValuesCorrectly()
        {
            // Arrange
            var state = new GameState();

            // Act
            state.Dice1 = 6;
            state.Dice2 = 2;

            // Assert
            Assert.Equal(6, state.Dice1);
            Assert.Equal(2, state.Dice2);
        }

        [Fact]
        public void MatchState_ShouldUpdateCubeAndScores()
        {
            // Arrange
            var state = new GameState();

            // Act
            state.CubeValue = 2;
            state.CubeOwner = 0; // Player 1 owns the cube
            state.Player1Score = 5;
            state.Player2Score = 3;
            state.MatchLength = 11;

            // Assert
            Assert.Equal(2, state.CubeValue);
            Assert.Equal(0, state.CubeOwner);
            Assert.Equal(5, state.Player1Score);
            Assert.Equal(3, state.Player2Score);
            Assert.Equal(11, state.MatchLength);
        }

        [Theory]
        [InlineData(0, 5)] // Point 0, 5 checkers
        [InlineData(24, 2)] // Point 24, 2 checkers
        public void BoardState_ShouldAllowSpecificCheckerPlacements(int index, int count)
        {
            // Arrange
            var state = new GameState();

            // Act
            state.Player1Checkers[index] = count;

            // Assert
            Assert.Equal(count, state.Player1Checkers[index]);
        }
    }
}