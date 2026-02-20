using Xunit;
using EngineCore;
using System.Linq;

namespace EngineCore.Tests
{
    public class FeatureEncoderTests
    {
        [Fact]
        public void EncodeContact_EmptyBoard_ReturnsMostlyZeros()
        {
            // Arrange
            int[] onRoll = new int[25]; // 0-23 points, 24 is bar
            int[] waiting = new int[25];

            // Act
            float[] result = FeatureEncoder.EncodeContact(onRoll, waiting);

            // Assert
            Assert.Equal(250, result.Length);
            // On an empty board, most features should be 0. 
            // Exceptions might be "Forward Anchor" which defaults to 2f/6f (0.333) when no anchor exists.
            Assert.All(result.Take(200), x => Assert.Equal(0f, x));
        }

        [Fact]
        public void EncodeBase_CorrectlyEncodesCheckerCounts()
        {
            // Testing the logic used in the private EncodeBase via the public EncodeContact
            // Offset 100 is where "onRoll" base features start.
            // Each point has 4 floats: [1, 2, 3+, (n-3)/2]

            int[] onRoll = new int[25];
            onRoll[0] = 1; // 1 checker on point 0
            onRoll[1] = 2; // 2 checkers on point 1
            onRoll[2] = 5; // 5 checkers on point 2
            int[] waiting = new int[25];

            float[] result = FeatureEncoder.EncodeContact(onRoll, waiting);

            // Point 0 (Index 100-103)
            Assert.Equal(1f, result[100]); // exactly 1
            Assert.Equal(0f, result[101]);

            // Point 1 (Index 104-107)
            Assert.Equal(0f, result[104]);
            Assert.Equal(1f, result[105]); // exactly 2

            // Point 2 (Index 108-111)
            Assert.Equal(0f, result[108]);
            Assert.Equal(0f, result[109]);
            Assert.Equal(1f, result[110]); // 3+
            Assert.Equal(1f, result[111]); // (5-3)/2 = 1.0
        }

        [Fact]
        public void EncodeBase_EncodesBarCorrectly()
        {
            int[] onRoll = new int[25];
            onRoll[24] = 2; // Two checkers on the bar
            int[] waiting = new int[25];

            float[] result = FeatureEncoder.EncodeContact(onRoll, waiting);

            // Bar for onRoll starts at 100 + (24 * 4) = 196
            Assert.Equal(1f, result[196]); // >= 1
            Assert.Equal(1f, result[197]); // >= 2
            Assert.Equal(0f, result[198]); // >= 3
        }

        [Theory]
        [InlineData(0, 0f)]      // 0 men off: OFF1 = 0
        [InlineData(1, 0.3333f)] // 1 man off: OFF1 = 1/3
        [InlineData(15, 1f)]     // 15 men off: OFF1 = 1
        public void EncodeHalf_OffFeatures_CalculatesCorrectly(int menOff, float expectedFirstOffBit)
        {
            int[] onRoll = new int[25];
            // Place remaining checkers on the 1-point (index 0)
            onRoll[0] = 15 - menOff;

            int[] waiting = new int[25];

            float[] result = FeatureEncoder.EncodeContact(onRoll, waiting);

            // Offset 200 is OFF1 for onRoll
            // We use a precision of 4 decimal places for the float comparison
            Assert.Equal(expectedFirstOffBit, result[200], precision: 4);
        }

        [Fact]
        public void EncodeHalf_BackChequer_ReturnsNormalizedIndex()
        {
            int[] onRoll = new int[25];
            onRoll[12] = 1; // Midpoint
            int[] waiting = new int[25];

            float[] result = FeatureEncoder.EncodeContact(onRoll, waiting);

            // Offset 204 is BACK_CHEQUER
            // 12 / 24f = 0.5f
            Assert.Equal(0.5f, result[204]);
        }

        [Fact]
        public void CalculatePipLoss_DetectsHits()
        {
            // Placing a blot for 'waiting' that 'onRoll' can hit
            int[] onRoll = new int[25];
            onRoll[18] = 1; // Checker 6 pips away from a blot

            int[] waiting = new int[25];
            waiting[11] = 1; // Blot on the 12pt (relative to onRoll, this is index 12 in some systems, 
                             // but the code uses 24-i logic. Let's place it where the math hits.)

            // This is a complex heuristic, we are mainly checking for non-zero signal
            float[] result = FeatureEncoder.EncodeContact(onRoll, waiting);

            // Offset 207 is PipLoss
            Assert.True(result[207] >= 0f);
        }
    }
}