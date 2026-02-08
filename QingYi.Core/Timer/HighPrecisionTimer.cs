using System;
using System.Diagnostics;

namespace QingYi.Core.Timer
{
    /// <summary>
    /// Provides high-precision timing function, supports two time units of millisecond and second measurement, and retains accuracy to two decimal places.
    /// </summary>
    /// <remarks>
    /// Based on the System. Diagnostics. Stopwatch realize timing accuracy depends on the hardware and operating System support. Before using recommends a Stopwatch. IsHighResolution inspection precision timing state support.
    /// </remarks>
    public class HighPrecisionTimer
    {
        private readonly Stopwatch _stopwatch;

        /// <summary>
        /// Initializes a new high-precision timer instance.
        /// </summary>
        /// <remarks>
        /// The Stopwatch instance is automatically created, but the timing does not start immediately.
        /// </remarks>
        public HighPrecisionTimer() => _stopwatch = new Stopwatch();

        /// <summary>
        /// Start timer.
        /// </summary>
        /// <remarks>
        /// If the timer is already running, calling this method has no effect. Multiple starts will not reset the timer, keeping the original timing state.
        /// </remarks>
        public void Start()
        {
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
            }
        }

        /// <summary>
        /// Stop timer.
        /// </summary>
        /// <remarks>
        /// If the timer has already stopped, calling this method will have no effect. After stopping, timing can be resumed using the Start method.
        /// </remarks>
        public void Stop()
        {
            if (_stopwatch.IsRunning)
            {
                _stopwatch.Stop();
            }
        }

        /// <summary>
        /// Reset the timer to its initial state.
        /// </summary>
        /// <remarks>
        /// The timing is stopped and the recorded time cleared regardless of whether it is currently running. After the reset, you need to manually call Start to restart the timing.
        /// </remarks>
        public void Reset() => _stopwatch.Reset();

        /// <summary>
        /// Gets the number of milliseconds passed.
        /// </summary>
        /// <returns>A time value of type decimal, with two decimal digits reserved.</returns>
        /// <remarks>
        /// The result is rounded to two decimal places. The timer runtime call returns the currently timed value without affecting the timing status.
        /// </remarks>
        public decimal GetElapsedMilliseconds() => Math.Round((decimal)_stopwatch.Elapsed.TotalMilliseconds, 4);

        /// <summary>
        /// Gets the number of seconds passed.
        /// </summary>
        /// <returns>A time value of type decimal, with two decimal digits reserved.</returns>
        /// <remarks>
        /// The result is rounded to two decimal places. The timer runtime call returns the currently timed value without affecting the timing status.
        /// </remarks>
        public decimal GetElapsedSeconds() => Math.Round((decimal)_stopwatch.Elapsed.TotalSeconds, 4);

        /// <summary>
        /// Get the time value in both milliseconds and seconds
        /// </summary>
        /// <returns>
        /// A tuple containing two decimal values: <br />
        /// · Milliseconds - milliseconds (keep two decimal places)<br />
        /// · Seconds - Number of seconds (two decimal places reserved)
        /// </returns>
        /// <remarks>
        /// The two return values are obtained at the same point in time to ensure data consistency. Applicable to scenarios where two time units need to be displayed at the same time.
        /// </remarks>
        public (decimal Milliseconds, decimal Seconds) GetBothElapsedTimes()
        {
            var ms = (decimal)_stopwatch.Elapsed.TotalMilliseconds;
            var sec = (decimal)_stopwatch.Elapsed.TotalSeconds;
            return (Math.Round(ms, 2), Math.Round(sec, 2));
        }
    }
}
