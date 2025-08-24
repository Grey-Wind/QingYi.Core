#if !NETFRAMEWORK && NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER || NETCOREAPP
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.FileUtility.IO
{
    /// <summary>
    /// Provides static convenience methods for simplified file writing operations.
    /// </summary>
    /// <remarks>
    /// This static class offers simplified APIs for common file writing scenarios,
    /// internally using the more feature-rich <see cref="FileWriter"/> class.
    /// </remarks>
    public static class FileWriterStatic
    {
#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        /// <summary>
        /// Writes data to a file in a single operation (synchronous).
        /// </summary>
        /// <param name="data">The byte array containing data to write.</param>
        /// <param name="filePath">The path of the file to write to.</param>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item><description>Creates a new FileWriter instance</description></item>
        /// <item><description>Writes all data at once</description></item>
        /// <item><description>Automatically disposes resources</description></item>
        /// </list>
        /// For more control over writing options, use <see cref="FileWriter"/> directly.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if data or filePath is null.</exception>
        /// <exception cref="IOException">Thrown for file system errors.</exception>
        public static void Write(byte[] data, string filePath)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            // Create and immediately use FileWriter with using declaration
            using var writer = new FileWriter(filePath);
            writer.Write(data.AsSpan());
        }

        /// <summary>
        /// Asynchronously writes data to a file in a single operation.
        /// </summary>
        /// <param name="data">The byte array containing data to write.</param>
        /// <param name="filePath">The path of the file to write to.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <inheritdoc cref="Write"/>
        public static async Task WriteAsync(byte[] data, string filePath)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));

            // Create and immediately use FileWriter with await using pattern
            await using var writer = new FileWriter(filePath);
            await writer.WriteAsync(data.AsMemory());
        }

#else
        /// <summary>
        /// Writes data to a file in a single operation (synchronous).
        /// </summary>
        /// <param name="data">The byte array containing data to write.</param>
        /// <param name="filename">The path of the file to write to.</param>
        /// <remarks>
        /// This .NET Standard 2.0 version uses traditional using blocks rather than
        /// using declarations for broader compatibility.
        /// </remarks>
        /// <inheritdoc cref="Write"/>
        public static void Write(byte[] data, string filename)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            using (var writer = new FileWriter(filename))
            {
                writer.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        /// Asynchronously writes data to a file in a single operation.
        /// </summary>
        /// <param name="data">The byte array containing data to write.</param>
        /// <param name="filename">The path of the file to write to.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Note: Explicit DisposeAsync call is needed in NETSTANDARD2_0 since
        /// await using declarations aren't available.
        /// </remarks>
        /// <inheritdoc cref="WriteAsync"/>
        public static async Task WriteAsync(byte[] data, string filename)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (filename == null) throw new ArgumentNullException(nameof(filename));

            using (var writer = new FileWriter(filename))
            {
                await writer.WriteAsync(data, 0, data.Length, CancellationToken.None);
                await writer.DisposeAsync();
            }
        }
#endif
    }
}
#endif
