using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QingYi.Core.FileUtility
{
#if WINDOWS
    public struct ShowHide
    {
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

        // 显示文件夹的函数
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
                    // 获取文件夹的当前属性
                    FileAttributes attributes = File.GetAttributes(folderPath);

                    // 如果文件夹是隐藏的，则显示它
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

        // 隐藏文件夹的函数
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
                    // 获取文件夹的当前属性
                    FileAttributes attributes = File.GetAttributes(folderPath);

                    // 如果文件夹是隐藏的，则无需更改
                    switch (attributes & FileAttributes.Hidden)
                    {
                        case FileAttributes.Hidden:
                            throw new Exception("The folder is already hidden.");
                        default:
                            // 否则，将文件夹设置为隐藏
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
#endif
}
