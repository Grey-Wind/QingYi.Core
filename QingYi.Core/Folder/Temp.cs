using System.IO;

namespace Folder
{
    public class Temp
    {
        /// <summary>
        /// Get the Temp folder<br></br>
        /// 获取Temp文件夹
        /// </summary>
        /// <returns>
        /// Temp folder<br></br>
        /// Temp文件夹路径
        /// </returns>
        public static string Get()
        {
            return Path.GetTempPath();
        }

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
        /// Create a new subfolder within the Temp folder<br></br>
        /// 在Temp文件夹内创建新的子文件夹
        /// </summary>
        /// <param name="newFolderName">
        /// The name of the new folder you want to create<br></br>
        /// 想要创建的新文件夹名称
        /// </param>
        public static string CreateFolder(string newFolderName)
        {
            string name = Path.Combine(Get(), newFolderName);
            Directory.CreateDirectory(name);

            return name;
        }
    }
}
