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

        [Fact]
        public void GetCheckers_ReflectsBoardArray()
        {
            var state = new GameState();
            // Board[player, point] uses 0-based point index (0-23 = points 1-24, 24 = bar)
            state.Board[0, 0] = 3; // Player 0, point 1
            state.Board[0, 23] = 2; // Player 0, point 24

            Assert.Equal(3, state.GetCheckers(0, 1));
            Assert.Equal(2, state.GetCheckers(0, 24));
        }

        [Fact]
        public void GetCheckersOnBar_ReflectsBoardSlot24()
        {
            var state = new GameState();
            state.Board[0, 24] = 2;
            state.Board[1, 24] = 1;

            Assert.Equal(2, state.GetCheckersOnBar(0));
            Assert.Equal(1, state.GetCheckersOnBar(1));
        }

        [Fact]
        public void GetCheckersOffBoard_CountsCorrectly()
        {
            var state = new GameState();
            // 15 total; put 10 on board/bar, 5 off
            state.Board[0, 0] = 5;
            state.Board[0, 5] = 5;
            state.Board[0, 24] = 0;

            Assert.Equal(5, state.GetCheckersOffBoard(0));
        }

        [Fact]
        public void GetCheckersOffBoard_AllOnBoard_ReturnsZero()
        {
            var state = new GameState();
            for (int i = 0; i < 25; i++) state.Board[0, i] = 0;
            state.Board[0, 5] = 15;

            Assert.Equal(0, state.GetCheckersOffBoard(0));
        }
    }
}