using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace QingYi.Core.Timer
{
    /// <summary>
    /// Provides ultra-high precision timing with nanosecond resolution.
    /// Uses platform-specific APIs for maximum accuracy:
    /// - Windows: QueryPerformanceCounter / QueryPerformanceFrequency
    /// - Linux/macOS: clock_gettime with MONOTONIC_RAW
    /// Falls back to Stopwatch for compatibility.
    /// </summary>
    public unsafe sealed class UltraHighPrecisionTimer : IDisposable
    {
        /// <summary>
        /// Represents different timer modes available.
        /// </summary>
        public enum TimerMode
        {
            /// <summary>
            /// Automatically selects the best timer for the current platform.
            /// </summary>
            AutoDetect,

            /// <summary>
            /// Uses Windows QueryPerformanceCounter API.
            /// </summary>
            WindowsHighPrecision,

            /// <summary>
            /// Uses Unix clock_gettime with CLOCK_MONOTONIC_RAW.
            /// </summary>
            UnixMonotonicRaw,

            /// <summary>
            /// Uses managed System.Diagnostics.Stopwatch.
            /// </summary>
            ManagedStopwatch
        }

        #region Platform-Specific Native Imports

        [SupportedOSPlatform("windows")]
        private static class WindowsNative
        {
            [DllImport("kernel32.dll")]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            public static extern int QueryPerformanceCounter(long* lpPerformanceCount);

            [DllImport("kernel32.dll")]
            [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
            public static extern int QueryPerformanceFrequency(long* lpFrequency);
        }

        [SupportedOSPlatform("linux")]
        [SupportedOSPlatform("macos")]
        private static class UnixNative
        {
            public const int CLOCK_MONOTONIC_RAW = 4;

            [DllImport("libc", SetLastError = true)]
            public static extern int clock_gettime(int clk_id, TimeSpec* tp);

            [StructLayout(LayoutKind.Sequential)]
            public struct TimeSpec
            {
                public long tv_sec;  // seconds
                public long tv_nsec; // nanoseconds
            }
        }

        #endregion

        #region Constants and Fields

        private TimerMode _mode;
        private bool _useUnsafeMethods;
        private long _frequency;
        private double _tickDuration; // Time in seconds per tick
        private double _nanosecondsPerTick;

        // Cached delegates for performance
        private readonly Func<long> _getTimestamp;
        private readonly Func<long, double> _getElapsedSeconds;
        private readonly Func<long, double> _getElapsedNanoseconds;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the timer resolution in nanoseconds.
        /// Lower values indicate higher precision.
        /// </summary>
        public double ResolutionNanoseconds { get; private set; }

        /// <summary>
        /// Gets the timer frequency in Hz (ticks per second).
        /// </summary>
        public long Frequency => _frequency;

        /// <summary>
        /// Gets the timer mode currently in use.
        /// </summary>
        public TimerMode Mode => _mode;

        /// <summary>
        /// Indicates whether unsafe methods are being used for maximum performance.
        /// </summary>
        public bool IsUsingUnsafeMethods => _useUnsafeMethods;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UltraHighPrecisionTimer"/> class
        /// with the specified safety mode.
        /// </summary>
        /// <param name="allowUnsafe">
        /// When true, uses unsafe methods for maximum performance.
        /// When false, uses safe managed methods with slightly lower performance.
        /// </param>
        public UltraHighPrecisionTimer(bool allowUnsafe = true)
        {
            _useUnsafeMethods = allowUnsafe && IsUnsafeSupported();
            InitializePlatformSpecificTimer();

            // Select optimal delegates based on mode and safety settings
            (_getTimestamp, _getElapsedSeconds, _getElapsedNanoseconds) =
                CreateDelegateMethods();
        }

        /// <summary>
        /// Initializes a new instance with the specified timer mode.
        /// </summary>
        /// <param name="mode">The timer mode to use.</param>
        public UltraHighPrecisionTimer(TimerMode mode)
        {
            _mode = mode;
            _useUnsafeMethods = (mode == TimerMode.WindowsHighPrecision ||
                               mode == TimerMode.UnixMonotonicRaw) &&
                               IsUnsafeSupported();

            InitializePlatformSpecificTimer();

            (_getTimestamp, _getElapsedSeconds, _getElapsedNanoseconds) =
                CreateDelegateMethods();
        }

        private void InitializePlatformSpecificTimer()
        {
            if (_mode != TimerMode.AutoDetect)
            {
                SetupTimerForMode(_mode);
                return;
            }

            // Auto-detect best available timer
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (TryInitializeWindowsHighPrecision())
                {
                    _mode = TimerMode.WindowsHighPrecision;
                }
                else
                {
                    SetupStopwatchTimer();
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (TryInitializeUnixMonotonicRaw())
                {
                    _mode = TimerMode.UnixMonotonicRaw;
                }
                else
                {
                    SetupStopwatchTimer();
                }
            }
            else
            {
                SetupStopwatchTimer();
            }

            CalculateResolution();
        }

        #endregion

        #region Platform-Specific Initialization

        private bool TryInitializeWindowsHighPrecision()
        {
            if (!_useUnsafeMethods)
                return false;

            try
            {
                long frequency = 0;
                long counter = 0;

#pragma warning disable CA1416 // 验证平台兼容性
                if (WindowsNative.QueryPerformanceFrequency(&frequency) != 0 &&
                    WindowsNative.QueryPerformanceCounter(&counter) != 0)
                {
                    _frequency = frequency;
                    _tickDuration = 1.0 / frequency;
                    _nanosecondsPerTick = 1_000_000_000.0 / frequency;
                    return true;
                }
#pragma warning restore CA1416 // 验证平台兼容性
            }
            catch
            {
                // Fall through to return false
            }

            return false;
        }

        private bool TryInitializeUnixMonotonicRaw()
        {
            if (!_useUnsafeMethods)
                return false;

            try
            {
                UnixNative.TimeSpec ts = default;

#pragma warning disable CA1416 // 验证平台兼容性
                if (UnixNative.clock_gettime(UnixNative.CLOCK_MONOTONIC_RAW, &ts) == 0)
                {
                    // Use Stopwatch frequency as approximation for Unix clocks
                    // Actual frequency varies by hardware
                    _frequency = Stopwatch.Frequency;
                    _tickDuration = 1.0 / _frequency;
                    _nanosecondsPerTick = 1_000_000_000.0 / _frequency;
                    return true;
                }
#pragma warning restore CA1416 // 验证平台兼容性
            }
            catch
            {
                // Fall through to return false
            }

            return false;
        }

        private void SetupStopwatchTimer()
        {
            _mode = TimerMode.ManagedStopwatch;
            _frequency = Stopwatch.Frequency;
            _tickDuration = 1.0 / _frequency;
            _nanosecondsPerTick = 1_000_000_000.0 / _frequency;
        }

        private void SetupTimerForMode(TimerMode mode)
        {
            switch (mode)
            {
                case TimerMode.WindowsHighPrecision:
                    if (!TryInitializeWindowsHighPrecision())
                        throw new PlatformNotSupportedException(
                            "Windows high precision timer is not available");
                    break;

                case TimerMode.UnixMonotonicRaw:
                    if (!TryInitializeUnixMonotonicRaw())
                        throw new PlatformNotSupportedException(
                            "Unix monotonic raw timer is not available");
                    break;

                case TimerMode.ManagedStopwatch:
                    SetupStopwatchTimer();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode),
                        $"Unsupported timer mode: {mode}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the current timestamp in timer ticks.
        /// </summary>
        /// <returns>The current timestamp in ticks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetTimestamp() => _getTimestamp();

        /// <summary>
        /// Gets the elapsed time in seconds between two timestamps.
        /// </summary>
        /// <param name="startTimestamp">The starting timestamp.</param>
        /// <param name="endTimestamp">The ending timestamp.</param>
        /// <returns>Elapsed time in seconds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetElapsedSeconds(long startTimestamp, long endTimestamp) =>
            (endTimestamp - startTimestamp) * _tickDuration;

        /// <summary>
        /// Gets the elapsed time in nanoseconds between two timestamps.
        /// </summary>
        /// <param name="startTimestamp">The starting timestamp.</param>
        /// <param name="endTimestamp">The ending timestamp.</param>
        /// <returns>Elapsed time in nanoseconds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetElapsedNanoseconds(long startTimestamp, long endTimestamp) =>
            (endTimestamp - startTimestamp) * _nanosecondsPerTick;

        /// <summary>
        /// Gets the elapsed time in seconds since the specified timestamp.
        /// </summary>
        /// <param name="startTimestamp">The starting timestamp.</param>
        /// <returns>Elapsed time in seconds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetElapsedSeconds(long startTimestamp) =>
            _getElapsedSeconds(startTimestamp);

        /// <summary>
        /// Gets the elapsed time in nanoseconds since the specified timestamp.
        /// </summary>
        /// <param name="startTimestamp">The starting timestamp.</param>
        /// <returns>Elapsed time in nanoseconds.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetElapsedNanoseconds(long startTimestamp) =>
            _getElapsedNanoseconds(startTimestamp);

        /// <summary>
        /// Creates a timestamp measurement context for easy elapsed time calculation.
        /// </summary>
        /// <returns>A new timestamp measurement context.</returns>
        public TimestampContext CreateContext() => new(this);

        /// <summary>
        /// Measures the execution time of an action with nanosecond precision.
        /// </summary>
        /// <param name="action">The action to measure.</param>
        /// <returns>Elapsed time in nanoseconds.</returns>
        public double MeasureExecutionTime(Action action)
        {
            long start = GetTimestamp();
            action();
            long end = GetTimestamp();
            return GetElapsedNanoseconds(start, end);
        }

        /// <summary>
        /// Measures the execution time of a function with nanosecond precision.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="function">The function to measure.</param>
        /// <returns>A tuple containing the result and elapsed time in nanoseconds.</returns>
        public (T Result, double ElapsedNanoseconds) MeasureExecutionTime<T>(Func<T> function)
        {
            long start = GetTimestamp();
            T result = function();
            long end = GetTimestamp();
            return (result, GetElapsedNanoseconds(start, end));
        }

        #endregion

        #region Private Helper Methods

        private (Func<long>, Func<long, double>, Func<long, double>) CreateDelegateMethods()
        {
            Func<long> getTimestamp;
            Func<long, double> getElapsedSeconds;
            Func<long, double> getElapsedNanoseconds;

            if (_useUnsafeMethods && _mode == TimerMode.WindowsHighPrecision)
            {
                getTimestamp = GetWindowsHighPrecisionTimestamp;
                getElapsedSeconds = (start) =>
                    (GetWindowsHighPrecisionTimestamp() - start) * _tickDuration;
                getElapsedNanoseconds = (start) =>
                    (GetWindowsHighPrecisionTimestamp() - start) * _nanosecondsPerTick;
            }
            else if (_useUnsafeMethods && _mode == TimerMode.UnixMonotonicRaw)
            {
                getTimestamp = GetUnixMonotonicRawTimestamp;
                getElapsedSeconds = (start) =>
                    (GetUnixMonotonicRawTimestamp() - start) * _tickDuration;
                getElapsedNanoseconds = (start) =>
                    (GetUnixMonotonicRawTimestamp() - start) * _nanosecondsPerTick;
            }
            else
            {
                getTimestamp = Stopwatch.GetTimestamp;
                getElapsedSeconds = (start) =>
                    (Stopwatch.GetTimestamp() - start) * _tickDuration;
                getElapsedNanoseconds = (start) =>
                    (Stopwatch.GetTimestamp() - start) * _nanosecondsPerTick;
            }

            return (getTimestamp, getElapsedSeconds, getElapsedNanoseconds);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetWindowsHighPrecisionTimestamp()
        {
            long counter = 0;
#pragma warning disable CA1416 // 验证平台兼容性
            WindowsNative.QueryPerformanceCounter(&counter);
#pragma warning restore CA1416 // 验证平台兼容性
            return counter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetUnixMonotonicRawTimestamp()
        {
            UnixNative.TimeSpec ts = default;
#pragma warning disable CA1416 // 验证平台兼容性
            UnixNative.clock_gettime(UnixNative.CLOCK_MONOTONIC_RAW, &ts);

            // Convert to nanoseconds, then to Stopwatch ticks for consistency
            long nanoseconds = ts.tv_sec * 1_000_000_000L + ts.tv_nsec;
#pragma warning restore CA1416 // 验证平台兼容性
            return (long)(nanoseconds * (_frequency / 1_000_000_000.0));
        }

        private void CalculateResolution()
        {
            // Measure timer resolution by taking multiple samples
            const int samples = 100;
            long[] deltas = new long[samples];

            for (int i = 0; i < samples; i++)
            {
                long t1 = GetTimestamp();
                long t2 = GetTimestamp();

                // Spin until we get a different timestamp
                while (t2 == t1)
                {
                    t2 = GetTimestamp();
                }

                deltas[i] = t2 - t1;
            }

            // Use minimum delta as resolution estimate
            long minDelta = deltas.Min();
            ResolutionNanoseconds = minDelta * _nanosecondsPerTick;
        }

        private static bool IsUnsafeSupported()
        {
            // Check if unsafe code is allowed in current context
            try
            {
                // Try to compile a simple unsafe operation
                Unsafe.SizeOf<int>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region IDisposable Implementation

        private bool _disposed;

        /// <summary>
        /// Releases all resources used by the timer.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        ~UltraHighPrecisionTimer()
        {
            Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Provides a convenient context for measuring elapsed time.
    /// </summary>
    public readonly ref struct TimestampContext
    {
        private readonly UltraHighPrecisionTimer _timer;
        private readonly long _startTimestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimestampContext"/> struct.
        /// </summary>
        /// <param name="timer">The timer instance to use for measurements.</param>
        public TimestampContext(UltraHighPrecisionTimer timer)
        {
            _timer = timer ?? throw new ArgumentNullException(nameof(timer));
            _startTimestamp = timer.GetTimestamp();
        }

        /// <summary>
        /// Gets the elapsed time in seconds since the context was created.
        /// </summary>
        public double ElapsedSeconds => _timer.GetElapsedSeconds(_startTimestamp);

        /// <summary>
        /// Gets the elapsed time in nanoseconds since the context was created.
        /// </summary>
        public double ElapsedNanoseconds => _timer.GetElapsedNanoseconds(_startTimestamp);

        /// <summary>
        /// Restarts the measurement and returns the elapsed time since the last start.
        /// </summary>
        /// <returns>The elapsed time in nanoseconds since the context was created or last restarted.</returns>
        public double Restart()
        {
            // Using Unsafe to modify readonly field in safe context
            long newTimestamp = _timer.GetTimestamp();
            double elapsed = _timer.GetElapsedNanoseconds(_startTimestamp);
            Unsafe.AsRef(in _startTimestamp) = newTimestamp;
            return elapsed;
        }
    }

    /// <summary>
    /// Provides extension methods for <see cref="UltraHighPrecisionTimer"/>.
    /// </summary>
    public static class UltraHighPrecisionTimerExtensions
    {
        /// <summary>
        /// Measures the execution time of an asynchronous action.
        /// </summary>
        /// <param name="timer">The timer instance.</param>
        /// <param name="asyncAction">The asynchronous action to measure.</param>
        /// <returns>Elapsed time in nanoseconds.</returns>
        public static async Task<double> MeasureExecutionTimeAsync(
            this UltraHighPrecisionTimer timer,
            Func<Task> asyncAction)
        {
            long start = timer.GetTimestamp();
            await asyncAction();
            long end = timer.GetTimestamp();
            return timer.GetElapsedNanoseconds(start, end);
        }

        /// <summary>
        /// Measures the execution time of an asynchronous function.
        /// </summary>
        /// <typeparam name="T">The return type of the function.</typeparam>
        /// <param name="timer">The timer instance.</param>
        /// <param name="asyncFunction">The asynchronous function to measure.</param>
        /// <returns>A tuple containing the result and elapsed time in nanoseconds.</returns>
        public static async Task<(T Result, double ElapsedNanoseconds)>
            MeasureExecutionTimeAsync<T>(
                this UltraHighPrecisionTimer timer,
                Func<Task<T>> asyncFunction)
        {
            long start = timer.GetTimestamp();
            T result = await asyncFunction();
            long end = timer.GetTimestamp();
            return (result, timer.GetElapsedNanoseconds(start, end));
        }

        /// <summary>
        /// Calibrates the timer by measuring its resolution and jitter.
        /// </summary>
        /// <param name="timer">The timer instance.</param>
        /// <param name="iterations">Number of calibration iterations.</param>
        /// <returns>Calibration results including resolution and jitter.</returns>
        public static TimerCalibrationResult Calibrate(
            this UltraHighPrecisionTimer timer,
            int iterations = 1000)
        {
            if (iterations < 10)
                throw new ArgumentException("At least 10 iterations are required",
                    nameof(iterations));

            List<double> measurements = new(iterations);

            for (int i = 0; i < iterations; i++)
            {
                long start = timer.GetTimestamp();
                long end = timer.GetTimestamp();

                while (end == start)
                {
                    end = timer.GetTimestamp();
                }

                measurements.Add(timer.GetElapsedNanoseconds(start, end));
            }

            return new TimerCalibrationResult(measurements);
        }
    }

    /// <summary>
    /// Represents the results of timer calibration.
    /// </summary>
    /// <param name="Measurements">Individual timing measurements in nanoseconds.</param>
    public readonly record struct TimerCalibrationResult(
        IReadOnlyList<double> Measurements)
    {
        /// <summary>
        /// Gets the minimum measured resolution in nanoseconds.
        /// </summary>
        public double MinResolution => Measurements.Min();

        /// <summary>
        /// Gets the maximum measured resolution in nanoseconds.
        /// </summary>
        public double MaxResolution => Measurements.Max();

        /// <summary>
        /// Gets the average resolution in nanoseconds.
        /// </summary>
        public double AverageResolution => Measurements.Average();

        /// <summary>
        /// Gets the median resolution in nanoseconds.
        /// </summary>
        public double MedianResolution
        {
            get
            {
                var sorted = Measurements.OrderBy(x => x).ToList();
                int mid = sorted.Count / 2;

                if (sorted.Count % 2 == 0)
                    return (sorted[mid - 1] + sorted[mid]) / 2.0;
                else
                    return sorted[mid];
            }
        }

        /// <summary>
        /// Gets the standard deviation of measurements in nanoseconds.
        /// </summary>
        public double StandardDeviation
        {
            get
            {
                double avg = AverageResolution;
                double sum = Measurements.Sum(x => Math.Pow(x - avg, 2));
                return Math.Sqrt(sum / Measurements.Count);
            }
        }
    }
}
