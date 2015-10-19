using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    /// <summary>
    /// Runs as a thread processing "requests" coming in over channel, recording
    /// the time required to 
    /// </summary>
    class RequestProcessor: IRunnable
    {
        private Channel<string> requestChannel;
        private Channel<long> statsChannel;
        private ITokenParser parser;

        public RequestProcessor(Channel<string> requestChannel, Channel<long> statsChannel, ITokenParser parser)
        {
            this.requestChannel = requestChannel;
            this.statsChannel = statsChannel;
            this.parser = parser;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("Request processor starting up");
            await requestChannel.ReceiveAllAsync(async token =>
            {
                Stopwatch s = new Stopwatch();
                s.Start();

                await parser.ParseAsync(token);

                s.Stop();
                statsChannel.Publish(s.ElapsedMilliseconds);
            });

            Console.WriteLine("Request processor shutting down, channel is closed");
        }
    }
}
