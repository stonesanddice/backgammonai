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
        public void Evaluate_OneSidedPosition_WhenOsNotLoaded_ReturnsNull()
        {
            string emptyDir = Path.Combine(Path.GetTempPath(), "BearoffEvaluatorTests_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(emptyDir);
                var evaluator = new BearoffEvaluator(emptyDir);
                var probs = evaluator.Evaluate(1000, 1, PositionClass.BearoffOneSided);
                Assert.Null(probs);
            }
            finally
            {
                if (Directory.Exists(emptyDir))
                    Directory.Delete(emptyDir);
            }
        }

        [Fact]
        public void Evaluate_OneSidedPosition_WhenOsLoaded_ReturnsValidProbabilities()
        {
            var evaluator = new BearoffEvaluator(_dataDir);
            if (!evaluator.IsOneSidedLoaded)
            {
                return; // OS database not present in this environment
            }
            // Both players same bearoff position (ID 1 = one checker on 1-point). Result should be valid probabilities.
            var probs = evaluator.Evaluate(1, 1, PositionClass.BearoffOneSided);
            Assert.NotNull(probs);
            Assert.Equal(5, probs.Length);
            Assert.True(probs[0] >= 0f && probs[0] <= 1f, $"Win prob should be in [0,1], got {probs[0]}");
            Assert.Equal(0f, probs[1]);
            Assert.Equal(0f, probs[2]);
            Assert.Equal(0f, probs[3]);
            Assert.Equal(0f, probs[4]);
            // Symmetric position (1,1): on roll has slight edge (moves first), so win prob should be >= 0.5
            Assert.True(probs[0] >= 0.5f, $"Symmetric bearoff (1,1): on roll should have at least 50% win, got {probs[0]}");
        }

        [Fact]
        public void WhenTwoSidedDbExists_IsTwoSidedLoadedIsTrue()
        {
            // _dataDir is the directory used by other tests; it must contain gnubg_ts0.bd for two-sided tests to pass
            var evaluator = new BearoffEvaluator(_dataDir);
            Assert.True(evaluator.IsTwoSidedLoaded, "Two-sided bearoff database (gnubg_ts0.bd) should be loaded when present in data directory.");
        }

        [Fact]
        public void WhenOneSidedDbFileExists_IsOneSidedLoadedIsTrue()
        {
            string osPath = Path.Combine(_dataDir, "gnubg_os0.bd");
            if (!File.Exists(osPath))
            {
                // File not present in this environment (e.g. CI without the asset); skip the assertion
                return;
            }
            var evaluator = new BearoffEvaluator(_dataDir);
            Assert.True(evaluator.IsOneSidedLoaded, "One-sided bearoff database (gnubg_os0.bd) should be loaded when the file exists.");
        }

        [Fact]
        public void WhenDataDirHasNoBearoffFiles_NeitherDatabaseIsLoaded()
        {
            string emptyDir = Path.Combine(Path.GetTempPath(), "BearoffEvaluatorTests_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(emptyDir);
                var evaluator = new BearoffEvaluator(emptyDir);
                Assert.False(evaluator.IsTwoSidedLoaded);
                Assert.False(evaluator.IsOneSidedLoaded);
            }
            finally
            {
                if (Directory.Exists(emptyDir))
                    Directory.Delete(emptyDir);
            }
        }

        [Fact]
        public void CalculateOneSidedWinProb_WhenOnRollAlwaysFaster_ReturnsOne()
        {
            var evaluator = new BearoffEvaluator(_dataDir);
            // On roll always finishes in 1 step (prob 1); opponent always in 2 steps (prob 1)
            float[] onRollDist = { 0f, 1f };
            float[] waitingDist = { 0f, 0f, 1f };
            float winProb = evaluator.CalculateOneSidedWinProb(onRollDist, waitingDist);
            Assert.True(winProb > 0.99f, $"Expected ~1.0, got {winProb}");
        }

        [Fact]
        public void CalculateOneSidedWinProb_WhenOpponentAlwaysFaster_ReturnsZero()
        {
            var evaluator = new BearoffEvaluator(_dataDir);
            float[] onRollDist = { 0f, 0f, 1f };
            float[] waitingDist = { 0f, 1f };
            float winProb = evaluator.CalculateOneSidedWinProb(onRollDist, waitingDist);
            Assert.True(winProb < 0.01f, $"Expected ~0.0, got {winProb}");
        }
    }
}