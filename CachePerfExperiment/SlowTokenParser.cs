using System;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class SlowTokenParser : ITokenParser
    {
        private Random rng = new Random();

        public async Task<string> ParseAsync(string token)
        {
            int delay = Parameters.TokenParsingBaseDelayMs;
            lock (rng)
            {
                delay += rng.Next(-Parameters.TokenParsingDelayVarianceMs, 
                    Parameters.TokenParsingDelayVarianceMs + 1);
            }

            await Task.Delay(delay);
            return "It was parsed";
        }
    }
}
