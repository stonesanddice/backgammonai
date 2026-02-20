using EngineCore;
using Xunit;

namespace EngineTests
{
    public class BoardClassifierTests
    {
        private readonly BoardClassifier _classifier = new();

        // Ensure this path maps back to your root Data folder from the bin output directory!
        // Replace the old _dataDir string with this:
        private readonly string _dataDir = FindDataDirectory();

        private static string FindDataDirectory()
        {
            // Start at the directory where the test DLL is running
            var currentDir = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory);

            // Search upward until we find a directory containing the "Data" folder
            while (currentDir != null)
            {
                string potentialDataDir = System.IO.Path.Combine(currentDir.FullName, "Data");
                // Check if the directory exists AND contains our specific file
                if (System.IO.Directory.Exists(potentialDataDir) &&
                    System.IO.File.Exists(System.IO.Path.Combine(potentialDataDir, "gnubg_ts0.bd")))
                {
                    return potentialDataDir;
                }
                currentDir = currentDir.Parent; // Move up one folder
            }

            throw new System.IO.DirectoryNotFoundException("Could not find the 'Data' directory containing gnubg_ts0.bd.");
        }

        [Fact]
        public void Classify_StartingPosition_IsContact()
        {
            var state = new GameState();
            // Manually set up the starting board to test the classifier in isolation
            state.Player1Checkers[5] = 5; state.Player1Checkers[7] = 3;
            state.Player1Checkers[12] = 5; state.Player1Checkers[23] = 2;

            state.Player2Checkers[5] = 5; state.Player2Checkers[7] = 3;
            state.Player2Checkers[12] = 5; state.Player2Checkers[23] = 2;

            Assert.Equal(PositionClass.Contact, _classifier.Classify(state));
        }

        [Fact]
        public void Classify_DeepRace_IsRace()
        {
            var state = new GameState();
            state.Player1Checkers[9] = 15;
            state.Player2Checkers[9] = 15;

            Assert.Equal(PositionClass.Race, _classifier.Classify(state));
        }

        [Fact]
        public void Classify_CrunchedBoard_IsCrashed()
        {
            var state = new GameState();

            state.Player2Checkers[0] = 12;
            state.Player2Checkers[1] = 3;

            // FIX: Move Player 1 to the Bar/24-point so they overlap and contact is still possible!
            state.Player1Checkers[24] = 15;

            Assert.Equal(PositionClass.Crashed, _classifier.Classify(state));
        }

        [Fact]
        public void Evaluate_ValidTwoSidedPosition_ReturnsCorrectWinProbability()
        {
            var evaluator = new BearoffEvaluator(_dataDir);

            // FIX: Let the classifier generate the true IDs for "1 checker on the 1-point"
            var p1Board = new int[25]; p1Board[0] = 1;
            var p2Board = new int[25]; p2Board[0] = 1;

            uint actualId1 = _classifier.GetPositionBearoff(p1Board);
            uint actualId2 = _classifier.GetPositionBearoff(p2Board);

            var probs = evaluator.Evaluate(actualId1, actualId2, PositionClass.BearoffTwoSided);

            Assert.NotNull(probs);
            Assert.Equal(5, probs.Length);

            // The Win probability MUST be 100% since P1 is on roll.
            Assert.True(probs[0] > 0.999f, $"Expected ~1.0, but got {probs[0]}");
        }
    }
}