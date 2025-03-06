using System.IO;

namespace QingYi.Core.FileUtility.UserProfile
{
    public class Desktop
    {
        public static string path = Get();

        public static string Get() => Path.Combine(Profile.UserProfilePath, "Desktop");
    }
}
