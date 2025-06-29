#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base91 encoding and decoding functionality
    /// </summary>
    public class Base91
    {
        /// <summary>
        /// Base91 encoding table (standard ASCII 33-126 excluding quotes and backslash)
        /// </summary>
        private const string EncodeTable = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%&()*+,./:;<=>?@[]^_`{|}~";

        /// <summary>
        /// Decoding lookup table (256-element fast lookup)
        /// </summary>
        private static readonly sbyte[] DecodeTable = new sbyte[256];

        /// <summary>
        /// Static constructor initializes the decoding table
        /// </summary>
        static Base91()
        {
            // Initialize decode table (invalid characters marked as -1)
            Array.Fill(DecodeTable, (sbyte)-1);
            for (sbyte i = 0; i < EncodeTable.Length; i++)
            {
                char c = EncodeTable[i];
                DecodeTable[c] = i;
            }
        }

        #region Encoding Interface
        /// <summary>
        /// Encodes binary data to Base91 string
        /// </summary>
        /// <param name="data">Binary data to encode</param>
        /// <returns>Base91 encoded string</returns>
        public static string Encode(byte[] data) => EncodeCore(data);

        /// <summary>
        /// Encodes text string to Base91 string
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF8)</param>
        /// <returns>Base91 encoded string</returns>
        public static string Encode(string text, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] data = GetBytes(text, encoding);
            return EncodeCore(data);
        }
        #endregion

        #region Decoding Interface
        /// <summary>
        /// Decodes Base91 string to binary data
        /// </summary>
        /// <param name="base91Text">Base91 encoded string</param>
        /// <returns>Decoded binary data</returns>
        public static byte[] Decode(string base91Text) => DecodeCore(base91Text);

        /// <summary>
        /// Decodes Base91 string to text string
        /// </summary>
        /// <param name="base91Text">Base91 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF8)</param>
        /// <returns>Decoded text string</returns>
        public static string DecodeToString(string base91Text, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] data = DecodeCore(base91Text);
            return GetString(data, encoding);
        }
        #endregion

        #region Core Encoding/Decoding Logic
        /// <summary>
        /// Core Base91 encoding implementation
        /// </summary>
        private static unsafe string EncodeCore(byte[] input)
        {
            if (input == null || input.Length == 0) return string.Empty;

            // Pre-allocate output buffer (max length = original length * 1.25)
            int outputLen = (int)Math.Ceiling(input.Length * 1.25) + 16;
            char[] outputBuffer = new char[outputLen];
            int outIndex = 0;
            int b = 0, n = 0; // b = accumulator, n = bit counter

            fixed (byte* inputPtr = input)
            fixed (char* outputPtr = outputBuffer)
            {
                byte* p = inputPtr;
                byte* end = p + input.Length;

                while (p < end)
                {
                    b |= *p << n;  // Accumulate bits
                    n += 8;         // Count added bits

                    if (n >= 13)    // Enough data to generate two characters
                    {
                        int v = b & 0x1FFF; // Take 13 bits
                        int shift = (v < 0x5F) ? 14 : 13; // Threshold optimization
                        b >>= shift;
                        n -= shift;

                        // Write two characters (reverse order for efficiency)
                        outputPtr[outIndex++] = EncodeTable[v % 91];
                        outputPtr[outIndex++] = EncodeTable[v / 91];
                    }
                    p++;
                }

                // Handle remaining data
                if (n > 0)
                {
                    outputPtr[outIndex++] = EncodeTable[b % 91];
                    if (n > 7 || b > 90) // Need second character
                        outputPtr[outIndex++] = EncodeTable[b / 91];
                }
            }
            return new string(outputBuffer, 0, outIndex);
        }

        /// <summary>
        /// Core Base91 decoding implementation
        /// </summary>
        private static unsafe byte[] DecodeCore(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();

            List<byte> output = new List<byte>(input.Length);
            int v = -1, b = 0, n = 0; // v = value, b = accumulator, n = bit counter

            fixed (char* inputPtr = input)
            {
                char* p = inputPtr;
                char* end = p + input.Length;

                while (p < end)
                {
                    // Skip invalid characters
                    if (*p < 0 || *p >= 256 || DecodeTable[*p] == -1)
                    {
                        p++;
                        continue;
                    }

                    int digit = DecodeTable[*p];
                    if (v < 0)
                    {
                        v = digit; // Store first character
                    }
                    else
                    {
                        v += digit * 91;
                        int shift = (v & 0x1FFF) > 88 ? 13 : 14; // Dynamic bit selection
                        b |= v << n;
                        n += shift;

                        // Extract complete bytes
                        while (n >= 8)
                        {
                            output.Add((byte)(b & 0xFF));
                            b >>= 8;
                            n -= 8;
                        }
                        v = -1; // Reset state
                    }
                    p++;
                }

                // Handle last character if present
                if (v > -1)
                {
                    b |= v << n;
                    n += 7; // Pad remaining data
                    while (n > 0)
                    {
                        output.Add((byte)(b & 0xFF));
                        b >>= 8;
                        n -= 8;
                    }
                }
            }
            return output.ToArray();
        }
        #endregion

        #region Encoding Conversion Helpers
        /// <summary>
        /// Converts text to bytes using specified encoding
        /// </summary>
        private static byte[] GetBytes(string text, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.ASCII => Encoding.ASCII.GetBytes(text),
                StringEncoding.UTF16LE => Encoding.Unicode.GetBytes(text),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetBytes(text),
                StringEncoding.UTF32 => Encoding.UTF32.GetBytes(text),
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => Encoding.UTF7.GetBytes(text),
#pragma warning restore SYSLIB0001, CS0618
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetBytes(text),
#endif
                _ => Encoding.UTF8.GetBytes(text) // Default to UTF-8
            };
        }

        /// <summary>
        /// Converts bytes to text using specified encoding
        /// </summary>
        private static string GetString(byte[] data, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.ASCII => Encoding.ASCII.GetString(data),
                StringEncoding.UTF16LE => Encoding.Unicode.GetString(data),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetString(data),
                StringEncoding.UTF32 => Encoding.UTF32.GetString(data),
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => Encoding.UTF7.GetString(data),
#pragma warning restore SYSLIB0001, CS0618
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetString(data),
#endif
                _ => Encoding.UTF8.GetString(data) // Default to UTF-8
            };
        }
        #endregion
    }
}
#endif
