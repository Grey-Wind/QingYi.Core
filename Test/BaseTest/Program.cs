﻿using QingYi.Core.String.Base;

namespace BaseTest
{
    internal class Program
    {
        static void Main()
        {
            string testText = "Hello World!";

            #region Start
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.DarkYellow;

            // 输出文本
            Console.WriteLine("Base encode/decode test");
            #endregion

            #region Base2
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.Write("·Base2 ");

            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Encode: ");
            Console.Write($"{Base2.Encode(testText, StringEncoding.UTF8)}  ");
            Console.Write("Decode: ");
            Console.Write($"{Base2.Decode(Base2.Encode(testText, StringEncoding.UTF8), StringEncoding.UTF8)}\n");

            // 恢复为默认颜色
            Console.ResetColor();
            #endregion

            #region Base8
            #endregion

            #region Base10
            #endregion

            #region Base16
            #endregion

            #region Base32 RFC4648
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.Write("·Base32 RFC4648 ");

            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Encode: ");
            Console.Write($"{Base32RFC4648.Encode(testText, StringEncoding.UTF8)}  ");
            Console.Write("Decode: ");
            Console.Write($"{Base32RFC4648.Decode(Base32RFC4648.Encode(testText, StringEncoding.UTF8), StringEncoding.UTF8)}\n");

            // 恢复为默认颜色
            Console.ResetColor();
            #endregion

            #region Base32 Extended Hex
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.Write("·Base32 Extended Hex ");

            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Encode: ");
            Console.Write($"{Base32ExtendedHex.Encode(testText, StringEncoding.UTF8)}  ");
            Console.Write("Decode: ");
            Console.Write($"{Base32ExtendedHex.Decode(Base32ExtendedHex.Encode(testText, StringEncoding.UTF8), StringEncoding.UTF8)}\n");

            // 恢复为默认颜色
            Console.ResetColor();
            #endregion

            #region Base32 z-base-32
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.Write("·Base32 z-base-32 ");

            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Encode: ");
            Console.Write($"{Base32z.Encode(testText, StringEncoding.UTF8)}  ");
            Console.Write("Decode: ");
            Console.Write($"{Base32z.Decode(Base32z.Encode(testText, StringEncoding.UTF8), StringEncoding.UTF8)}\n");

            // 恢复为默认颜色
            Console.ResetColor();
            #endregion

            #region Base32 Crockford's
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.Write("·Base32 Crockford's ");

            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Encode: ");
            Console.Write($"{Base32Crockford.Encode(testText, StringEncoding.UTF8)}  ");
            Console.Write("Decode: ");
            Console.Write($"{Base32Crockford.Decode(Base32Crockford.Encode(testText, StringEncoding.UTF8), StringEncoding.UTF8)}\n");

            // 恢复为默认颜色
            Console.ResetColor();
            #endregion

            #region Base36
            #endregion

            #region Base45
            #endregion

            #region Base58
            #endregion

            #region Base62
            #endregion

            #region Base64
            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Magenta;

            Console.Write("·Base64 ");

            // 设置前景颜色
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Encode: ");
            Console.Write($"{Base64.Encode(testText, StringEncoding.UTF8)}  ");
            Console.Write("Decode: ");
            Console.Write($"{Base64.Decode(Base64.Encode(testText, StringEncoding.UTF8), StringEncoding.UTF8)}\n");

            // 恢复为默认颜色
            Console.ResetColor();
            #endregion

            #region Base85
            #endregion

            #region Base91
            #endregion

            #region Base92
            #endregion

            #region Base94
            #endregion

            #region Base100
            #endregion

            #region Base122
            #endregion

            #region Base128
            #endregion

            Console.ReadLine();
        }
    }
}
