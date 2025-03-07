namespace QingYi.Core.FileUtility.GetFileInfo
{
    /// <summary>
    /// Provides functionality to retrieve the size of a file.
    /// </summary>
    public class FileSize
    {
        /// <summary>
        /// Retrieves the size of the file specified by the provided file path.
        /// </summary>
        /// <param name="filePath">The path to the file for which the size is to be retrieved.</param>
        /// <returns>A <see cref="long"/> representing the size of the file in bytes.</returns>
        public static long Get(string filePath)
        {
            Select select = new Select();

            var result = select.SelectFile(filePath);

            long fileSize = result.Item3;

            return fileSize;
        }
    }
}
