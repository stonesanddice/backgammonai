using System.IO;
using Xunit;
using EngineCore;

namespace EngineTests
{
    public class WeightParserTests
    {
        private readonly string _weightsPath;

        public WeightParserTests()
        {
            _weightsPath = Path.Combine(FindDataDirectory(), "gnubg.weights");
        }

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
        public void Load_ValidWeightsFile_ParsesNetworksSuccessfully()
        {
            // Act
            var nets = WeightParser.Load(_weightsPath);

            // Assert
            Assert.NotNull(nets);
            Assert.True(nets.Count >= 2, "The file should contain multiple networks.");

            // FIX: Dynamically find the Contact Network by its known input size!
            // We need System.Linq included at the top of the file for this.
            var contactNet = System.Linq.Enumerable.FirstOrDefault(nets, n => n.InputCount == 250);

            Assert.NotNull(contactNet); // Will fail if it can't find the 250-input net
            Assert.Equal(128, contactNet.HiddenCount);
            Assert.Equal(5, contactNet.OutputCount);

            // Verify a few weights/biases were actually loaded
            Assert.NotEqual(0.0f, contactNet.HiddenBiases[0]);
            Assert.NotEqual(0.0f, contactNet.OutputWeights[0, 0]);
        }
    }
}