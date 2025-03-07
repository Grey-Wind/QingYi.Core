using System;

namespace QingYi.Core.FileUtility.GetFileInfo
{
    /// <summary>
    /// Provides functionality to retrieve the last write time of a file.
    /// </summary>
    public class FileLastWriteTime
    {
        /// <summary>
        /// Retrieves the last write time of the file specified by the provided file path.
        /// </summary>
        /// <param name="filePath">The path to the file for which the last write time is to be retrieved.</param>
        /// <returns>A <see cref="DateTime"/> representing the last write time of the file.</returns>
        public static DateTime Get(string filePath)
        {
            Select select = new Select();

            var result = select.SelectFile(filePath);

            DateTime dateTime = result.Item6;

            return dateTime;
        }
    }
}
