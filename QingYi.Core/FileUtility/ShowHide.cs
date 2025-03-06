using System;
using System.IO;

namespace QingYi.Core.FileUtility
{
    /// <summary>
    /// <b>Windows Only!</b><br />
    /// Provides ways to show and hide files and folders.<br />
    /// 提供显示和隐藏文件及文件夹的方法。
    /// </summary>
    public struct ShowHide
    {
        /// <summary>
        /// Displays the specified file.<br />
        /// 显示指定文件。
        /// </summary>
        /// <param name="filePath">要显示的文件路径。<br />File path to display.</param>
        /// <exception cref="Exception">如果文件不存在或文件已显示，抛出异常。<br />If the file does not exist or is already displayed, throw an exception.</exception>
        /// <remarks>该方法会检查文件是否存在，并确保文件未被隐藏。如果文件已显示，则会抛出异常。<br />This method checks if the file exists and makes sure it is not hidden. If the file is already displayed, an exception is thrown.</remarks>
        public static void ShowFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new Exception("File not found.");
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(filePath);

                    switch (attributes & FileAttributes.Hidden)
                    {
                        case FileAttributes.Hidden:
                            File.SetAttributes(filePath, attributes & ~FileAttributes.Hidden);
                            break;
                        default:
                            throw new Exception("The file is already show");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Show file error: {e.Message}");
            }
        }

        /// <summary>
        /// Hide the specified file.<br />
        /// 隐藏指定文件。
        /// </summary>
        /// <param name="filePath">要隐藏的文件路径。<br />File path to hide.</param>
        /// <exception cref="Exception">如果文件不存在或文件已隐藏，抛出异常。<br />If the file does not exist or is hidden, throw an exception.</exception>
        /// <remarks>该方法会检查文件是否存在，并确保文件未被隐藏。如果文件已隐藏，则会抛出异常。<br />This method checks if the file exists and makes sure it is not hidden. If the file is hidden, an exception is thrown.</remarks>
        public static void HideFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new Exception("File not found.");
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(filePath);

                    switch (attributes & FileAttributes.Hidden)
                    {
                        case FileAttributes.Hidden:
                            throw new Exception("File is already hide.");
                        default:
                            File.SetAttributes(filePath, attributes | FileAttributes.Hidden);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Catch error when hide the file: {e.Message}");
            }
        }

        /// <summary>
        /// Displays the specified folder.<br />
        /// 显示指定文件夹。
        /// </summary>
        /// <param name="folderPath">要显示的文件夹路径。<br />The folder path to display.</param>
        /// <exception cref="Exception">如果文件夹不存在或文件夹已显示，抛出异常。<br />If the folder does not exist or is already displayed, throw an exception.</exception>
        /// <remarks>该方法会检查文件夹是否存在，并确保文件夹未被隐藏。如果文件夹已显示，则会抛出异常。<br />This method checks whether the folder exists and ensures that the folder is not hidden. If the folder is already displayed, an exception is thrown.</remarks>
        public static void ShowFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    throw new Exception("Folder not found.");
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(folderPath);

                    switch (attributes & FileAttributes.Hidden)
                    {
                        case FileAttributes.Hidden:
                            File.SetAttributes(folderPath, attributes & ~FileAttributes.Hidden);
                            break;
                        default:
                            throw new Exception("The folder is already displayed.");
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error displaying folder: {e.Message}");
            }
        }

        /// <summary>
        /// Hide the specified folder.<br />
        /// 隐藏指定文件夹。
        /// </summary>
        /// <param name="folderPath">要隐藏的文件夹路径。<br />Folder path to hide.</param>
        /// <exception cref="Exception">如果文件夹不存在或文件夹已隐藏，抛出异常。<br />If the folder does not exist or the folder is hidden, throw an exception.</exception>
        /// <remarks>该方法会检查文件夹是否存在，并确保文件夹未被隐藏。如果文件夹已隐藏，则会抛出异常。<br />This method checks whether the folder exists and ensures that the folder is not hidden. If the folder is hidden, an exception is thrown.</remarks>
        public static void HideFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    throw new Exception("Folder does not exist.");
                }
                else
                {
                    FileAttributes attributes = File.GetAttributes(folderPath);

                    switch (attributes & FileAttributes.Hidden)
                    {
                        case FileAttributes.Hidden:
                            throw new Exception("The folder is already hidden.");
                        default:
                            File.SetAttributes(folderPath, attributes | FileAttributes.Hidden);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error hiding folder: {e.Message}");
            }
        }
    }
}
