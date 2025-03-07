namespace QingYi.Core.FileUtility.GetFileInfo
{
    /// <summary>
    /// Provides functionality to retrieve the extension of a file.
    /// </summary>
    public class FileExtension
    {
        /// <summary>
        /// Retrieves the extension of the file specified by the provided file path.
        /// </summary>
        /// <param name="filePath">The path to the file for which the extension is to be retrieved.</param>
        /// <returns>A <see cref="string"/> representing the extension of the file, including the dot (e.g., ".txt", ".jpg").</returns>
        public static string Get(string filePath)
        {
            Select select = new Select();

            var result = select.SelectFile(filePath);

            string fileExtension = result.Item2;

            return fileExtension;
        }
    }
}
