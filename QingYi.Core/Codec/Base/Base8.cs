using System;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base8 (Octal) encoding and decoding functionality
    /// </summary>
    public class Base8
    {
        /// <summary>
        /// Encodes binary data to a Base8 (Octal) string
        /// </summary>
        /// <param name="data">Binary data to encode</param>
        /// <returns>Base8 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input data is null</exception>
        public static unsafe string Encode(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return string.Empty;

            int length = data.Length;
            // Each byte expands to 3 octal digits (8^3 = 512, which covers 0-255)
            char[] result = new char[length * 3];

            fixed (byte* pData = data)
            fixed (char* pResult = result)
            {
                byte* src = pData;
                char* dest = pResult;

                for (int i = 0; i < length; i++)
                {
                    byte b = *src++;
                    // Extract and encode each 3-bit segment (octal digit)
                    *dest++ = (char)('0' + (b >> 6));        // First 2 bits (shifted right 6)
                    *dest++ = (char)('0' + (b >> 3 & 0x07)); // Middle 3 bits (mask 0x07)
                    *dest++ = (char)('0' + (b & 0x07));      // Last 3 bits (mask 0x07)
                }
            }

            return new string(result);
        }

        /// <summary>
        /// Decodes a Base8 (Octal) string to binary data
        /// </summary>
        /// <param name="base8">Base8 encoded string</param>
        /// <returns>Decoded binary data</returns>
        /// <exception cref="ArgumentNullException">Thrown when input string is null</exception>
        /// <exception cref="ArgumentException">Thrown for invalid Base8 strings</exception>
        public static unsafe byte[] Decode(string base8)
        {
            if (base8 == null) throw new ArgumentNullException(nameof(base8));
            // Base8 encoding expands each byte to 3 digits, so length must be multiple of 3
            if (base8.Length % 3 != 0) throw new ArgumentException("Invalid Base8 string length");

            int byteCount = base8.Length / 3;
            if (byteCount == 0) return Array.Empty<byte>();

            byte[] result = new byte[byteCount];

            fixed (char* pBase8 = base8)
            fixed (byte* pResult = result)
            {
                char* src = pBase8;
                byte* dest = pResult;

                for (int i = 0; i < byteCount; i++)
                {
                    byte b = 0;
                    // Combine 3 octal digits back into a byte
                    for (int j = 0; j < 3; j++)
                    {
                        int c = *src++;
                        if (c < '0' || c > '7')
                            throw new ArgumentException($"Invalid Base8 character: {(char)c}");

                        b = (byte)(b << 3 | c - '0'); // Shift left and add new 3 bits
                    }
                    *dest++ = b;
                }
            }

            return result;
        }

        /// <summary>
        /// Encodes a string to Base8 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base8 encoded string</returns>
        public static string EncodeString(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] bytes = StringEncodingHelper.GetBytes(input, encoding);
            return Encode(bytes);
        }

        /// <summary>
        /// Decodes a Base8 string to text using the specified encoding
        /// </summary>
        /// <param name="base8">Base8 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeString(string base8, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] bytes = Decode(base8);
            return StringEncodingHelper.GetString(bytes, encoding);
        }

        /// <summary>
        /// Helper class for string encoding/decoding operations
        /// </summary>
        static class StringEncodingHelper
        {
            /// <summary>
            /// Converts string to bytes using specified encoding
            /// </summary>
            /// <param name="s">String to convert</param>
            /// <param name="encoding">Encoding to use</param>
            /// <returns>Byte array representation of the string</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown for unsupported encodings</exception>
            public static byte[] GetBytes(string s, StringEncoding encoding)
            {
                Encoding encoder;
                switch (encoding)
                {
                    case StringEncoding.UTF8:
                        encoder = Encoding.UTF8;
                        break;
                    case StringEncoding.UTF16LE:
                        encoder = Encoding.Unicode;
                        break;
                    case StringEncoding.UTF16BE:
                        encoder = Encoding.BigEndianUnicode;
                        break;
                    case StringEncoding.UTF32:
                        encoder = Encoding.UTF32;
                        break;
#pragma warning disable CS0618, SYSLIB0001
                    case StringEncoding.UTF7:
                        encoder = Encoding.UTF7;
                        break;
#pragma warning restore CS0618, SYSLIB0001
#if NET6_0_OR_GREATER
                    case StringEncoding.Latin1:
                        encoder = Encoding.Latin1;
                        break;
#endif
                    case StringEncoding.ASCII:
                        encoder = Encoding.ASCII;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(encoding));
                }
                return encoder.GetBytes(s);
            }

            /// <summary>
            /// Converts bytes to string using specified encoding
            /// </summary>
            /// <param name="bytes">Bytes to convert</param>
            /// <param name="encoding">Encoding to use</param>
            /// <returns>String representation of the bytes</returns>
            /// <exception cref="ArgumentOutOfRangeException">Thrown for unsupported encodings</exception>
            public static string GetString(byte[] bytes, StringEncoding encoding)
            {
                Encoding decoder;
                switch (encoding)
                {
                    case StringEncoding.UTF8:
                        decoder = Encoding.UTF8;
                        break;
                    case StringEncoding.UTF16LE:
                        decoder = Encoding.Unicode;
                        break;
                    case StringEncoding.UTF16BE:
                        decoder = Encoding.BigEndianUnicode;
                        break;
                    case StringEncoding.ASCII:
                        decoder = Encoding.ASCII;
                        break;
                    case StringEncoding.UTF32:
                        decoder = Encoding.UTF32;
                        break;
#if NET6_0_OR_GREATER
                    case StringEncoding.Latin1:
                        decoder = Encoding.Latin1;
                        break;
#endif
#pragma warning disable CS0618, SYSLIB0001
                    case StringEncoding.UTF7:
                        decoder = Encoding.UTF7;
                        break;
#pragma warning restore CS0618, SYSLIB0001
                    default:
                        throw new ArgumentOutOfRangeException(nameof(encoding));
                }
                return decoder.GetString(bytes);
            }
        }
    }

    /// <summary>
    /// Provides extension methods for Base8 encoding/decoding
    /// </summary>
    public static class Base8Extension
    {
        /// <summary>
        /// Encodes a string to Base8 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base8 encoded string</returns>
        public static string EncodeBase8(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base8.EncodeString(input, encoding);

        /// <summary>
        /// Decodes a Base8 string to text using the specified encoding
        /// </summary>
        /// <param name="input">Base8 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeBase8(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base8.DecodeString(input, encoding);

        /// <summary>
        /// Encodes binary data to a Base8 string
        /// </summary>
        /// <param name="input">Binary data to encode</param>
        /// <returns>Base8 encoded string</returns>
        public static string EncodeBase8(this byte[] input) => Base8.Encode(input);

        /// <summary>
        /// Decodes a Base8 string to binary data
        /// </summary>
        /// <param name="input">Base8 encoded string</param>
        /// <returns>Decoded binary data</returns>
        public static byte[] DecodeBase8(this string input) => Base8.Decode(input);
    }
}
