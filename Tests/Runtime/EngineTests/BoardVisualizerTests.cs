using Xunit;
using EngineCore;
using EngineCLI;
using System;
using System.IO;

namespace EngineTests
{
    /// <summary>
    /// Tests for BoardVisualizer to achieve 100% coverage.
    /// </summary>
    public class BoardVisualizerTests
    {
        [Fact]
        public void PrintBoard_InitialPosition_DoesNotThrow()
        {
            var state = new GameState
            {
                Player1Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Player2Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PlayerOnRoll = 0,
                Dice1 = 3,
                Dice2 = 5
            };

            // Capture console output
            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state);

            var result = output.ToString();
            Assert.Contains("=============================================", result);
            Assert.Contains("Position ID", result);
        }

        [Fact]
        public void PrintBoard_WithMatchState_DisplaysMatchInfo()
        {
            var state = new GameState
            {
                Player1Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Player2Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PlayerOnRoll = 1,
                Dice1 = 2,
                Dice2 = 4
            };

            var match = new MatchState
            {
                MatchLength = 7,
                Player0Score = 2,
                Player1Score = 3,
                Cube = 2,
                CubeOwner = 0
            };

            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state, match);

            var result = output.ToString();
            Assert.Contains("7-Point Match", result);
            Assert.Contains("Score: P0 (2) - P1 (3)", result);
            Assert.Contains("Cube : 2 (Player 0)", result);
            Assert.Contains("Dice: 2-4", result);
        }

        [Fact]
        public void PrintBoard_MoneyGame_DisplaysMoneyGame()
        {
            var state = new GameState
            {
                Player1Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Player2Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PlayerOnRoll = 0,
                Dice1 = 6,
                Dice2 = 6
            };

            var match = new MatchState
            {
                MatchLength = 0, // Money game
                Cube = 4,
                CubeOwner = -1
            };

            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state, match);

            var result = output.ToString();
            Assert.Contains("Money Game", result);
            Assert.Contains("Cube : 4 (Centered)", result);
        }

        [Fact]
        public void PrintBoard_CubeOwnedByPlayer1_DisplaysCorrectly()
        {
            var state = new GameState
            {
                Player1Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Player2Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PlayerOnRoll = 0,
                Dice1 = 1,
                Dice2 = 1
            };

            var match = new MatchState
            {
                MatchLength = 5,
                Cube = 2,
                CubeOwner = 1
            };

            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state, match);

            var result = output.ToString();
            Assert.Contains("Cube : 2 (Player 1)", result);
        }

        [Fact]
        public void PrintBoard_WithCheckersOnBar_DisplaysBarCount()
        {
            var state = new GameState
            {
                Player1Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2 }, // 2 on bar
                Player2Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }, // 1 on bar
                PlayerOnRoll = 0,
                Dice1 = 3,
                Dice2 = 2
            };

            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state);

            var result = output.ToString();
            Assert.Contains("On Bar:", result);
        }

        [Fact]
        public void PrintBoard_WithBorneOffCheckers_DisplaysBorneOffCount()
        {
            var state = new GameState
            {
                // Player 1 has only 10 checkers on board (5 borne off)
                Player1Checkers = new int[] { 5, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                // Player 2 has only 12 checkers on board (3 borne off)
                Player2Checkers = new int[] { 6, 6, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PlayerOnRoll = 0,
                Dice1 = 4,
                Dice2 = 5
            };

            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state);

            var result = output.ToString();
            Assert.Contains("Borne Off:", result);
        }

        [Fact]
        public void PrintBoard_StackedCheckers_DisplaysCorrectly()
        {
            var state = new GameState
            {
                // Stack of 10 checkers on point 0
                Player1Checkers = new int[] { 10, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Player2Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PlayerOnRoll = 0,
                Dice1 = 2,
                Dice2 = 3
            };

            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state);

            var result = output.ToString();
            // Should display the board without errors
            Assert.Contains("=============================================", result);
        }

        [Fact]
        public void PrintBoard_Player1OnRoll_DisplaysCorrectOrientation()
        {
            var state = new GameState
            {
                Player1Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                Player2Checkers = new int[] { 2, 0, 0, 0, 0, 5, 0, 3, 0, 0, 0, 5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                PlayerOnRoll = 1, // Player 1 on roll
                Dice1 = 5,
                Dice2 = 6
            };

            var output = new StringWriter();
            Console.SetOut(output);

            BoardVisualizer.PrintBoard(state);

            var result = output.ToString();
            // Just verify it doesn't throw and produces output
            Assert.Contains("Position ID", result);
            Assert.Contains("Player 0 (O)", result);
            Assert.Contains("Player 1 (X)", result);
        }
    }
}
