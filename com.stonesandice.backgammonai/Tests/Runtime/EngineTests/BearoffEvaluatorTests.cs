using System;
using System.IO;
using Xunit;
using EngineCore;

namespace EngineCore.Tests
{
    public class BearoffEvaluatorTests
    {
        // FIX: Dynamically path up from the bin/Debug/net7.0/ directory back to the root Data folder!
        private readonly string _dataDir = FindDataDirectory();

        private static string FindDataDirectory()
        {
            var currentDir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);
            while (currentDir != null)
            {
                // Look for the specific UPM package data path
                string potential = System.IO.Path.Combine(currentDir.FullName, "com.stonesandice.backgammonai", "Runtime", "Data");
                if (System.IO.Directory.Exists(potential)) return potential;

                // Fallback for standard root Data folder
                string rootData = System.IO.Path.Combine(currentDir.FullName, "Data");
                if (System.IO.Directory.Exists(rootData)) return rootData;

                currentDir = currentDir.Parent;
            }
            throw new System.IO.DirectoryNotFoundException("Data folder not found in any parent directories.");
        }

        [Fact]
        public void Evaluate_ValidTwoSidedPosition_ReturnsCorrectWinProbability()
        {
            var evaluator = new BearoffEvaluator(_dataDir);

            // Act: Player 1 has ID 1 (one checker on the 1-point)
            //      Player 2 has ID 1 (one checker on the 1-point)
            var probs = evaluator.Evaluate(1, 1, PositionClass.BearoffTwoSided);

            // Assert
            Assert.NotNull(probs); // If this passes, the database successfully loaded!

            Assert.Equal(5, probs.Length);
            Assert.Equal(0.0f, probs[1]);
            Assert.Equal(0.0f, probs[2]);
            Assert.Equal(0.0f, probs[3]);
            Assert.Equal(0.0f, probs[4]);

            // The big test: Win probability MUST be 100%.
            Assert.True(probs[0] > 0.999f, $"Expected ~1.0, but got {probs[0]}");
        }

        [Fact]
        public void Evaluate_OneSidedPosition_SafelyReturnsNull()
        {
            var evaluator = new BearoffEvaluator(_dataDir);
            var probs = evaluator.Evaluate(1000, 1, PositionClass.BearoffOneSided);
            Assert.Null(probs);
        }
    }
}