using System.IO;
using System.IO.Compression;

namespace QingYi.Core.Compression
{
    public struct Zip
    {
        public static void ArchiveFile(string sourceFile)
        {
            string zipFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".zip");

            using FileStream fs = new FileStream(zipFile, FileMode.Create);
            using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(sourceFile, Path.GetFileName(sourceFile));
        }

        public static void ArchiveFile(string sourceFile, string zipFile)
        {
            using FileStream fs = new FileStream(zipFile, FileMode.Create);
            using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(sourceFile, Path.GetFileName(sourceFile));
        }

        public static void ArchiveFolder(string sourceFolder)
        {
            string zipFile = Path.Combine(Path.GetDirectoryName(sourceFolder), Path.GetFileName(sourceFolder) + ".zip");

            ZipFile.CreateFromDirectory(sourceFolder, zipFile, CompressionLevel.Optimal, true);
        }

        public static void ArchiveFolder(string sourceFolder, CompressionLevel compressionLevel)
        {
            string zipFile = Path.Combine(Path.GetDirectoryName(sourceFolder), Path.GetFileName(sourceFolder) + ".zip");

            ZipFile.CreateFromDirectory(sourceFolder, zipFile, compressionLevel, true);
        }

        public static void ArchiveFolder(string sourceFolder, string zipFile)
        {
            ZipFile.CreateFromDirectory(sourceFolder, zipFile, CompressionLevel.Optimal, true);
        }

        public static void ArchiveFolder(string sourceFolder, string zipFile, CompressionLevel compressionLevel)
        {
            ZipFile.CreateFromDirectory(sourceFolder, zipFile, compressionLevel, true);
        }
    }
}
