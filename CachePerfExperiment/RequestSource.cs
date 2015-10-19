using System;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    /// <summary>
    /// Class that generates "traffic" to feed into request processors.
    /// Creates a pool of random tokens, then reuses them for the configured
    /// amount of time.
    /// </summary>
    class RequestSource : IRunnable
    {
        private Random rng = new Random();
        private string[] tokens = new string[Parameters.NumDistinctTokens];

        private Channel<string> requestChannel;

        public RequestSource(Channel<String> requestChannel)
        {
            this.requestChannel = requestChannel;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Request source starting up");
            CreateTokens();
            int hotIndex = 0;
            int maxRequests = (int)(Parameters.RunLengthMs*(Parameters.RequestsPerSecond/1000.0));
            for(int requestCount = 0; requestCount < maxRequests; ++requestCount)
            {
                int index;
                if (requestCount % Parameters.HotEntryInterval == 0)
                {
                    index = hotIndex;
                    hotIndex = (hotIndex + 1) % Parameters.NumHotEntries;
                }
                else
                {
                    index = rng.Next(tokens.Length);
                }
                await requestChannel.PublishAsync(tokens[index]);
                // Delay generation a bit just to keep from blowing out memory in queues.
                //await RandomDelay();
            }
            Console.WriteLine("Request source finished, closing channel and exiting");
            requestChannel.Close();
        }

        private void CreateTokens()
        {
            byte[] binaryToken = new byte[Parameters.TokenLengthBytes];
            for (int i = 0; i < Parameters.NumDistinctTokens; ++i)
            {
                rng.NextBytes(binaryToken);
                tokens[i] = Convert.ToBase64String(binaryToken);
            }
        }

        private async Task RandomDelay()
        {
            int delayVariance = Parameters.RequestGenerationDelayVarianceMs;
            var delayDelta = delayVariance == 0 ? 0 : rng.Next(-delayVariance, delayVariance + 1);
            await Task.Delay(Parameters.RequestGenerationBaseDelayMs + delayDelta);
        }
    }
}
