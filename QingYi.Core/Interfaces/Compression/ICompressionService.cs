using QingYi.Core.Compression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QingYi.Core.Interfaces.Compression
{
    /// <summary>
    /// 压缩服务接口（简化API）
    /// </summary>
    public interface ICompressionService
    {
        /// <summary>
        /// 压缩字符串
        /// </summary>
        ValueTask<string> CompressStringAsync(string text, CompressionAlgorithm algorithm);

        /// <summary>
        /// 解压字符串
        /// </summary>
        ValueTask<string> DecompressStringAsync(string compressedText, CompressionAlgorithm algorithm);

        /// <summary>
        /// 压缩文件
        /// </summary>
        ValueTask CompressFileAsync(string sourceFile, string destinationFile, CompressionAlgorithm algorithm);

        /// <summary>
        /// 解压文件
        /// </summary>
        ValueTask DecompressFileAsync(string sourceFile, string destinationFile, CompressionAlgorithm algorithm);

        /// <summary>
        /// 批量压缩文件
        /// </summary>
        IAsyncEnumerable<FileCompressionResult> CompressFilesAsync(
            IEnumerable<string> sourceFiles,
            string outputDirectory,
            CompressionAlgorithm algorithm);
    }
}
