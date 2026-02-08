using QingYi.Core.Compression;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Interfaces.Compression
{
    /// <summary>
    /// 压缩提供程序接口
    /// </summary>
    public interface ICompressionProvider
    {
        /// <summary>
        /// 压缩级别
        /// </summary>
        CompressionLevel Level { get; set; }

        /// <summary>
        /// 压缩算法类型
        /// </summary>
        CompressionAlgorithm Algorithm { get; }

        /// <summary>
        /// 异步压缩字节数组
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>压缩后的数据</returns>
        ValueTask<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步解压字节数组
        /// </summary>
        /// <param name="compressedData">压缩数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>解压后的数据</returns>
        ValueTask<byte[]> DecompressAsync(byte[] compressedData, CancellationToken cancellationToken = default);

        /// <summary>
        /// 流式压缩
        /// </summary>
        /// <param name="inputStream">输入流</param>
        /// <param name="outputStream">输出流</param>
        /// <param name="cancellationToken">取消令牌</param>
        ValueTask CompressStreamAsync(Stream inputStream, Stream outputStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// 流式解压
        /// </summary>
        /// <param name="inputStream">输入流</param>
        /// <param name="outputStream">输出流</param>
        /// <param name="cancellationToken">取消令牌</param>
        ValueTask DecompressStreamAsync(Stream inputStream, Stream outputStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// 使用ArrayPool压缩（高性能场景）
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含压缩数据和实际长度的结果</returns>
        ValueTask<MemoryPoolResult> CompressWithMemoryPoolAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

        /// <summary>
        /// 使用ArrayPool解压（高性能场景）
        /// </summary>
        /// <param name="compressedData">压缩数据</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>包含解压数据和实际长度的结果</returns>
        ValueTask<MemoryPoolResult> DecompressWithMemoryPoolAsync(ReadOnlyMemory<byte> compressedData, CancellationToken cancellationToken = default);
    }
}
