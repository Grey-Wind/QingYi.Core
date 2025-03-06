using System.IO;
using System.IO.Compression;

namespace QingYi.Core.Compression
{
    public struct Gz
    {
#if NET6_0_OR_GREATER
        // 最小压缩

        public static void ArchiveFile(string sourceFile)
        {
            string gzFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".gz");

            using FileStream sourceStream = new(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new(destinationStream, CompressionLevel.SmallestSize);
            sourceStream.CopyTo(compressionStream);
        }

        public static void ArchiveFile(string sourceFile, string gzFile)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, CompressionLevel.SmallestSize);
            sourceStream.CopyTo(compressionStream);
        }
#else
        // 低版本使用普通压缩

        public static void ArchiveFile(string sourceFile)
        {
            string gzFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".gz");

            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, CompressionLevel.Optimal);
            sourceStream.CopyTo(compressionStream);
        }

        public static void ArchiveFile(string sourceFile, string gzFile)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, CompressionLevel.Optimal);
            sourceStream.CopyTo(compressionStream);
        }
#endif

        public static void ArchiveFile(string sourceFile, CompressionLevel compressionLevel)
        {
            string gzFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".gz");

            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, compressionLevel);
            sourceStream.CopyTo(compressionStream);
        }

        public static void ArchiveFile(string sourceFile, string gzFile, CompressionLevel compressionLevel)
        {
            using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
            using FileStream destinationStream = new FileStream(gzFile, FileMode.Create, FileAccess.Write);
            using GZipStream compressionStream = new GZipStream(destinationStream, compressionLevel);
            sourceStream.CopyTo(compressionStream);
        }
    }
}
