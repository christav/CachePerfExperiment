using System;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class StatsProcessor : IRunnable
    {
        private StatisticsTracker stats = new StatisticsTracker();
        private Channel<long> samplesChannel;

        public StatsProcessor(Channel<long> samplesChannel)
        {
            this.samplesChannel = samplesChannel;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Stats processor starting up");
            await samplesChannel.ReceiveAllAsync(sample =>
            {
                stats.AddSample(sample);
            });

            Console.WriteLine("Stats processor shutting down, channel was closed");

        }

        public long Count { get { return stats.Count; } }
        public double Mean { get { return stats.Mean; } }
        public double StandardDeviation { get { return stats.StandardDeviation; } }

        public long Min { get { return stats.Min; } }
        public long Max { get { return stats.Max; } }
    }
}
