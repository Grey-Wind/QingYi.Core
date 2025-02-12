using System.IO;
using System.IO.Compression;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// A utility class for archiving files and folders into zip archives.<br />
    /// 用于将文件和文件夹归档到zip档案中的工具类。
    /// </summary>
    public struct Zip
    {
        /// <summary>
        /// Archives a single file into a zip file. <br />
        /// 将单个文件归档为zip文件。
        /// </summary>
        /// <param name="sourceFile">The path of the source file to be archived. <br />要归档的源文件路径。</param>
        public static void ArchiveFile(string sourceFile)
        {
            string zipFile = Path.Combine(Path.GetDirectoryName(sourceFile), Path.GetFileName(sourceFile) + ".zip");

            using FileStream fs = new FileStream(zipFile, FileMode.Create);
            using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(sourceFile, Path.GetFileName(sourceFile));
        }

        /// <summary>
        /// Archives a single file into a zip file with a specified zip file name.<br />
        /// 使用指定的zip文件名将单个文件归档为zip文件。
        /// </summary>
        /// <param name="sourceFile">The path of the source file to be archived.<br />要归档的源文件路径。</param>
        /// <param name="zipFile">The path where the zip file will be created.<br />zip文件将创建到的路径。</param>
        public static void ArchiveFile(string sourceFile, string zipFile)
        {
            using FileStream fs = new FileStream(zipFile, FileMode.Create);
            using ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(sourceFile, Path.GetFileName(sourceFile));
        }

        /// <summary>
        /// Archives an entire folder into a zip file.<br /> 
        /// 将整个文件夹归档为zip文件。
        /// </summary>
        /// <param name="sourceFolder">The path of the source folder to be archived.<br />要归档的源文件夹路径。</param>
        public static void ArchiveFolder(string sourceFolder)
        {
            string zipFile = Path.Combine(Path.GetDirectoryName(sourceFolder), Path.GetFileName(sourceFolder) + ".zip");

            ZipFile.CreateFromDirectory(sourceFolder, zipFile, CompressionLevel.Optimal, true);
        }

        /// <summary>
        /// Archives an entire folder into a zip file with a specified compression level.<br />
        /// 使用指定的压缩级别将整个文件夹归档为zip文件。
        /// </summary>
        /// <param name="sourceFolder">The path of the source folder to be archived.<br />要归档的源文件夹路径。</param>
        /// <param name="compressionLevel">The compression level to be used when creating the zip file.<br />创建zip文件时使用的压缩级别。</param>
        public static void ArchiveFolder(string sourceFolder, CompressionLevel compressionLevel)
        {
            string zipFile = Path.Combine(Path.GetDirectoryName(sourceFolder), Path.GetFileName(sourceFolder) + ".zip");

            ZipFile.CreateFromDirectory(sourceFolder, zipFile, compressionLevel, true);
        }

        /// <summary>
        /// Archives an entire folder into a zip file with a specified zip file name. <br />
        /// 使用指定的zip文件名将整个文件夹归档为zip文件。
        /// </summary>
        /// <param name="sourceFolder">The path of the source folder to be archived.<br />要归档的源文件夹路径。</param>
        /// <param name="zipFile">The path where the zip file will be created.<br />zip文件将创建到的路径。</param>
        public static void ArchiveFolder(string sourceFolder, string zipFile) => ZipFile.CreateFromDirectory(sourceFolder, zipFile, CompressionLevel.Optimal, true);

        /// <summary>
        /// Archives an entire folder into a zip file with a specified zip file name and compression level.<br />
        /// 使用指定的zip文件名和压缩级别将整个文件夹归档为zip文件。
        /// </summary>
        /// <param name="sourceFolder">The path of the source folder to be archived.<br />要归档的源文件夹路径。</param>
        /// <param name="zipFile">The path where the zip file will be created.<br />zip文件将创建到的路径。</param>
        /// <param name="compressionLevel">The compression level to be used when creating the zip file.<br />创建zip文件时使用的压缩级别。</param>
        public static void ArchiveFolder(string sourceFolder, string zipFile, CompressionLevel compressionLevel) => ZipFile.CreateFromDirectory(sourceFolder, zipFile, compressionLevel, true);
    }
}
