using System;

namespace QingYi.Core.FileUtility.GetFileInfo
{
    /// <summary>
    /// Provides functionality to retrieve the last access time of a file.
    /// </summary>
    public class FileLastAccessTime
    {
        /// <summary>
        /// Retrieves the last access time of the file specified by the provided file path.
        /// </summary>
        /// <param name="filePath">The path to the file for which the last access time is to be retrieved.</param>
        /// <returns>A <see cref="DateTime"/> representing the last access time of the file.</returns>
        public static DateTime Get(string filePath)
        {
            Select select = new Select();

            var result = select.SelectFile(filePath);

            DateTime dateTime = result.Item5;

            return dateTime;
        }
    }
}
