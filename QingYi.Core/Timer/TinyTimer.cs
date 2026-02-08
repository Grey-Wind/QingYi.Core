#if !BROWSER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace QingYi.Core.Timer
{
    /// <summary>
    /// Ultra-low overhead and high-performance timer class
    /// </summary>
    /// <remarks>
    /// This timer is designed for scenarios requiring minimal performance impact.
    /// It uses a dedicated thread with adaptive waiting strategies for precise timing.
    /// </remarks>
    public sealed class TinyTimer : IDisposable
    {
        #region Private Fields
        private readonly long _intervalTicks;          // Interval in ticks
        private readonly Action<long> _callback;       // Callback function (parameter: execution count)
        private readonly bool _useHighPrecision;       // Whether to use high-precision timing
        private readonly Stopwatch _stopwatch;         // High-precision stopwatch
        private long _executionCount;                  // Number of executions
        private volatile bool _isRunning;              // Running state
        private Thread _timerThread;                   // Timer thread
        private readonly ManualResetEventSlim _stopEvent; // Stop synchronization event
        private SpinWait _spinWait;                    // Spin-wait object for short waits
        private long _lastExecutionTime;               // Last execution time in ticks
        #endregion

        #region Properties
        /// <summary>
        /// Gets whether the timer is currently running
        /// </summary>
        /// <value>True if the timer is active; otherwise false</value>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Gets the total number of times the callback has been executed
        /// </summary>
        /// <value>The execution count as a 64-bit integer</value>
        public long ExecutionCount => Interlocked.Read(ref _executionCount);

        /// <summary>
        /// Gets or sets the timer interval in milliseconds
        /// </summary>
        /// <value>The interval in milliseconds. Setting this property stops the timer if running.</value>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when value is less than or equal to 0</exception>
        public int IntervalMs
        {
            get => (int)(_intervalTicks / TimeSpan.TicksPerMillisecond);
            private init
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(IntervalMs), "Interval must be greater than 0");
                _intervalTicks = value * TimeSpan.TicksPerMillisecond;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the TinyTimer class
        /// </summary>
        /// <param name="intervalMs">The time interval between invocations of the callback in milliseconds</param>
        /// <param name="callback">A delegate representing the method to execute when the timer elapses</param>
        /// <param name="useHighPrecision">Whether to use high-precision timing (Stopwatch). Default is true</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when intervalMs is less than or equal to 0</exception>
        /// <exception cref="ArgumentNullException">Thrown when callback is null</exception>
        public TinyTimer(int intervalMs, Action<long> callback, bool useHighPrecision = true)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            IntervalMs = intervalMs;
            _useHighPrecision = useHighPrecision;
            _stopEvent = new ManualResetEventSlim(false);
            _spinWait = new SpinWait();

            if (_useHighPrecision)
            {
                _stopwatch = new Stopwatch();
            }

            _lastExecutionTime = GetCurrentTimestamp();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts the timer
        /// </summary>
        /// <remarks>
        /// If the timer is already running, this method has no effect.
        /// The timer executes the callback on a dedicated background thread.
        /// </remarks>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _executionCount = 0;
            _stopEvent.Reset();

            _timerThread = new Thread(TimerWorker)
            {
                Name = $"TinyTimer_{IntervalMs}ms",
                IsBackground = true,
                Priority = ThreadPriority.Normal
            };

            if (_useHighPrecision)
            {
                _stopwatch.Restart();
            }

            _lastExecutionTime = GetCurrentTimestamp();
            _timerThread.Start();
        }

        /// <summary>
        /// Stops the timer
        /// </summary>
        /// <remarks>
        /// If the timer is not running, this method has no effect.
        /// This method blocks until the timer thread has safely terminated.
        /// </remarks>
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _stopEvent.Set();

            // Wait for timer thread to finish with timeout
            _timerThread?.Join(TimeSpan.FromMilliseconds(IntervalMs * 2));
            _timerThread = null;
        }

        /// <summary>
        /// Restarts the timer
        /// </summary>
        /// <remarks>
        /// Stops the timer if it's running, then starts it again.
        /// This resets the execution count to 0.
        /// </remarks>
        public void Restart()
        {
            Stop();
            Start();
        }

        /// <summary>
        /// Releases all resources used by the current instance of TinyTimer
        /// </summary>
        /// <remarks>
        /// This method stops the timer if it's running and releases managed resources.
        /// Call Dispose when you are finished using the TinyTimer.
        /// </remarks>
        public void Dispose()
        {
            Stop();
            _stopEvent?.Dispose();

            if (_useHighPrecision)
            {
                _stopwatch?.Stop();
            }

            GC.SuppressFinalize(this);
        }
        #endregion

        #region Core Timer Logic
        /// <summary>
        /// Main worker method that runs on the timer thread
        /// </summary>
        private void TimerWorker()
        {
            while (_isRunning)
            {
                try
                {
                    long currentTime = GetCurrentTimestamp();
                    long nextExecutionTime = _lastExecutionTime + _intervalTicks;

                    if (currentTime >= nextExecutionTime)
                    {
                        // Execute callback
                        long count = Interlocked.Increment(ref _executionCount);
                        _callback?.Invoke(count);

                        // Update last execution time
                        _lastExecutionTime = currentTime;

                        // Calculate next execution time
                        nextExecutionTime = _lastExecutionTime + _intervalTicks;
                    }

                    // Wait until next execution time
                    WaitUntilNextExecution(nextExecutionTime);
                }
                catch (ThreadInterruptedException)
                {
                    // Thread was interrupted, exit normally
                    break;
                }
                catch (Exception ex)
                {
                    // Log exception but don't stop the timer
                    Debug.WriteLine($"TinyTimer exception: {ex.Message}");
                    // Small delay to prevent tight error loops
                    Thread.Sleep(Math.Min(100, IntervalMs));
                }
            }
        }

        /// <summary>
        /// Waits until the specified next execution time using adaptive waiting strategies
        /// </summary>
        /// <param name="nextExecutionTime">The timestamp (in ticks) when the next execution should occur</param>
        /// <remarks>
        /// Uses a hybrid waiting strategy:
        /// - Event wait for long intervals (>10ms)
        /// - Thread.Sleep for medium intervals (1-10ms)
        /// - SpinWait for short intervals (&lt;1ms)
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WaitUntilNextExecution(long nextExecutionTime)
        {
            // Use hybrid waiting strategy
            while (_isRunning)
            {
                long currentTime = GetCurrentTimestamp();
                long remainingTicks = nextExecutionTime - currentTime;

                if (remainingTicks <= 0)
                    break;

                // Select waiting strategy based on remaining time
                if (remainingTicks > TimeSpan.TicksPerMillisecond * 10)
                {
                    // Long wait: use event wait with timeout
                    int waitMs = (int)(remainingTicks / TimeSpan.TicksPerMillisecond);
                    _stopEvent.Wait(Math.Max(1, waitMs / 2));
                }
                else if (remainingTicks > TimeSpan.TicksPerMillisecond * 1)
                {
                    // Medium wait: use Thread.Sleep
                    int waitMs = (int)(remainingTicks / TimeSpan.TicksPerMillisecond);
                    Thread.Sleep(Math.Max(0, waitMs));
                }
                else
                {
                    // Short wait: use spin waiting
                    _spinWait.SpinOnce();
                }

                // Check for early exit
                if (!_isRunning)
                    break;
            }
        }

        /// <summary>
        /// Gets the current timestamp in ticks
        /// </summary>
        /// <returns>The current timestamp in ticks</returns>
        /// <remarks>
        /// Uses Stopwatch for high precision if enabled, otherwise uses DateTime.UtcNow
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetCurrentTimestamp()
        {
            if (_useHighPrecision && _stopwatch.IsRunning)
            {
                // Use high-precision Stopwatch timing
                return _stopwatch.Elapsed.Ticks;
            }

            // Use lower precision DateTime timing
            return DateTime.UtcNow.Ticks;
        }
        #endregion

        #region Static Factory Methods
        /// <summary>
        /// Creates and starts a new TinyTimer instance
        /// </summary>
        /// <param name="intervalMs">The time interval between invocations of the callback in milliseconds</param>
        /// <param name="callback">A delegate representing the method to execute when the timer elapses</param>
        /// <param name="useHighPrecision">Whether to use high-precision timing. Default is true</param>
        /// <returns>A new TinyTimer instance that is already started</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when intervalMs is less than or equal to 0</exception>
        /// <exception cref="ArgumentNullException">Thrown when callback is null</exception>
        public static TinyTimer StartNew(int intervalMs, Action<long> callback, bool useHighPrecision = true)
        {
            var timer = new TinyTimer(intervalMs, callback, useHighPrecision);
            timer.Start();
            return timer;
        }

        /// <summary>
        /// Creates a timer that executes once after a specified delay
        /// </summary>
        /// <param name="delayMs">The delay in milliseconds before executing the action</param>
        /// <param name="action">The action to execute after the delay</param>
        /// <returns>A TinyTimer instance configured for single delayed execution</returns>
        /// <remarks>
        /// The timer automatically stops after executing the action once.
        /// The caller is responsible for disposing the timer.
        /// </remarks>
        public static TinyTimer DelayedStart(int delayMs, Action action)
        {
            var timer = new TinyTimer(delayMs, count =>
            {
                if (count == 1) // Execute only once
                {
                    action?.Invoke();
                }
            });

            timer.Start();
            return timer;
        }
        #endregion
    }

    /// <summary>
    /// Provides extension methods for TinyTimer
    /// </summary>
    public static class TinyTimerExtensions
    {
        /// <summary>
        /// Safely restarts the timer if it's running
        /// </summary>
        /// <param name="timer">The TinyTimer instance</param>
        /// <remarks>
        /// If the timer is running, it restarts. If not running, it starts the timer.
        /// If the timer is null, this method has no effect.
        /// </remarks>
        public static void SafeRestart(this TinyTimer timer)
        {
            if (timer?.IsRunning == true)
            {
                timer.Restart();
            }
            else
            {
                timer?.Start();
            }
        }
    }
}
#endif
