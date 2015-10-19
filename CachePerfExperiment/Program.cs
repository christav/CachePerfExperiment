using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CachePerfExperiment
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().RunSimulation();
        }

        private void RunSimulation()
        {
            ITokenParser parser = CreateTokenParser();
            var requestChannel = new Channel<string>(10000);
            var statsChannel = new Channel<long>(10000);
            var actors = 
                new IRunnable[] {new RequestSource(requestChannel)}
                    .Concat(
                        Enumerable.Range(0, Parameters.NumRequestProcessors)
                        .Select(n => new RequestProcessor(requestChannel, statsChannel, parser)))
                .ToList();

            var stats = new StatsProcessor(statsChannel);
            var statsTask = stats.RunAsync();

            var actorTasks = actors.Select(a => a.RunAsync()).ToArray();

            Console.WriteLine("Waiting for simulation to complete");
            Task.WaitAll(actorTasks);

            Console.WriteLine("Simulation completed");
            statsChannel.Close();
            statsTask.Wait();

            Console.WriteLine("Number of Messages: {0}", stats.Count);
            Console.WriteLine("Min Processing Time: {0} ms", stats.Min);
            Console.WriteLine("Average processing time: {0}", stats.Mean);
            Console.WriteLine("Max Processing Time: {0} ms", stats.Max);
            Console.WriteLine("Standard Deviation: {0}", stats.StandardDeviation);
        }

        private ITokenParser CreateTokenParser()
        {
            return Decorator.Chain<ITokenParser>(
                //new TokenParserCache2(),
                new TokenParserCache(),
                new SlowTokenParser());
        }
    }
}
