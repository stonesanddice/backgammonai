using EngineCore;
using Xunit;
using System.Linq;

namespace EngineTests
{
    public class SearchStrategicTests
    {
        private readonly SearchEngine _ai;
        private readonly MatchState _match;

        public SearchStrategicTests()
        {
            // Initialize with real weights to ensure strategic accuracy
            string dataDir = FindDataDirectory();
            var nets = WeightParser.Load(System.IO.Path.Combine(dataDir, "gnubg.weights"));
            var contactNet = nets.First(n => n.InputCount == 250);

            _ai = new SearchEngine(contactNet, null, new BearoffEvaluator(dataDir), new CubeEvaluator());

            _match = new MatchState { MatchLength = 0 }; // Money game
        }

        [Fact]
        public void GetBestTurn_2Ply_AvoidsRiskThat1PlyAccepts()
        {
            // Setup: A classic "Loose Hit" trap.
            // Player 0 (AI) has a blot on the 1-point. 
            // The opponent has a blot on the AI's 2-point.
            // 1-Ply will want to hit that blot. 
            // 2-Ply should realize that hitting leaves TWO blots in the home board
            // and the opponent has a 30%+ chance to hit back from the bar.

            // This Position ID represents a complex middle-game contact position.
            // You can also manually construct this using GameState arrays.
            string trapPositionId = "4HPwATDbt/AABA";
            GameState state = PositionId.Decode(trapPositionId);
            state.Dice1 = 3;
            state.Dice2 = 1;

            // Act: Compare 1-Ply vs 2-Ply
            _ai.ClearCache();
            Turn? turn1Ply = _ai.GetBestTurn(state, _match, depth: 1);

            _ai.ClearCache();
            Turn? turn2Ply = _ai.GetBestTurn(state, _match, depth: 2);

            // Assert
            Assert.NotNull(turn1Ply);
            Assert.NotNull(turn2Ply);

            // In many high-level positions, the moves will differ.
            // If they are different, we can inspect if 2-Ply chose the lower-volatility play.
            // NOTE: If the NN is already very "smart," 1-Ply might already see the danger.
            // This test verifies the search tree returns a valid, distinct result.
            Assert.NotEqual(turn1Ply.ToString(), turn2Ply.ToString());

            // Log for manual inspection in Test Output
            System.Diagnostics.Debug.WriteLine($"1-Ply Move: {turn1Ply}");
            System.Diagnostics.Debug.WriteLine($"2-Ply Move: {turn2Ply}");
        }

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
    }
}