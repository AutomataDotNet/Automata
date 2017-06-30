using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Automata.Utilities
{
    /// <summary>
    /// High precision timer
    /// </summary>
    internal static class HighTimer
    {
        [SuppressUnmanagedCodeSecurity]
        sealed class Win32
        {
            [DllImport("Kernel32.dll"), SuppressUnmanagedCodeSecurity]
            public static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

            [DllImport("Kernel32.dll"), SuppressUnmanagedCodeSecurity]
            public static extern bool QueryPerformanceFrequency(out long lpFrequency);
        }

        private readonly static long frequency;
        static HighTimer()
        {
            if (!Win32.QueryPerformanceFrequency(out frequency))
            {
                // high-performance counter not supported
                throw new Exception();
            }
        }

        /// <summary>
        /// Gets the frequency.
        /// </summary>
        /// <value>The frequency.</value>
        public static long Frequency
        {
            get { return frequency; }
        }

        /// <summary>
        /// Gets the current ticks value.
        /// </summary>
        /// <value>The now.</value>
        public static long Now
        {
            get
            {
                long startTime;
                if (!Win32.QueryPerformanceCounter(out startTime))
                    throw new AutomataException("QueryPerformanceCounter failed");
                return startTime;
            }
        }

        /// <summary>
        /// Returns the duration of the timer (in seconds)
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static double ToSeconds(long start, long end)
        {
            return (end - start) / (double)frequency;
        }

        /// <summary>
        ///Returns the duration in seconds
        /// </summary>
        /// <param name="ticks">The ticks.</param>
        /// <returns></returns>
        public static double ToSeconds(long ticks)
        {
            return ticks / (double)frequency;
        }

        /// <summary>
        ///Returns the duration in seconds from <paramref name="start"/>
        /// </summary>
        /// <param name="start">The start.</param>
        /// <returns></returns>
        public static double ToSecondsFromNow(long start)
        {
            return ToSeconds(start, HighTimer.Now);
        }
    }
}
