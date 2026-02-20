using System.Collections.Generic;

namespace EngineCore
{
    public class EvaluationCache
    {
        // Stores the 5 base probabilities: [Win, WinG, WinBG, LoseG, LoseBG]
        private readonly Dictionary<string, float[]> _cache;
        private readonly int _maxSize;

        public EvaluationCache(int maxSize = 500000)
        {
            _maxSize = maxSize;
            // Pre-allocate capacity to avoid expensive resizing during deep searches
            _cache = new Dictionary<string, float[]>(maxSize);
        }

        public bool TryGet(string positionId, out float[]? probabilities)
        {
            return _cache.TryGetValue(positionId, out probabilities);
        }

        public void Store(string positionId, float[] probabilities)
        {
            // If the cache gets too full during a massive search, clear it.
            // (Clearing is drastically faster than implementing a complex Least-Recently-Used removal algorithm)
            if (_cache.Count >= _maxSize)
            {
                _cache.Clear();
            }

            _cache[positionId] = probabilities;
        }

        public void Clear()
        {
            _cache.Clear();
        }
    }
}