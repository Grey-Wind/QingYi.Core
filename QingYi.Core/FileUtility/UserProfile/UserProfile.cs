using System;
using System.IO;

namespace QingYi.Core.FileUtility.UserProfile
{
    public class Profile
    {
        public static string UserProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static string AppDataPath = Path.Combine(UserProfilePath, "AppData");

        public static string AppDataLoaclPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public static string AppDataLoaclLowPath = Path.Combine(AppDataPath, "LocalLow");

        public static string AppDataRomingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }
}
