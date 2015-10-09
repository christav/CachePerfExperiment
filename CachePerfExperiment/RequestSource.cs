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
            DateTime endTime = DateTime.Now + TimeSpan.FromMilliseconds(Parameters.RunLengthMs);
            while (DateTime.Now < endTime)
            {
                var index = rng.Next(tokens.Length);
                requestChannel.Publish(tokens[index]);
                await RandomDelay();
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
            var delayDelta = rng.Next(-Parameters.RequestGenerationDelayVarianceMs, Parameters.RequestGenerationDelayVarianceMs + 1);
            await Task.Delay(Parameters.RequestGenerationBaseDelayMs + delayDelta);
        }
    }
}
