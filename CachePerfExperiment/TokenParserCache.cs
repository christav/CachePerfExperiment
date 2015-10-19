using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class TokenParserCache: ITokenParser, IDecorator<ITokenParser>
    {
        private ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();
        private ConcurrentQueue<Tuple<DateTime, string>> expirationList = new ConcurrentQueue<Tuple<DateTime, string>>();

        private Task cacheReaper;

        private TimeSpan reaperInterval = TimeSpan.FromMilliseconds(Parameters.CacheReaperIntervalMs);
        private TimeSpan cacheExpiration = TimeSpan.FromMilliseconds(Parameters.CacheEntryTtlMs);

        private Channel<bool> hitCounterChannel;
 
        public TokenParserCache(Channel<bool> hitCounterChannel)
        {
            this.hitCounterChannel = hitCounterChannel;
            cacheReaper = Task.Run(() => CacheReaper());
        }

        public async Task<string> ParseAsync(string token)
        {
            string cacheHit;
            if (cache.TryGetValue(token, out cacheHit))
            {
                hitCounterChannel.PublishAsync(true);
                return cacheHit;
            }
            string result = await Next.ParseAsync(token);
            cache[token] = result;
            expirationList.Enqueue(Tuple.Create(DateTime.Now + cacheExpiration, token));
            hitCounterChannel.PublishAsync(false);
            return result;
        }

        public ITokenParser Next { get; private set; }

        public void Wrap(ITokenParser inner)
        {
            Next  = inner;
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
