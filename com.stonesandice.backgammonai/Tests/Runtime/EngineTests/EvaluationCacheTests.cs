using System.Diagnostics;
using EngineCore;
using Xunit;

namespace EngineTests
{
    public class EvaluationCacheTests
    {
        [Fact]
        public void Cache_StoreAndRetrieve_ReturnsCorrectProbabilities()
        {
            // Arrange
            var cache = new EvaluationCache(100);
            string posId = "4HPwATDgc/ABMA"; // Standard starting position ID
            float[] expectedProbs = { 0.5f, 0.1f, 0.01f, 0.1f, 0.01f };

            // Act
            cache.Store(posId, expectedProbs);
            bool found = cache.TryGet(posId, out float[]? retrievedProbs);

            // Assert
            Assert.True(found);
            Assert.NotNull(retrievedProbs);
            Assert.Equal(expectedProbs, retrievedProbs);
        }

        [Fact]
        public void Cache_MaxSize_ClearsWhenFull()
        {
            // Arrange: Create a tiny cache that only holds 2 items
            var cache = new EvaluationCache(2);
            float[] dummyProbs = { 0.5f, 0.0f, 0.0f, 0.0f, 0.0f };

            // Act: Add 3 items
            cache.Store("ID_1", dummyProbs);
            cache.Store("ID_2", dummyProbs);
            cache.Store("ID_3", dummyProbs); // This should trigger a Clear()

            // Assert: The first item should be gone because the cache reset
            bool foundOld = cache.TryGet("ID_1", out _);
            bool foundNew = cache.TryGet("ID_3", out _);

            Assert.False(foundOld, "Cache should have cleared after reaching max size.");
            Assert.True(foundNew, "Latest item should still be present in the new cache cycle.");
        }

        [Fact]
        public void Cache_Performance_LookupIsDrasticallyFasterThanFirstCalc()
        {
            // Arrange
            var cache = new EvaluationCache(1000);
            string posId = "TestPosition123";
            float[] probs = { 0.72f, 0.15f, 0.02f, 0.10f, 0.01f };

            // Warm up the CPU and the dictionary structures
            cache.Store(posId, probs);
            for (int i = 0; i < 100; i++) cache.TryGet(posId, out _);

            // Act: Measure the time for 100,000 lookups
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                cache.TryGet(posId, out _);
            }
            sw.Stop();

            // Assert: 100,000 lookups should happen in just a few milliseconds.
            // Even on slow hardware, a dictionary lookup takes nanoseconds.
            // If this is taking more than 50ms, something is wrong with the hashing!
            Assert.True(sw.ElapsedMilliseconds < 50, $"Lookup took too long: {sw.ElapsedMilliseconds}ms");

            // Informational output for your console
            Debug.WriteLine($"100k Cache Lookups took: {sw.Elapsed.TotalMilliseconds:F4}ms");
        }
    }
}