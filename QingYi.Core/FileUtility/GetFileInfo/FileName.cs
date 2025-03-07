namespace QingYi.Core.FileUtility.GetFileInfo
{
    /// <summary>
    /// Provides functionality to retrieve the name of a file.
    /// </summary>
    public class FileName
    {
        /// <summary>
        /// Retrieves the name of the file specified by the provided file path.
        /// </summary>
        /// <param name="filePath">The path to the file for which the name is to be retrieved.</param>
        /// <returns>A <see cref="string"/> representing the name of the file (without extension).</returns>
        public static string Get(string filePath)
        {
            Select select = new Select();

            var result = select.SelectFile(filePath);

            string fileName = result.Item1;

            return fileName;
        }
    }
}
