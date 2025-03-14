using System.Text;
using System;

namespace QingYi.Core.String.Base
{
    public static class Base32WordSafe
    {
        private const string Alphabet = "23456789CFGHJMPQRVWXcfghjmpqrvwx";
        private static readonly byte[] LookupTable = new byte[256];

        static Base32WordSafe()
        {
            Array.Fill(LookupTable, (byte)0xFF);
            for (var i = 0; i < Alphabet.Length; i++)
                LookupTable[Alphabet[i]] = (byte)i;
        }

        public static string Encode(string input, StringEncoding encoding)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var bytes = GetEncoding(encoding).GetBytes(input);
            return ConvertToBase32(bytes);
        }

        public static string Decode(string base32Input, StringEncoding encoding)
        {
            if (base32Input == null) throw new ArgumentNullException(nameof(base32Input));
            var bytes = ConvertFromBase32(base32Input);
            return GetEncoding(encoding).GetString(bytes);
        }

        private static Encoding GetEncoding(StringEncoding encoding) => encoding switch
        {
            StringEncoding.UTF8 => Encoding.UTF8,
            StringEncoding.UTF16LE => new UnicodeEncoding(false, false),
            StringEncoding.UTF16BE => new UnicodeEncoding(true, false),
            StringEncoding.UTF32 => new UTF32Encoding(),
            StringEncoding.UTF7 => Encoding.UTF7,
            StringEncoding.ASCII => Encoding.ASCII,
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };

        private static unsafe string ConvertToBase32(byte[] input)
        {
            if (input.Length == 0) return string.Empty;

            // 计算基础长度和需要填充的字符数
            var baseLength = (input.Length * 8 + 4) / 5;
            var padding = (8 - (baseLength % 8)) % 8;
            var outputLength = baseLength + padding;
            var output = new char[outputLength];

            fixed (byte* inputPtr = input)
            fixed (char* outputPtr = output)
            {
                var buffer = 0;
                var bitsLeft = 0;
                var outputIndex = 0;

                // 编码主体
                for (var i = 0; i < input.Length; i++)
                {
                    buffer = (buffer << 8) | inputPtr[i];
                    bitsLeft += 8;

                    while (bitsLeft >= 5)
                    {
                        var value = (buffer >> (bitsLeft - 5)) & 0x1F;
                        outputPtr[outputIndex++] = Alphabet[value];
                        bitsLeft -= 5;
                    }
                }

                // 处理剩余位
                if (bitsLeft > 0)
                {
                    var value = (buffer << (5 - bitsLeft)) & 0x1F;
                    outputPtr[outputIndex++] = Alphabet[value];
                }

                // 添加填充字符
                for (; outputIndex < outputLength; outputIndex++)
                    outputPtr[outputIndex] = '=';
            }

            return new string(output);
        }

        private static unsafe byte[] ConvertFromBase32(string input)
        {
            if (input.Length == 0) return Array.Empty<byte>();
            if (input.Length % 8 != 0)
                throw new ArgumentException("Base32 string length must be multiple of 8");

            var effectiveLength = input.Length;
            while (effectiveLength > 0 && input[effectiveLength - 1] == '=')
                effectiveLength--;

            var output = new byte[effectiveLength * 5 / 8];

            fixed (char* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                var buffer = 0;
                var bitsLeft = 0;
                var outputIndex = 0;

                for (var i = 0; i < effectiveLength; i++)
                {
                    var c = inputPtr[i];
                    if (c >= 256 || LookupTable[c] == 0xFF)
                        throw new ArgumentException($"Invalid character: {c}");

                    buffer = (buffer << 5) | LookupTable[c];
                    bitsLeft += 5;

                    if (bitsLeft >= 8)
                    {
                        bitsLeft -= 8;
                        outputPtr[outputIndex++] = (byte)((buffer >> bitsLeft) & 0xFF);
                    }
                }
            }

            return output;
        }
    }
}
