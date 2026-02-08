using QingYi.Core.Interfaces;
using System.Buffers;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Compression
{
    ///<summary>
    /// 压缩算法抽象基类
    /// </summary>
    public abstract class CompressionBase : ICompression
    {
        private const int DefaultBufferSize = 81920; // 80KB
        private CompressionLevel _level = CompressionLevel.Optimal;

        public abstract string AlgorithmName { get; }

        public CompressionLevel Level
        {
            get => _level;
            set => _level = value;
        }

        public abstract byte[] Compress(byte[] input);
        public abstract byte[] Decompress(byte[] compressedData);

        public abstract void CompressStream(Stream inputStream, Stream outputStream);
        public abstract void DecompressStream(Stream compressedStream, Stream outputStream);

        public virtual async ValueTask<byte[]> CompressAsync(byte[] input, CancellationToken cancellationToken = default)
        {
            await using var inputStream = new MemoryStream(input);
            await using var outputStream = new MemoryStream();

            await CompressStreamAsync(inputStream, outputStream, cancellationToken);
            return outputStream.ToArray();
        }

        public virtual async ValueTask<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default)
        {
            await using var inputStream = new MemoryStream(compressedData);
            await using var outputStream = new MemoryStream();

            await DecompressStreamAsync(inputStream, outputStream, cancellationToken);
            return outputStream.ToArray();
        }

        public virtual async ValueTask CompressStreamAsync(
            Stream inputStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            using var compressionStream = CreateCompressionStream(outputStream);
            await inputStream.CopyToAsync(compressionStream, DefaultBufferSize, cancellationToken);
        }

        public virtual async ValueTask DecompressStreamAsync(
            Stream compressedStream,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            using var decompressionStream = CreateDecompressionStream(compressedStream);
            await decompressionStream.CopyToAsync(outputStream, DefaultBufferSize, cancellationToken);
        }

        /// <summary>
        /// 创建压缩流
        /// </summary>
        protected abstract Stream CreateCompressionStream(Stream outputStream);

        /// <summary>
        /// 创建解压流
        /// </summary>
        protected abstract Stream CreateDecompressionStream(Stream inputStream);

        /// <summary>
        /// 辅助方法：从池中获取缓冲区
        /// </summary>
        protected static byte[] RentBuffer(int size = DefaultBufferSize)
        {
            return ArrayPool<byte>.Shared.Rent(size);
        }

        /// <summary>
        /// 辅助方法：归还缓冲区到池
        /// </summary>
        protected static void ReturnBuffer(byte[] buffer)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
