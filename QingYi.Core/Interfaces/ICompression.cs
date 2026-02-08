using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Interfaces
{
    /// <summary>
    /// 压缩算法接口
    /// </summary>
    public interface ICompression
    {
        /// <summary>
        /// 算法名称（如：GZip, Brotli, Deflate, LZ4等）
        /// </summary>
        string AlgorithmName { get; }

        /// <summary>
        /// 压缩等级
        /// </summary>
        CompressionLevel Level { get; set; }

        /// <summary>
        /// 压缩数据
        /// </summary>
        /// <param name="input">输入数据</param>
        /// <returns>压缩后的数据</returns>
        byte[] Compress(byte[] input);

        /// <summary>
        /// 解压数据
        /// </summary>
        /// <param name="compressedData">压缩数据</param>
        /// <returns>解压后的原始数据</returns>
        byte[] Decompress(byte[] compressedData);

        /// <summary>
        /// 流式压缩
        /// </summary>
        /// <param name="inputStream">输入流</param>
        /// <param name="outputStream">输出流</param>
        void CompressStream(Stream inputStream, Stream outputStream);

        /// <summary>
        /// 流式解压
        /// </summary>
        /// <param name="compressedStream">压缩流</param>
        /// <param name="outputStream">输出流</param>
        void DecompressStream(Stream compressedStream, Stream outputStream);

        /// <summary>
        /// 异步压缩数据
        /// </summary>
        ValueTask<byte[]> CompressAsync(byte[] input, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步解压数据
        /// </summary>
        ValueTask<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步流式压缩
        /// </summary>
        ValueTask CompressStreamAsync(Stream inputStream, Stream outputStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步流式解压
        /// </summary>
        ValueTask DecompressStreamAsync(Stream compressedStream, Stream outputStream, CancellationToken cancellationToken = default);
    }
}
