using QingYi.Core.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Interfaces.Compression
{
    /// <summary>
    /// 高级压缩接口（支持分块压缩、加密等）
    /// </summary>
    public interface IAdvancedCompressionProvider : ICompressionProvider
    {
        /// <summary>
        /// 分块压缩
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="chunkSize">块大小</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>压缩后的块列表</returns>
        IAsyncEnumerable<CompressionChunk> CompressChunkedAsync(
            byte[] data,
            int chunkSize = 1024 * 1024, // 默认1MB
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 分块解压
        /// </summary>
        /// <param name="chunks">压缩块</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>解压后的数据</returns>
        ValueTask<byte[]> DecompressChunkedAsync(
            IAsyncEnumerable<CompressionChunk> chunks,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 带加密的压缩
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="encryptionKey">加密密钥</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>压缩并加密的数据</returns>
        ValueTask<byte[]> CompressWithEncryptionAsync(
            byte[] data,
            ReadOnlyMemory<byte> encryptionKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// 带加密的解压
        /// </summary>
        /// <param name="encryptedCompressedData">加密的压缩数据</param>
        /// <param name="decryptionKey">解密密钥</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>解压后的数据</returns>
        ValueTask<byte[]> DecompressWithDecryptionAsync(
            byte[] encryptedCompressedData,
            ReadOnlyMemory<byte> decryptionKey,
            CancellationToken cancellationToken = default);
    }
}
