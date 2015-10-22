using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class TokenParserLRUCache : ITokenParser, IDecorator<ITokenParser>
    {
        private class CacheEntry
        {
            public string RawToken;
            public string ParsedToken;
            public DateTime Expiration;
            public CacheEntry Next;
            public CacheEntry Prev;

            public void Detach()
            {
                CacheEntry prevEntry = Prev;
                CacheEntry nextEntry = Next;
                if (prevEntry != null)
                {
                    prevEntry.Next = nextEntry;
                }
                if (nextEntry != null)
                {
                    nextEntry.Prev = prevEntry;
                }
                Next = null;
                Prev = null;
            }

            public void InsertAfter(CacheEntry other)
            {
                CacheEntry nextEntry = other.Next;

                nextEntry.Prev = this;
                Next = nextEntry;
                Prev = other;
                other.Next = this;
            }
        }

        private ConcurrentDictionary<string, CacheEntry> cache = new ConcurrentDictionary<string, CacheEntry>();
        private CacheEntry lruListRoot;

        private object cacheListLock = new object();
        private Channel<bool> hitCounterChannel;

        public TokenParserLRUCache(Channel<bool> hitCounterChannel)
        {
            lruListRoot = new CacheEntry();
            lruListRoot.Next = lruListRoot;
            lruListRoot.Prev = lruListRoot;

            this.hitCounterChannel = hitCounterChannel;
        }

        public ITokenParser Next
        {
            get; private set;
        }

        public async Task<string> ParseAsync(string token)
        {
            CacheEntry entry = Access(token);

            if (entry != null)
            {
                hitCounterChannel.PublishAsync(true);
            }
            else
            {
                entry = new CacheEntry()
                {
                    RawToken = token,
                    ParsedToken = await Next.ParseAsync(token),
                    Expiration = DateTime.Now + TimeSpan.FromMilliseconds(Parameters.CacheEntryTtlMs)
                };

                lock (cacheListLock)
                {
                    entry.InsertAfter(lruListRoot);
                    cache[token] = entry;
                    if (cache.Count > Parameters.CacheMaxEntries)
                    {
                        Scavenge();
                    }
                }
                hitCounterChannel.PublishAsync(false);
            }
            return entry.ParsedToken;
        }

        public void Wrap(ITokenParser inner)
        {
            Next = inner;
        }

        private CacheEntry Access(string rawToken)
        {
            CacheEntry entry = null;
            if (cache.TryGetValue(rawToken, out entry))
            {
                lock(cacheListLock)
                {
                    entry.Detach();
                    entry.InsertAfter(lruListRoot);
                }
            }
            return entry;
        }

        private void Scavenge()
        {
            CacheEntry toRemove = lruListRoot;
            CacheEntry tailEnd = toRemove.Prev;
            tailEnd.Next = null;
            for(int i = 0; i < Parameters.CacheNumToScavenge; ++i)
            {
                toRemove = toRemove.Prev;
            }

            CacheEntry newTail = toRemove.Prev;
            newTail.Next = lruListRoot;
            lruListRoot.Prev = newTail;

            CacheEntry _;
            while(toRemove != null)
            {
                cache.TryRemove(toRemove.RawToken, out _);
                CacheEntry next = toRemove.Next;
                toRemove.Prev = null;
                toRemove.Next = null;
                toRemove = next;
            }
        }
    }
}
