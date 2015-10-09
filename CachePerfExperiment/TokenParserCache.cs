using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class TokenParserCache: ITokenParser
    {
        private ITokenParser inner;
        private ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();
        private ConcurrentQueue<Tuple<DateTime, string>> expirationList = new ConcurrentQueue<Tuple<DateTime, string>>();

        private Task cacheReaper;

        private TimeSpan reaperInterval = TimeSpan.FromMilliseconds(Parameters.CacheReaperIntervalMs);
        private TimeSpan cacheExpiration = TimeSpan.FromMilliseconds(Parameters.CacheEntryTtlMs);

        public TokenParserCache(ITokenParser nextParser)
        {
            inner = nextParser;
            cacheReaper = Task.Run(() => CacheReaper());
        }

        public async Task<string> ParseAsync(string token)
        {
            string cacheHit;
            if (cache.TryGetValue(token, out cacheHit))
            {
                return cacheHit;
            }
            string result = await inner.ParseAsync(token);
            cache[token] = result;
            expirationList.Enqueue(Tuple.Create(DateTime.Now + cacheExpiration, token));
            return result;
        }

        private async Task CacheReaper()
        {
            while (true)
            {
                await Task.Delay(reaperInterval);
                Tuple<DateTime, string> expirationEntry;

                while (expirationList.TryPeek(out expirationEntry) &&
                    expirationEntry.Item1 > DateTime.Now)
                {
                    expirationList.TryDequeue(out expirationEntry);
                    string dummy;
                    cache.TryRemove(expirationEntry.Item2, out dummy);
                }
            }
        }
    }
}
