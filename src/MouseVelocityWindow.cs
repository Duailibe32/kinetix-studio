using System;

namespace KinetixStudio
{
    // Average displacement over a short, time-weighted window. This bridges empty
    // timer ticks between mouse reports without depending on the timer frequency.
    public sealed class MouseVelocityWindow
    {
        struct Sample { public double X, Y, Seconds; }
        readonly Sample[] samples = new Sample[32];
        int next, count;

        public void Reset() { next = count = 0; }

        public void Apply(ref double x, ref double y, double seconds, bool enabled)
        {
            if (!enabled || seconds <= 0 || seconds >= .016)
            {
                Reset();
                return;
            }
            samples[next] = new Sample { X = x, Y = y, Seconds = seconds };
            next = (next + 1) % samples.Length;
            count = Math.Min(count + 1, samples.Length);
            double duration = 0, sumX = 0, sumY = 0;
            for (int i = 0; i < count && duration < .016; i++)
            {
                Sample sample = samples[(next - 1 - i + samples.Length) % samples.Length];
                double used = Math.Min(sample.Seconds, .016 - duration);
                sumX += sample.X * used / sample.Seconds;
                sumY += sample.Y * used / sample.Seconds;
                duration += used;
            }
            x = sumX / duration * seconds;
            y = sumY / duration * seconds;
        }
    }
}
