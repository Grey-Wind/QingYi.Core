using System;
using System.IO;

namespace QingYi.Core.FileUtility.UserProfile
{
    /// <summary>
    /// Represents the profile paths for user and application data.<br />
    /// 表示用户和应用程序数据的配置文件路径。
    /// </summary>
    public class Profile
    {
        /// <summary>
        /// Gets the path to the user profile directory.<br />
        /// 获取用户配置文件目录的路径。
        /// </summary>
        public static string UserProfilePath { get; }

        /// <summary>
        /// Gets the path to the AppData directory.<br />
        /// 获取AppData目录的路径。
        /// </summary>
        public static string AppDataPath { get; }

        /// <summary>
        /// Gets the path to the LocalApplicationData directory.<br />
        /// 获取LocalApplicationData目录的路径。
        /// </summary>
        public static string AppDataLoaclPath { get; }

        /// <summary>
        /// Gets the path to the LocalLow directory within AppData.<br />
        /// 获取AppData中LocalLow目录的路径。
        /// </summary>
        public static string AppDataLoaclLowPath { get; }

        /// <summary>
        /// Gets the path to the ApplicationData directory.<br />
        /// 获取ApplicationData目录的路径。
        /// </summary>
        public static string AppDataRomingPath { get; }

        // Static constructor to initialize all static properties.
        static Profile()
        {
            UserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            AppDataPath = Path.Combine(UserProfilePath, "AppData");
            AppDataLoaclPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            AppDataLoaclLowPath = Path.Combine(AppDataPath, "LocalLow");
            AppDataRomingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
    }

}
