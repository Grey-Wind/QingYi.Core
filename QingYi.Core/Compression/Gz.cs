using System.IO;
using System.IO.Compression;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 用于将文件归档到gz压缩包中的工具类。<br />
    /// A utility class for archiving files to the gz zip package.
    /// </summary>
    public struct Gz
    {
#if NET6_0_OR_GREATER
        // 最小压缩
        /// <summary>
        /// Archive the file to the file path.
        /// </summary>
        /// <param name="sourceFile">Source file.</param>
        public static void ArchiveFile(string sourceFile)
        {
            string gzFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".gz");

            using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new(destinationStream, CompressionLevel.SmallestSize);
            sourceStream.CopyTo(compressionStream);
        }

        /// <summary>
        /// Archive the file to the Gzip file path.
        /// </summary>
        /// <param name="sourceFile">Source file.</param>
        /// <param name="gzFile">Gzip file path</param>
        public static void ArchiveFile(string sourceFile, string gzFile)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, CompressionLevel.SmallestSize);
            sourceStream.CopyTo(compressionStream);
        }
#else
        // 低版本使用普通压缩
        /// <summary>
        /// Archive the file to the file path.
        /// </summary>
        /// <param name="sourceFile">Source file.</param>
        public static void ArchiveFile(string sourceFile)
        {
            string gzFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".gz");

            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, CompressionLevel.Optimal);
            sourceStream.CopyTo(compressionStream);
        }

        /// <summary>
        /// Archive the file to the Gzip file path.
        /// </summary>
        /// <param name="sourceFile">Source file.</param>
        /// <param name="gzFile">Gzip file path</param>
        public static void ArchiveFile(string sourceFile, string gzFile)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, CompressionLevel.Optimal);
            sourceStream.CopyTo(compressionStream);
        }
#endif
        /// <summary>
        /// Archive the file to the file path.
        /// </summary>
        /// <param name="sourceFile">Source file.</param>
        /// <param name="compressionLevel">Compression level</param>
        public static void ArchiveFile(string sourceFile, CompressionLevel compressionLevel)
        {
            string gzFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".gz");

            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, compressionLevel);
            sourceStream.CopyTo(compressionStream);
        }

        /// <summary>
        /// Archive the file to the Gzip file path.
        /// </summary>
        /// <param name="sourceFile">Source file.</param>
        /// <param name="gzFile">Gzip file path</param>
        /// <param name="compressionLevel">Compression level</param>
        public static void ArchiveFile(string sourceFile, string gzFile, CompressionLevel compressionLevel)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, compressionLevel);
            sourceStream.CopyTo(compressionStream);
        }
    }
}
