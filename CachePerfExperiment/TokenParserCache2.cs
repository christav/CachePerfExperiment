using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class TokenParserCache2 : ITokenParser, IDecorator<ITokenParser>
    {
        private ConcurrentDictionary<string, Tuple<string, DateTime>> cache = 
            new ConcurrentDictionary<string, Tuple<string, DateTime>>();

        private Random rng = new Random();

        private Channel<bool> hitCounterChannel;

        public TokenParserCache2(Channel<bool> hitCounterChannel)
        {
            this.hitCounterChannel = hitCounterChannel;
        }

        public async Task<string> ParseAsync(string token)
        {
            Tuple<string, DateTime> cacheEntry;
            if (cache.TryGetValue(token, out cacheEntry))
            {
                if (cacheEntry.Item2 < DateTime.Now)
                {
                    Scavenge();
                    hitCounterChannel.PublishAsync(true);
                    return cacheEntry.Item1;
                }
            }

            Scavenge();
            string result = await Next.ParseAsync(token);
            var newValue = Tuple.Create(result, DateTime.Now + TimeSpan.FromMilliseconds(Parameters.CacheEntryTtlMs));

            cache.AddOrUpdate(token, newValue, (k, v) => newValue);
            hitCounterChannel.PublishAsync(false);
            return result;
        }

        private void Scavenge()
        {
            var keys = cache.Keys.ToArray();
            if (keys.Length >= Parameters.CacheMaxEntries)
            {
                List<string> indexes;
                lock (rng)
                {
                    indexes = Enumerable.Range(0, Parameters.CacheNumToScavenge)
                        .Select(n => keys[rng.Next(keys.Length)])
                        .ToList();
                }
                indexes.ForEach(i => {
                    Tuple<string, DateTime> _;
                    cache.TryRemove(i, out _);
                });
            }
        }

        public ITokenParser Next { get; private set; }
        public void Wrap(ITokenParser inner)
        {
            Next = inner;
        }
    }
}
