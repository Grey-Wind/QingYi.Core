using QingYi.Core.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Compression
{
    public static class CompressionExtensions
    {
        /// <summary>
        /// 将字符串压缩为Base64
        /// </summary>
        public static string ToBase64(this ICompression compression, string text, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var bytes = compression.Compress(encoding.GetBytes(text));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// 从Base64解压字符串
        /// </summary>
        public static string DecodeFromBase64(this ICompression compression, string base64Text, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            var bytes = Convert.FromBase64String(base64Text);
            var decompressed = compression.Decompress(bytes);
            return encoding.GetString(decompressed);
        }

        /// <summary>
        /// 压缩到文件
        /// </summary>
        public static void CompressToFile(
            this ICompression compression,
            Stream inputStream,
            string filePath)
        {
            using var fileStream = File.Create(filePath);
            compression.CompressStream(inputStream, fileStream);
        }

        /// <summary>
        /// 压缩到文件
        /// </summary>
        public static async Task CompressToFileAsync(
            this ICompression compression,
            Stream inputStream,
            string filePath,
            CancellationToken cancellationToken = default)
        {
            await using var fileStream = File.Create(filePath);
            await compression.CompressStreamAsync(inputStream, fileStream, cancellationToken);
        }

        /// <summary>
        /// 从文件解压
        /// </summary>
        public static void DecompressFromFile(
            this ICompression compression,
            string filePath,
            Stream outputStream)
        {
            using var fileStream = File.OpenRead(filePath);
            compression.DecompressStream(fileStream, outputStream);
        }

        /// <summary>
        /// 从文件解压
        /// </summary>
        public static async Task DecompressFromFileAsync(
            this ICompression compression,
            string filePath,
            Stream outputStream,
            CancellationToken cancellationToken = default)
        {
            await using var fileStream = File.OpenRead(filePath);
            await compression.DecompressStreamAsync(fileStream, outputStream, cancellationToken);
        }
    }
}
