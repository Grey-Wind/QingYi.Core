﻿using System.Text;
using System;

namespace QingYi.Core.String.Base
{
    public static class Base32z
    {
        /// <summary>
        /// 字符串编码格式。<br />
        /// String encoding.
        /// </summary>
        public enum StringEncoding
        {
            /// <summary>
            /// UTF-8
            /// </summary>
            UTF8,

            /// <summary>
            /// UTF-16 LE
            /// </summary>
            UTF16LE,

            /// <summary>
            /// UTF-16 BE
            /// </summary>
            UTF16BE,

            /// <summary>
            /// ASCII
            /// </summary>
            ASCII,

            /// <summary>
            /// UTF-32
            /// </summary>
            UTF32,

#if NET6_0_OR_GREATER
            /// <summary>
            /// Latin1
            /// </summary>
            Latin1,
#endif
            /// <summary>
            /// UTF-7
            /// </summary>
            [Obsolete(message: "UTF-7 has been deprecated because it is obsolete.")]
            UTF7,
        }

        private const string ZBase32Chars = "ybndrfg8ejkmcpqxot1uwisza345h769";
        private static readonly byte[] ReverseTable = new byte[128];

        static Base32z()
        {
            for (int i = 0; i < ReverseTable.Length; i++)
                ReverseTable[i] = 0xFF;

            for (byte i = 0; i < ZBase32Chars.Length; i++)
            {
                char c = ZBase32Chars[i];
                if (c >= ReverseTable.Length)
                    throw new InvalidOperationException("Invalid character in Z-Base-32 charset.");
                ReverseTable[c] = i;
            }
        }

        public static string Encode(string input, StringEncoding encoding)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            byte[] bytes = GetBytes(input, encoding);
            return EncodeToString(bytes);
        }

        public static string Decode(string base32, StringEncoding encoding)
        {
            if (base32 == null)
                throw new ArgumentNullException(nameof(base32));

            byte[] bytes = DecodeToBytes(base32);
            return GetString(bytes, encoding);
        }

        private static byte[] GetBytes(string input, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8.GetBytes(input),
                StringEncoding.UTF16LE => Encoding.Unicode.GetBytes(input),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetBytes(input),
                StringEncoding.UTF32 => Encoding.UTF32.GetBytes(input),
                StringEncoding.UTF7 => Encoding.UTF7.GetBytes(input),
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetBytes(input),
#endif
                StringEncoding.ASCII => Encoding.ASCII.GetBytes(input),
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };
        }

        private static string GetString(byte[] bytes, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8.GetString(bytes),
                StringEncoding.UTF16LE => Encoding.Unicode.GetString(bytes),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetString(bytes),
                StringEncoding.UTF32 => Encoding.UTF32.GetString(bytes),
                StringEncoding.UTF7 => Encoding.UTF7.GetString(bytes),
                StringEncoding.ASCII => Encoding.ASCII.GetString(bytes),
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };
        }

        private static string EncodeToString(byte[] bytes)
        {
            if (bytes.Length == 0)
                return string.Empty;

            int byteCount = bytes.Length;
            int outputLength = (byteCount * 8 + 4) / 5; // ceil(bitCount/5)
            char[] output = new char[outputLength];

            ulong buffer = 0;
            int bitsInBuffer = 0;
            int outputPos = 0;

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    byte* current = ptr;
                    byte* end = ptr + byteCount;
                    while (current < end)
                    {
                        buffer = (buffer << 8) | *current++;
                        bitsInBuffer += 8;

                        while (bitsInBuffer >= 5)
                        {
                            int index = (int)((buffer >> (bitsInBuffer - 5)) & 0x1F);
                            output[outputPos++] = ZBase32Chars[index];
                            bitsInBuffer -= 5;
                            buffer &= (1UL << bitsInBuffer) - 1;
                        }
                    }
                }
            }

            if (bitsInBuffer > 0)
            {
                buffer <<= (5 - bitsInBuffer);
                int index = (int)(buffer & 0x1F);
                output[outputPos++] = ZBase32Chars[index];
            }

            return new string(output);
        }

        private static byte[] DecodeToBytes(string base32)
        {
            if (base32.Length == 0)
                return Array.Empty<byte>();

            int inputLength = base32.Length;
            int bitCount = inputLength * 5;
            int byteCount = (bitCount + 7) / 8;
            byte[] output = new byte[byteCount];

            ulong buffer = 0;
            int bitsInBuffer = 0;
            int outputPos = 0;

            unsafe
            {
                fixed (char* inputPtr = base32)
                {
                    char* current = inputPtr;
                    char* end = inputPtr + inputLength;
                    while (current < end)
                    {
                        char c = *current++;
                        if (c >= ReverseTable.Length || ReverseTable[c] == 0xFF)
                            throw new ArgumentException($"Invalid character '{c}' in Base32 string.");

                        byte value = ReverseTable[c];
                        buffer = (buffer << 5) | value;
                        bitsInBuffer += 5;

                        while (bitsInBuffer >= 8)
                        {
                            byte b = (byte)(buffer >> (bitsInBuffer - 8));
                            output[outputPos++] = b;
                            bitsInBuffer -= 8;
                            buffer &= (1UL << bitsInBuffer) - 1;
                        }
                    }
                }
            }

            if (bitsInBuffer > 0)
            {
                buffer <<= (8 - bitsInBuffer);
                output[outputPos++] = (byte)buffer;
            }

            if (outputPos < output.Length)
                Array.Resize(ref output, outputPos);

            return output;
        }
    }
}
