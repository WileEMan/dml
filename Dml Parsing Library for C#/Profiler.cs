using System;
using System.Collections.Generic;
using System.Text;

namespace WileyBlack.Utility
{
    public class wbProfiler : IDisposable
    {
        public string Name;

        public wbProfiler(string Name)
        {
            this.Name = Name;
        }

        ~wbProfiler()
        {
            if (OutputAtSample < Samples) ResultsToConsole();
        }

        public void Dispose()
        {
            if (OutputAtSample < Samples) ResultsToConsole();
        }

        //long StartTick = 0;
        System.Diagnostics.Stopwatch SW = new System.Diagnostics.Stopwatch();

        public void Start()
        {
            //StartTick = DateTime.Now.Ticks;
            SW.Start();
        }

        public void End()
        {
#           if true
            SW.Stop();
            double ElapsedSeconds = SW.Elapsed.TotalSeconds;
            SW.Reset();
#           else
            const double TicksToSeconds = (1.0 /*second*/ / 1000000000.0 /*nanoseconds*/) * (100.0 /*nanoseconds*/ / 1.0 /*tick*/);            
            long EndTick = DateTime.Now.Ticks;            
            long ElapsedTicks = EndTick - StartTick;                        
            double ElapsedSeconds = (double)ElapsedTicks * TicksToSeconds;
#           endif

            Sum += (double)ElapsedSeconds;
            SumOfSquares += (double)ElapsedSeconds * (double)ElapsedSeconds;
            Samples++;
            if (ElapsedSeconds < MinTime) MinTime = ElapsedSeconds;
            if (ElapsedSeconds > MaxTime) MaxTime = ElapsedSeconds;
        }
        
        double Sum = 0.0;
        double SumOfSquares = 0.0;
        double MinTime = double.MaxValue;
        double MaxTime = double.MinValue;
        public int Samples = 0;        

        string AutoFormat(double Seconds)
        {
            if (Seconds > 1.0) return string.Format("{0:0.0} s", Seconds);
            else if (Seconds > 0.010) return string.Format("{0:0.000} s", Seconds);
            else if (Seconds > 0.000010) return string.Format("{0:0.000} ms", (Seconds * 1000.0));
            else return string.Format("{0:0.000} µs", (Seconds * 1000000.0));
        }

        int OutputAtSample = -1;

        public void ResultsToConsole()
        {
            OutputAtSample = Samples;

            if (Samples < 1)
            {
                Console.Write("Profiler '{0}' had no samples.\n", Name);
                return;
            }

            double Mean = Sum / (double)Samples;
            double Variance = (SumOfSquares / (double)Samples) - (Mean * Mean);
            double StdDev = Math.Sqrt(Variance);

            Console.Write("Profiler '{0}' saw performance of {1} < {2} (Mean) < {3} with stddev of {4}.  {5} Samples.\n",
                Name, AutoFormat(MinTime), AutoFormat(Mean), AutoFormat(MaxTime), AutoFormat(StdDev), Samples);
        }
    }

    public class wbFrequencyProfiler
    {
        public string Name;
        public TimeSpan WarmupPeriod;

        public wbFrequencyProfiler(string Name)
        {
            this.Name = Name;
            this.WarmupPeriod = new TimeSpan(0);
        }

        public wbFrequencyProfiler(string Name, TimeSpan WarmupPeriod)
        {
            this.Name = Name;
            this.WarmupPeriod = WarmupPeriod;
        }

        ~wbFrequencyProfiler()
        {
            if (OutputAtSample < Samples) ResultsToConsole();
        }

        public void Dispose()
        {
            if (OutputAtSample < Samples) ResultsToConsole();
        }

        System.Diagnostics.Stopwatch Initial = new System.Diagnostics.Stopwatch();

        System.Diagnostics.Stopwatch SW = new System.Diagnostics.Stopwatch();
        bool Counting = false;

        public int Samples = 0;        
        double Sum = 0.0;
        double SumOfSquares = 0.0;
        double MinTime = double.MaxValue;
        double MaxTime = double.MinValue;

        public void Mark()
        {
            if (!Counting) { Counting = true; SW.Start(); Initial.Start(); }
            else
            {
                double ElapsedSeconds = SW.Elapsed.TotalSeconds;
                if (Initial.Elapsed.TotalSeconds >= WarmupPeriod.TotalSeconds)
                {
                    Sum += (double)ElapsedSeconds;
                    SumOfSquares += (double)ElapsedSeconds * (double)ElapsedSeconds;
                    Samples++;
                    if (ElapsedSeconds < MinTime) MinTime = ElapsedSeconds;
                    if (ElapsedSeconds > MaxTime) MaxTime = ElapsedSeconds;
                }

                SW.Reset(); SW.Start();
            }
        }

        public void Discard()
        {
            SW.Reset();
            Counting = false;
        }                                            

        string AutoFormat(double Seconds)
        {
            if (Seconds < 1.0e-10)
                return "[Inf] Hz";
            double Freq = 1.0 / Seconds;
            if (Freq > 1000000.0) return string.Format("{0:0.0} MHz", (Freq / 1000000.0));
            else if (Freq > 1000.0) return string.Format("{0:0.0} KHz", (Freq / 1000.0));
            else if (Freq >= 1.0) return string.Format("{0:0.0} Hz", Freq);
            else return string.Format("{0:0.000} Hz", Freq);
        }

        int OutputAtSample = -1;

        public void ResultsToConsole()
        {
            OutputAtSample = Samples;

            if (Samples < 1)
            {
                Console.Write("Frequency Profiler '{0}' had no samples.\n", Name);
                return;
            }

            double Mean = Sum / (double)Samples;
            double Variance = (SumOfSquares / (double)Samples) - (Mean * Mean);
            double StdDev = Math.Sqrt(Variance);

            Console.Write("Frequency Profiler '{0}' saw performance of {1} < {2} (Mean) < {3} with stddev of {4}.  {5} Samples.\n",
                Name, AutoFormat(MaxTime), AutoFormat(Mean), AutoFormat(MinTime), AutoFormat(StdDev), Samples);
        }
    }
}
