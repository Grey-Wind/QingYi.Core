#if WINDOWS
using System;
using System.IO;

namespace QingYi.Core.FileUtility
{
    /// <summary>
    /// 提供显示和隐藏文件及文件夹的方法。
    /// </summary>
    public struct ShowHide
    {
        /// <summary>
        /// 显示指定文件。
        /// </summary>
        /// <param name="filePath">要显示的文件路径。</param>
        /// <exception cref="Exception">如果文件不存在或文件已显示，抛出异常。</exception>
        /// <remarks>该方法会检查文件是否存在，并确保文件未被隐藏。如果文件已显示，则会抛出异常。</remarks>
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
        /// 隐藏指定文件。
        /// </summary>
        /// <param name="filePath">要隐藏的文件路径。</param>
        /// <exception cref="Exception">如果文件不存在或文件已隐藏，抛出异常。</exception>
        /// <remarks>该方法会检查文件是否存在，并确保文件未被隐藏。如果文件已隐藏，则会抛出异常。</remarks>
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
        /// 显示指定文件夹。
        /// </summary>
        /// <param name="folderPath">要显示的文件夹路径。</param>
        /// <exception cref="Exception">如果文件夹不存在或文件夹已显示，抛出异常。</exception>
        /// <remarks>该方法会检查文件夹是否存在，并确保文件夹未被隐藏。如果文件夹已显示，则会抛出异常。</remarks>
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
        /// 隐藏指定文件夹。
        /// </summary>
        /// <param name="folderPath">要隐藏的文件夹路径。</param>
        /// <exception cref="Exception">如果文件夹不存在或文件夹已隐藏，抛出异常。</exception>
        /// <remarks>该方法会检查文件夹是否存在，并确保文件夹未被隐藏。如果文件夹已隐藏，则会抛出异常。</remarks>
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
#endif
