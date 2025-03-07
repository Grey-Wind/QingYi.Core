using System;

namespace QingYi.Core.FileUtility.GetFileInfo
{
    /// <summary>
    /// Provides functionality to retrieve the creation time of a file.
    /// </summary>
    public class FileCreationTime
    {
        /// <summary>
        /// Retrieves the creation time of the file specified by the provided file path.
        /// </summary>
        /// <param name="filePath">The path to the file for which the creation time is to be retrieved.</param>
        /// <returns>A <see cref="DateTime"/> representing the creation time of the file.</returns>
        public static DateTime Get(string filePath)
        {
            Select select = new Select();

            var result = select.SelectFile(filePath);

            DateTime dateTime = result.Item4;

            return dateTime;
        }
    }
}
