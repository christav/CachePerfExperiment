using System;
using System.Runtime.CompilerServices;

namespace CachePerfExperiment
{
    class StatisticsTracker
    {
        private long n = 0;
        private double oldM;
        private double newM;
        private double oldS;
        private double newS;

        public StatisticsTracker()
        {
            Min = Int64.MaxValue;
            Max = Int64.MinValue;
        }

        public void AddSample(long x)
        {
            ++n;
            if (n == 1)
            {
                oldM = newM = x;
                oldS = 0.0;
            }
            else
            {
                newM = oldM + (x - oldM) / n;
                newS = oldS + (x - oldM)*(x - newM);
                oldM = newM;
                oldS = newS;
            }
            Min = x < Min ? x : Min;
            Max = x > Max ? x : Max;
        }

        public long Count
        {
            get { return n; }
        }

        public double Mean
        {
            get { return (n > 0) ? newM : 0.0; }
        }

        public double Variance
        {
            get { return (n > 1) ? newS/(n - 1) : 0.0; }
        }
        public double StandardDeviation
        {
            get { return Math.Sqrt(Variance); }
        }

        public long Min { get; private set; }
        public long Max { get; private set; }
    }
}
