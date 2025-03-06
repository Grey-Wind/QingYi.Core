using System.IO;

namespace QingYi.Core.FileUtility
{
    /// <summary>
    /// <b>Windows Only!</b><br />
    /// Operation class for the Temp folder.<br />
    /// 针对Temp文件夹的操作类。
    /// </summary>
    public class Temp
    {
        /// <summary>
        /// Temp folder path.<br />
        /// Temp 文件夹路径。
        /// </summary>
        public static string TempPath { get; }

        /// <summary>
        /// Get the Temp folder<br/>
        /// 获取Temp文件夹
        /// </summary>
        /// <returns>
        /// Temp folder<br/>
        /// Temp文件夹路径
        /// </returns>
        public static string Get()
        {
            return Path.GetTempPath();
        }

        /// <summary>
        /// Create a new file within the Temp folder<br />
        /// 在Temp文件夹内创建新的文件
        /// </summary>
        /// <param name="fileName">
        /// The name of the new file you want to create<br />
        /// 想要创建的文件名称
        /// </param>
        /// <returns>
        /// The full path to the newly created file<br />
        /// 新创建的文件的完整路径
        /// </returns>
        public static string CreateFile(string fileName)
        {
            string filePath = Path.Combine(Get(), fileName);

            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }

            return filePath;
        }

        /// <summary>
        /// Creates a new folder with the specified name and returns the full path to the created folder.
        /// <br/>创建指定名称的新文件夹，并返回创建的文件夹的完整路径。
        /// </summary>
        /// <param name="newFolderName">The name of the new folder to create.<br/>要创建的新文件夹的名称。</param>
        /// <returns>The full path to the newly created folder.<br/>新创建的文件夹的完整路径。</returns>
        public static string CreateFolder(string newFolderName)
        {
            string name = Path.Combine(Get(), newFolderName);
            Directory.CreateDirectory(name);

            return name;
        }

        static Temp()
        {
            TempPath = Get();
        }
    }
}
