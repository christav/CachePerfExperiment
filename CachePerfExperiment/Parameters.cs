namespace CachePerfExperiment
{
    /// <summary>
    /// Various constant simulation parameters, collected here so
    /// they're in one spot for easier tweaking.
    /// </summary>
    public static class Parameters
    {
        public const int NumRequestProcessors = 10;

        // Request generation
        public const int NumDistinctTokens = 100;
        public const int TokenLengthBytes = 2250;
        public const int RunLengthMs = 1*60*1000; // 1 minute in milliseconds

        public const int RequestGenerationBaseDelayMs = 5;
        public const int RequestGenerationDelayVarianceMs = 3;

        // "real" token parsing
        public const int TokenParsingBaseDelayMs = 50;
        public const int TokenParsingDelayVarianceMs = 15;

        // Token caching parameters
        public const int CacheReaperIntervalMs = 5 * 1000;
        public const int CacheEntryTtlMs = 10 * 1000;

    }
}
