using System;
using System.Threading;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class HitCounter : IRunnable
    {
        private Channel<bool> hitChannel;
        private int totalTries = 0;
        private int totalHits = 0;

        public HitCounter(Channel<bool> hitChannel)
        {
            this.hitChannel = hitChannel;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Hit counter starting up");
            var hitMessage = await hitChannel.ReceiveAsync();
            while (!hitMessage.IsClosed)
            {
                Interlocked.Increment(ref totalTries);
                if (hitMessage.Data)
                {
                    Interlocked.Increment(ref totalHits);
                }
                hitMessage = await hitChannel.ReceiveAsync();
            }

            Console.WriteLine("Hit counter shutting down, channel closed");
        }

        public int TotalHits { get { return totalHits; } }
        public int TotalRequests { get { return totalTries; } }

        public double HitRate
        {
            get
            {
                if (totalTries == 0)
                {
                    return Double.NaN;
                }
                return (double) totalHits/totalTries;
            }
        }
    }
}
