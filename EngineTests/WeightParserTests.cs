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
            var currentDir = new DirectoryInfo(System.AppContext.BaseDirectory);
            while (currentDir != null)
            {
                string potential = Path.Combine(currentDir.FullName, "Data");
                if (Directory.Exists(potential) && File.Exists(Path.Combine(potential, "gnubg.weights")))
                {
                    return potential;
                }
                currentDir = currentDir.Parent;
            }
            throw new DirectoryNotFoundException("Could not find Data folder containing gnubg.weights");
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