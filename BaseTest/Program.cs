﻿using QingYi.Core.String.Base;

namespace BaseTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string testText = "Hello World!";

            #region Start
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            // 输出文本
            Console.WriteLine("Base encode/decode test");
            #endregion

            #region Base64
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.Write("·Base64 ");

            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Encode: ");
            Console.Write($"{Base64.Encode(testText, Base64.TextEncoding.UTF8)}  ");
            Console.Write("Decode: ");
            Console.Write($"{Base64.Decode(Base64.Encode(testText, Base64.TextEncoding.UTF8), Base64.TextEncoding.UTF8)}");

            // 恢复为默认颜色
            Console.ResetColor();
            #endregion

            Console.ReadLine();
        }
    }
}
