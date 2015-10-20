namespace CachePerfExperiment
{
    /// <summary>
    /// Various constant simulation parameters, collected here so
    /// they're in one spot for easier tweaking.
    /// </summary>
    public static class Parameters
    {
        public const int NumRequestProcessors = 20;

        // Request generation
        public const int NumDistinctTokens = 10000;
        public const int TokenLengthBytes = 2250;
        public const int RunLengthMs = 10*60*1000;
        public const int RequestsPerSecond = 100;
        public const int NumHotEntries = 100;
        public const int HotEntryInterval = 5;

        public const int RequestGenerationBaseDelayMs = 5;
        public const int RequestGenerationDelayVarianceMs = 0;

        // "real" token parsing
        public const int TokenParsingBaseDelayMs = 50;
        public const int TokenParsingDelayVarianceMs = 15;

        // Token caching parameters
        public const int CacheReaperIntervalMs = 5 * 1000;
        public const int CacheEntryTtlMs = 5 * 60 * 1000;
        public const int CacheMaxEntries = 10000;
        public const int CacheNumToScavenge = 50;
    }
}
