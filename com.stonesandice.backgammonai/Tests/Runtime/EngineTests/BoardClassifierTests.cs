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
            var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
            while (currentDir != null)
            {
                // 1. Check the new nested Unity Package path
                string packageData = Path.Combine(currentDir.FullName, "com.stonesandice.backgammonai", "Runtime", "Data");
                if (Directory.Exists(packageData)) return packageData;

                // 2. Check the old root path (for GitHub Actions/Local fallbacks)
                string rootData = Path.Combine(currentDir.FullName, "Data");
                if (Directory.Exists(rootData)) return rootData;

                currentDir = currentDir.Parent;
            }
            throw new DirectoryNotFoundException("Could not find the Data directory in any parent folders.");
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

        [Fact]
        public void Classify_GameOver_IsOver()
        {
            var state = new GameState();
            state.Player1Checkers[0] = 15;  // P1 all on 1-point (e.g. about to bear off)
            state.Player2Checkers = new int[25]; // P2 has no checkers (borne off)

            Assert.Equal(PositionClass.Over, _classifier.Classify(state));
        }

        [Fact]
        public void GetPositionBearoff_AllOnOnePoint_ReturnsConsistentId()
        {
            var board = new int[25];
            board[0] = 15;
            uint id1 = _classifier.GetPositionBearoff(board);
            uint id2 = _classifier.GetPositionBearoff(board);
            Assert.Equal(id1, id2);
        }

        [Fact]
        public void GetPositionBearoff_DifferentPlacements_ReturnDifferentIds()
        {
            var boardA = new int[25]; boardA[0] = 15;
            var boardB = new int[25]; boardB[1] = 15;
            uint idA = _classifier.GetPositionBearoff(boardA);
            uint idB = _classifier.GetPositionBearoff(boardB);
            Assert.NotEqual(idA, idB);
        }
    }
}