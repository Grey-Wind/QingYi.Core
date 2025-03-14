using System.Text;
using System;

#pragma warning disable SYSLIB0001, CS0618

namespace QingYi.Core.String.Base
{
    public class Base32WordSafe
    {
        private const string Alphabet = "23456789CFGHJMPQRVWXcfghjmpqrvwx";
        private static readonly byte[] LookupTable = new byte[256];
        private const int MaxPaddingAttempts = 6; // 根据Base32规范最多需要6个填充

        static Base32WordSafe()
        {
            Array.Fill(LookupTable, (byte)0xFF);
            for (var i = 0; i < Alphabet.Length; i++)
                LookupTable[Alphabet[i]] = (byte)i;
        }

        public override string ToString() => Alphabet;

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
#if NET6_0_OR_GREATER
            StringEncoding.Latin1 => Encoding.Latin1,
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };

        private static unsafe string ConvertToBase32(byte[] input)
        {
            if (input.Length == 0) return string.Empty;

            var outputLength = (input.Length * 8 + 4) / 5;
            var output = new char[outputLength];

            fixed (byte* inputPtr = input)
            fixed (char* outputPtr = output)
            {
                var buffer = 0;
                var bitsLeft = 0;
                var outputIndex = 0;

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

                if (bitsLeft > 0)
                {
                    var value = (buffer << (5 - bitsLeft)) & 0x1F;
                    outputPtr[outputIndex] = Alphabet[value];
                }
            }

            return new string(output);
        }

        private static unsafe byte[] ConvertFromBase32(string input)
        {
            if (input.Length == 0) return Array.Empty<byte>();

            // 生成所有可能的填充组合
            var candidates = new string[MaxPaddingAttempts + 1];
            for (int p = 0; p <= MaxPaddingAttempts; p++)
            {
                candidates[p] = input.PadRight(input.Length + p, '=');
                if (candidates[p].Length % 8 != 0)
                    candidates[p] = null; // 仅保留有效长度
            }

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                try
                {
                    return ProcessPaddedString(candidate);
                }
                catch (ArgumentException)
                {
                    // 继续尝试下一个候选
                }
            }

            throw new ArgumentException("Invalid Base32 string");
        }

        private static unsafe byte[] ProcessPaddedString(string input)
        {
            var effectiveLength = input.Length;
            while (effectiveLength > 0 && input[effectiveLength - 1] == '=')
                effectiveLength--;

            var outputLength = effectiveLength * 5 / 8;
            var output = new byte[outputLength];

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

                // 验证剩余位数有效性
                if (bitsLeft >= 5)
                    throw new ArgumentException("Invalid padding");
            }

            return output;
        }
    }
}
