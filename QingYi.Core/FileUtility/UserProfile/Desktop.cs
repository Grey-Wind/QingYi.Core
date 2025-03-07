using System.IO;

namespace QingYi.Core.FileUtility.UserProfile
{
    /// <summary>
    /// Operations regarding desktop folders.
    /// </summary>
    public static class Desktop
    {
        /// <summary>
        /// Desktop path.
        /// </summary>
        public static string path => Get();

        /// <summary>
        /// Get desktop path.
        /// </summary>
        public static string Get() => Path.Combine(Profile.UserProfilePath, "Desktop");
    }
}
