namespace QingYi.Core.FileUtility.IO
{
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER

    using System;
    using System.IO;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using System.Threading;
    using System.IO.MemoryMappedFiles;

    /// <summary>
    /// Provides high-performance buffered file writing with configurable buffering strategies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Features:
    /// <list type="bullet">
    /// <item><description>Dual-mode writing (buffered/direct)</description></item>
    /// <item><description>IL-optimized memory copying</description></item>
    /// <item><description>Async support with proper cancellation</description></item>
    /// <item><description>Automatic file size management</description></item>
    /// <item><description>Memory-mapped I/O for large writes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Thread safety: Instance methods are not thread-safe. External synchronization required for concurrent access.
    /// </para>
    /// </remarks>
    public sealed class FileWriter : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly byte[] _buffer;
        private int _bufferPosition;
        private readonly int _bufferSize;
        private readonly bool _leaveOpen;

        // IL-optimized memory copier
        private static readonly MemoryCopier _memoryCopier = new MemoryCopier();

        /// <summary>
        /// Initializes a new instance writing to specified file path.
        /// </summary>
        /// <param name="path">Target file path</param>
        /// <param name="bufferSize">Buffer size in bytes (default: 81,920 bytes)</param>
        /// <param name="mode">File creation mode (default: Create)</param>
        /// <param name="access">File access type (default: Write)</param>
        /// <param name="share">File sharing permissions (default: Read)</param>
        /// <param name="options">Advanced file options (default: None)</param>
        /// <exception cref="ArgumentNullException">Thrown when path is null</exception>
        /// <exception cref="IOException">Thrown for file system access failures</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown for insufficient permissions</exception>
        public FileWriter(string path,
                 int bufferSize = 81920,
                 FileMode mode = FileMode.Create,
                 FileAccess access = FileAccess.Write,
                 FileShare share = FileShare.Read,
                 FileOptions options = FileOptions.None)
            : this(new FileStream(
                path,
                mode,
                access,
                share,
                bufferSize,
                options
            ), bufferSize, false)
        {
        }

        /// <summary>
        /// Initializes a new instance using an existing file stream.
        /// </summary>
        /// <param name="stream">Pre-opened writable file stream</param>
        /// <param name="bufferSize">Buffer size in bytes (default: 81,920 bytes)</param>
        /// <param name="leaveOpen">Keep stream open after disposal (default: false)</param>
        /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
        /// <exception cref="ArgumentException">Thrown when stream is not writable</exception>
        public FileWriter(FileStream stream, int bufferSize = 81920, bool leaveOpen = false)
        {
            _fileStream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite) throw new ArgumentException("Stream must be writable", nameof(stream));

            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _bufferPosition = 0;
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Writes data to the file using buffered or direct strategy based on size.
        /// </summary>
        /// <param name="data">Data to write as ReadOnlySpan</param>
        /// <remarks>
        /// <para>
        /// Write strategy:
        /// <list type="number">
        /// <item><description>Data smaller than buffer uses buffered writing</description></item>
        /// <item><description>Data larger than buffer uses direct writing</description></item>
        /// <item><description>Very large blocks (>1MB) use memory-mapped I/O</description></item>
        /// </list>
        /// </para>
        /// <para>
        /// Performance characteristics:
        /// <list type="bullet">
        /// <item><description>Zero allocations for buffered writes</description></item>
        /// <item><description>IL-optimized memory copy (faster than Array.Copy)</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public void Write(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return;

            int remaining = data.Length;
            int offset = 0;

            while (remaining > 0)
            {
                int available = _bufferSize - _bufferPosition;
                if (available == 0)
                {
                    Flush();
                    available = _bufferSize;
                }

                int copyBytes = Math.Min(available, remaining);

                unsafe
                {
                    fixed (byte* src = &data[offset])
                    fixed (byte* dest = &_buffer[_bufferPosition])
                    {
                        // Parameter order: destination, source, length
                        _memoryCopier.Copy((IntPtr)dest, (IntPtr)src, copyBytes);
                    }
                }

                _bufferPosition += copyBytes;
                offset += copyBytes;
                remaining -= copyBytes;

                // Direct write for large remaining blocks
                if (remaining > _bufferSize)
                {
                    Flush();
                    WriteDirect(data.Slice(offset, remaining));
                    return;
                }
            }
        }

        /// <summary>
        /// Asynchronously writes data to the file.
        /// </summary>
        /// <param name="data">Data to write as ReadOnlyMemory</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Task representing the async operation</returns>
        /// <inheritdoc cref="Write"/>
        public async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
        {
            if (data.IsEmpty) return;

            int remaining = data.Length;
            int offset = 0;

            while (remaining > 0)
            {
                int available = _bufferSize - _bufferPosition;
                if (available == 0)
                {
                    await FlushAsync(cancellationToken).ConfigureAwait(false);
                    available = _bufferSize;
                }

                int copyBytes = Math.Min(available, remaining);

                unsafe
                {
                    fixed (byte* src = &data.Span[offset])
                    fixed (byte* dest = &_buffer[_bufferPosition])
                    {
                        _memoryCopier.Copy((IntPtr)dest, (IntPtr)src, copyBytes);
                    }
                }

                _bufferPosition += copyBytes;
                offset += copyBytes;
                remaining -= copyBytes;

                // Direct write for large remaining blocks
                if (remaining > 0 && remaining >= available)
                {
                    await FlushAsync(cancellationToken).ConfigureAwait(false);
                    await WriteDirectAsync(
                        data.Slice(offset, remaining),
                        cancellationToken
                    ).ConfigureAwait(false);
                    offset += remaining;
                    remaining = 0;
                }
            }
        }

        /// <summary>
        /// Directly writes large blocks using memory-mapped I/O
        /// </summary>
        private async ValueTask WriteDirectAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
        {
            // Ensure file is large enough
            long requiredLength = _fileStream.Position + data.Length;
            if (_fileStream.Length < requiredLength)
            {
                _fileStream.SetLength(requiredLength);
            }

            // Use memory-mapped file for efficient large writes
            using (var mmFile = MemoryMappedFile.CreateFromFile(
                _fileStream,
                null,
                data.Length,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false))
            {
                using (var accessor = mmFile.CreateViewAccessor(0, data.Length))
                {
                    unsafe
                    {
                        byte* ptr = null;
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                        try
                        {
                            // Copy data directly to mapped memory
                            data.Span.CopyTo(new Span<byte>(ptr, data.Length));
                        }
                        finally
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }

            // Update file position
            _fileStream.Position += data.Length;
            await _fileStream.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Directly writes data without buffering
        /// </summary>
        private void WriteDirect(ReadOnlySpan<byte> data)
        {
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    using var ums = new UnmanagedMemoryStream(ptr, data.Length);
                    ums.CopyTo(_fileStream);
                }
            }
        }

        /// <summary>
        /// Flushes any buffered data to disk
        /// </summary>
        /// <remarks>
        /// Flushing occurs automatically when:
        /// <list type="bullet">
        /// <item><description>Buffer becomes full</description></item>
        /// <item><description>Writing data larger than buffer size</description></item>
        /// <item><description>Disposing the instance</description></item>
        /// </list>
        /// </remarks>
        public void Flush()
        {
            if (_bufferPosition > 0)
            {
                _fileStream.Write(_buffer, 0, _bufferPosition);
                _bufferPosition = 0;
            }
        }

        /// <summary>
        /// Asynchronously flushes any buffered data to disk
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Task representing the async operation</returns>
        /// <inheritdoc cref="Flush"/>
        public async ValueTask FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_bufferPosition > 0)
            {
                await _fileStream.WriteAsync(_buffer, 0, _bufferPosition, cancellationToken)
                    .ConfigureAwait(false);
                _bufferPosition = 0;
            }
        }

        /// <summary>
        /// Releases all resources and flushes remaining data
        /// </summary>
        /// <remarks>
        /// Disposal sequence:
        /// <list type="number">
        /// <item><description>Flush remaining buffer</description></item>
        /// <item><description>Close file stream if leaveOpen is false</description></item>
        /// </list>
        /// </remarks>
        public void Dispose()
        {
            Flush();
            if (!_leaveOpen)
            {
                _fileStream.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously releases all resources and flushes remaining data
        /// </summary>
        /// <returns>ValueTask representing the async operation</returns>
        /// <inheritdoc cref="Dispose"/>
        public async ValueTask DisposeAsync()
        {
            await FlushAsync().ConfigureAwait(false);
            if (!_leaveOpen)
            {
                await _fileStream.DisposeAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Provides IL-optimized memory copying
        /// </summary>
        private sealed class MemoryCopier
        {
            /// <summary>
            /// Delegate for fast memory copying using Cpblk IL instruction
            /// </summary>
            internal delegate void CopyMethod(IntPtr dest, IntPtr src, int count);
            public readonly CopyMethod Copy;

            /// <summary>
            /// Initializes the memory copier with dynamically generated IL
            /// </summary>
            public MemoryCopier()
            {
                var dynamicMethod = new DynamicMethod(
                    "ILMemCopy",
                    null,
                    new[] { typeof(IntPtr), typeof(IntPtr), typeof(int) },
                    typeof(MemoryCopier).Module,
                    true);

                var il = dynamicMethod.GetILGenerator();
                // Parameter order: destination, source, length
                il.Emit(OpCodes.Ldarg_0); // Destination address
                il.Emit(OpCodes.Ldarg_1); // Source address
                il.Emit(OpCodes.Ldarg_2); // Length
                il.Emit(OpCodes.Cpblk);   // Copy block instruction
                il.Emit(OpCodes.Ret);

                Copy = (CopyMethod)dynamicMethod.CreateDelegate(typeof(CopyMethod));
            }
        }
    }
#elif NETSTANDARD2_0
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using System.Threading;
    using System.IO.MemoryMappedFiles;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Provides high-performance buffered file writing for .NET Standard 2.0
    /// </summary>
    /// <remarks>
    /// <para>
    /// Features:
    /// <list type="bullet">
    /// <item><description>Buffered and direct write modes</description></item>
    /// <item><description>Optimized memory copying using Buffer.MemoryCopy</description></item>
    /// <item><description>Async support with cancellation</description></item>
    /// <item><description>Memory-mapped I/O for large writes</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: This implementation uses Buffer.MemoryCopy instead of IL-optimized copying
    /// available in newer framework versions.
    /// </para>
    /// </remarks>
    public sealed class FileWriter : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly byte[] _buffer;
        private int _bufferPosition;
        private readonly int _bufferSize;
        private readonly bool _leaveOpen;

        // Uses Buffer.MemoryCopy for memory operations
        private static readonly MemoryCopier _memoryCopier = new MemoryCopier();

        /// <summary>
        /// Initializes a new instance writing to specified file path
        /// </summary>
        /// <param name="path">Target file path</param>
        /// <param name="bufferSize">Buffer size in bytes (default: 81,920 bytes)</param>
        /// <param name="mode">File creation mode (default: Create)</param>
        /// <param name="access">File access type (default: Write)</param>
        /// <param name="share">File sharing permissions (default: Read)</param>
        /// <param name="options">Advanced file options (default: None)</param>
        /// <exception cref="ArgumentNullException">Thrown when path is null</exception>
        /// <exception cref="IOException">Thrown for file system errors</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown for permission issues</exception>
        public FileWriter(string path,
            int bufferSize = 81920,
            FileMode mode = FileMode.Create,
            FileAccess access = FileAccess.Write,
            FileShare share = FileShare.Read,
            FileOptions options = FileOptions.None)
            : this(new FileStream(
                path,
                mode,
                access,
                share,
                bufferSize,
                options
            ), bufferSize, false)
        {
        }

        /// <summary>
        /// Initializes a new instance using an existing file stream
        /// </summary>
        /// <param name="stream">Pre-opened writable file stream</param>
        /// <param name="bufferSize">Buffer size in bytes (default: 81,920 bytes)</param>
        /// <param name="leaveOpen">Keep stream open after disposal (default: false)</param>
        /// <exception cref="ArgumentNullException">Thrown when stream is null</exception>
        /// <exception cref="ArgumentException">Thrown when stream is not writable</exception>
        public FileWriter(FileStream stream, int bufferSize = 81920, bool leaveOpen = false)
        {
            _fileStream = stream ?? throw new ArgumentNullException(nameof(stream));
            if (!stream.CanWrite) throw new ArgumentException("Stream must be writable", nameof(stream));

            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _bufferPosition = 0;
            _leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Writes data to the file using buffered or direct strategy
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset in data array</param>
        /// <param name="count">Number of bytes to write</param>
        /// <remarks>
        /// <para>
        /// Write strategy:
        /// <list type="number">
        /// <item><description>Buffers data until full</description></item>
        /// <item><description>Auto-flushes full buffer to disk</description></item>
        /// <item><description>Direct writes for data larger than buffer</description></item>
        /// </list>
        /// </para>
        /// </remarks>
        public void Write(byte[] data, int offset, int count)
        {
            if (data == null || count == 0) return;

            int remaining = count;
            int currentOffset = offset;

            while (remaining > 0)
            {
                int available = _bufferSize - _bufferPosition;
                if (available == 0)
                {
                    Flush();
                    available = _bufferSize;
                }

                int copyBytes = Math.Min(available, remaining);

                unsafe
                {
                    fixed (byte* src = &data[currentOffset])
                    fixed (byte* dest = &_buffer[_bufferPosition])
                    {
                        _memoryCopier.Copy(
                            dest: dest,
                            src: src,
                            byteCount: copyBytes
                        );
                    }
                }

                _bufferPosition += copyBytes;
                currentOffset += copyBytes;
                remaining -= copyBytes;

                if (remaining > _bufferSize)
                {
                    Flush();
                    WriteDirect(data, currentOffset, remaining);
                    return;
                }
            }
        }

        /// <summary>
        /// Asynchronously writes data to the file
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset in data array</param>
        /// <param name="count">Number of bytes to write</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing async operation</returns>
        /// <inheritdoc cref="Write"/>
        public async Task WriteAsync(byte[] data, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (data == null || count == 0) return;

            int remaining = count;
            int currentOffset = offset;

            while (remaining > 0)
            {
                int available = _bufferSize - _bufferPosition;
                if (available == 0)
                {
                    await FlushAsync(cancellationToken).ConfigureAwait(false);
                    available = _bufferSize;
                }

                int copyBytes = Math.Min(available, remaining);

                unsafe
                {
                    fixed (byte* src = &data[currentOffset])
                    fixed (byte* dest = &_buffer[_bufferPosition])
                    {
                        _memoryCopier.Copy(
                            dest: dest,
                            src: src,
                            byteCount: copyBytes
                        );
                    }
                }

                _bufferPosition += copyBytes;
                currentOffset += copyBytes;
                remaining -= copyBytes;

                if (remaining > 0 && remaining >= available)
                {
                    await FlushAsync(cancellationToken).ConfigureAwait(false);
                    await WriteDirectAsync(
                        data,
                        currentOffset,
                        remaining,
                        cancellationToken
                    ).ConfigureAwait(false);
                    currentOffset += remaining;
                    remaining = 0;
                }
            }
        }

        /// <summary>
        /// Directly writes large blocks using memory-mapped I/O
        /// </summary>
        private async Task WriteDirectAsync(byte[] data, int offset, int count, CancellationToken cancellationToken)
        {
            long requiredLength = _fileStream.Position + count;
            if (_fileStream.Length < requiredLength)
            {
                _fileStream.SetLength(requiredLength);
            }

            using (var mmFile = MemoryMappedFile.CreateFromFile(
                _fileStream,
                null,
                count,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false))
            {
                using (var accessor = mmFile.CreateViewAccessor(0, count))
                {
                    unsafe
                    {
                        byte* ptr = null;
                        accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
                        try
                        {
                            Marshal.Copy(data, offset, (IntPtr)ptr, count);
                        }
                        finally
                        {
                            accessor.SafeMemoryMappedViewHandle.ReleasePointer();
                        }
                    }
                }
            }

            _fileStream.Position += count;
            await _fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Directly writes data without buffering
        /// </summary>
        private void WriteDirect(byte[] data, int offset, int count)
        {
            _fileStream.Write(data, offset, count);
        }

        /// <summary>
        /// Flushes any buffered data to disk
        /// </summary>
        public void Flush()
        {
            if (_bufferPosition > 0)
            {
                _fileStream.Write(_buffer, 0, _bufferPosition);
                _bufferPosition = 0;
            }
        }

        /// <summary>
        /// Asynchronously flushes any buffered data to disk
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing async operation</returns>
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            if (_bufferPosition > 0)
            {
                await _fileStream.WriteAsync(_buffer, 0, _bufferPosition, cancellationToken)
                    .ConfigureAwait(false);
                _bufferPosition = 0;
            }
        }

        /// <summary>
        /// Releases all resources and flushes remaining data
        /// </summary>
        public void Dispose()
        {
            Flush();
            if (!_leaveOpen)
            {
                _fileStream.Dispose();
            }
        }

        /// <summary>
        /// Asynchronously releases all resources and flushes remaining data
        /// </summary>
        /// <returns>Task representing async operation</returns>
        public async Task DisposeAsync()
        {
            await FlushAsync().ConfigureAwait(false);
            if (!_leaveOpen)
            {
                _fileStream.Dispose();
            }
        }

        /// <summary>
        /// Provides memory copying using Buffer.MemoryCopy
        /// </summary>
        private sealed class MemoryCopier
        {
            /// <summary>
            /// Copies memory using Buffer.MemoryCopy
            /// </summary>
            /// <param name="dest">Destination pointer</param>
            /// <param name="src">Source pointer</param>
            /// <param name="byteCount">Number of bytes to copy</param>
            public unsafe void Copy(byte* dest, byte* src, int byteCount)
            {
                Buffer.MemoryCopy(
                    source: src,
                    destination: dest,
                    destinationSizeInBytes: byteCount,
                    sourceBytesToCopy: byteCount
                );
            }
        }
    }
#endif
}

