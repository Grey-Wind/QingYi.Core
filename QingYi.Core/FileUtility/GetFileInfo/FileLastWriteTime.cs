using System;

namespace QingYi.Core.FileUtility.GetFileInfo
{
    public class FileLastWriteTime
    {
        public static DateTime Get(string filePath)
        {
            Select select = new Select();

            var result = select.SelectFile(filePath);

            DateTime dateTime = result.Item6;

            return dateTime;
        }
    }
}
