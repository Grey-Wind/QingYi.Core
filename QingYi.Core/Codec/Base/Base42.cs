#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base42 encoding and decoding functionality
    /// </summary>
    public class Base42
    {
        // Base42 character set (42 characters)
        private const string CHARSET = "1234567890!@#$%^&*()~`_+-={}|[]\\:\";'<>?,./";
        private const char PADDING_CHAR = 'A'; // Padding character for incomplete groups
        private const int GROUP_BYTES = 3;     // Number of bytes processed in each group
        private const int GROUP_CHARS = 5;     // Number of characters produced per group
        private static readonly byte[] s_decodeMap = new byte[128]; // Lookup table for character decoding
        private static readonly uint[] s_powers = { 1, 42, 1764, 74088, 3111696 }; // Powers of 42 for calculations

        /// <summary>
        /// Static constructor initializes the decoding map
        /// </summary>
        static Base42()
        {
            // Initialize decode map with invalid markers
            Array.Fill(s_decodeMap, byte.MaxValue);
            // Populate valid characters in the decode map
            for (byte i = 0; i < CHARSET.Length; i++)
            {
                char c = CHARSET[i];
                s_decodeMap[c] = i;
            }
        }

        /// <summary>
        /// Returns the Base42 character set used for encoding
        /// </summary>
        /// <returns>The Base42 character set string</returns>
        public override string ToString() => CHARSET;

        #region Encoding Methods

        /// <summary>
        /// Encodes binary data to a Base42 string
        /// </summary>
        /// <param name="data">Binary data to encode</param>
        /// <returns>Base42 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input data is null</exception>
        public static string Encode(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return string.Empty;

            int groups = (data.Length + GROUP_BYTES - 1) / GROUP_BYTES;
            char[] result = new char[groups * GROUP_CHARS];

            unsafe
            {
                fixed (byte* dataPtr = data)
                fixed (char* resultPtr = result)
                {
                    int dataIndex = 0;
                    char* current = resultPtr;
                    int remaining = data.Length;

                    // Process complete groups
                    while (remaining >= GROUP_BYTES)
                    {
                        EncodeGroup(dataPtr + dataIndex, current);
                        dataIndex += GROUP_BYTES;
                        current += GROUP_CHARS;
                        remaining -= GROUP_BYTES;
                    }

                    // Process remaining bytes in last incomplete group
                    if (remaining > 0)
                    {
                        EncodeLastGroup(dataPtr + dataIndex, current, remaining);
                    }
                }
            }

            return new string(result);
        }

        /// <summary>
        /// Encodes a string to Base42 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base42 encoded string</returns>
        public static string EncodeString(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) return null;
            byte[] data = GetBytes(input, encoding);
            return Encode(data);
        }

        /// <summary>
        /// Encodes a complete 3-byte group to 5 Base42 characters
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void EncodeGroup(byte* data, char* output)
        {
            // Combine 3 bytes into a 24-bit value
            uint value = (uint)data[0] << 16 | (uint)data[1] << 8 | data[2];
            // Convert to Base42 by repeated division
            for (int i = GROUP_CHARS - 1; i >= 0; i--)
            {
                output[i] = CHARSET[(int)(value % 42)];
                value /= 42;
            }
        }

        /// <summary>
        /// Encodes an incomplete byte group (1 or 2 bytes) with padding
        /// </summary>
        private static unsafe void EncodeLastGroup(byte* data, char* output, int count)
        {
            Debug.Assert(count > 0 && count < GROUP_BYTES);

            uint value = 0;
            // Combine remaining bytes into a value
            for (int i = 0; i < count; i++)
            {
                value = (value << 8) | data[i];
            }

            // Determine how many characters are needed
            int charsNeeded = count switch
            {
                1 => 2,  // 1 byte needs 2 Base42 chars
                2 => 3,  // 2 bytes need 3 Base42 chars
                _ => throw new InvalidOperationException()
            };

            // Convert to Base42
            for (int i = charsNeeded - 1; i >= 0; i--)
            {
                output[i] = CHARSET[(int)(value % 42)];
                value /= 42;
            }

            // Pad remaining characters
            for (int i = charsNeeded; i < GROUP_CHARS; i++)
            {
                output[i] = PADDING_CHAR;
            }
        }
        #endregion

        #region Decoding Methods

        /// <summary>
        /// Decodes a Base42 string to binary data
        /// </summary>
        /// <param name="base42">Base42 encoded string</param>
        /// <returns>Decoded binary data</returns>
        /// <exception cref="ArgumentNullException">Thrown when input string is null</exception>
        /// <exception cref="ArgumentException">Thrown for invalid Base42 strings</exception>
        public static byte[] Decode(string base42)
        {
            if (base42 == null) throw new ArgumentNullException(nameof(base42));
            if (base42.Length == 0) return Array.Empty<byte>();

            // Validate input length is multiple of group size
            if (base42.Length % GROUP_CHARS != 0)
                throw new ArgumentException($"Base42 string length must be multiple of {GROUP_CHARS}");

            int totalGroups = base42.Length / GROUP_CHARS;
            List<byte> result = new List<byte>(totalGroups * GROUP_BYTES);

            unsafe
            {
                fixed (char* base42Ptr = base42)
                {
                    // Process each group
                    for (int groupIndex = 0; groupIndex < totalGroups; groupIndex++)
                    {
                        char* groupStart = base42Ptr + groupIndex * GROUP_CHARS;
                        DecodeGroup(groupStart, result, groupIndex == totalGroups - 1);
                    }
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// Decodes a Base42 string to text using the specified encoding
        /// </summary>
        /// <param name="base42">Base42 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeString(string base42, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] data = Decode(base42);
            return GetString(data, encoding);
        }

        /// <summary>
        /// Decodes a group of Base42 characters to bytes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void DecodeGroup(char* input, List<byte> output, bool isLastGroup)
        {
            uint value = 0;
            int validChars = GROUP_CHARS;

            // Check for padding in last group
            if (isLastGroup)
            {
                for (int i = GROUP_CHARS - 1; i >= 0; i--)
                {
                    if (input[i] == PADDING_CHAR) validChars--;
                    else break;
                }
            }

            // Convert Base42 characters back to a value
            for (int i = 0; i < validChars; i++)
            {
                char c = input[i];
                if (c >= s_decodeMap.Length || s_decodeMap[c] == byte.MaxValue)
                    throw new ArgumentException($"Invalid character '{c}' in Base42 string");

                value = value * 42 + s_decodeMap[c];
            }

            // Determine how many bytes were encoded
            int bytesNeeded = validChars switch
            {
                2 => 1,  // 2 chars = 1 byte
                3 => 2,  // 3 chars = 2 bytes
                5 => 3,  // 5 chars = 3 bytes
                _ => throw new ArgumentException("Invalid group size in Base42 string")
            };

            // Extract bytes from the value
            for (int i = bytesNeeded - 1; i >= 0; i--)
            {
                byte b = (byte)(value >> (8 * i));
                output.Add(b);
            }
        }
        #endregion

        #region Encoding Helper Methods

        /// <summary>
        /// Converts a string to bytes using the specified encoding
        /// </summary>
        /// <param name="s">String to convert</param>
        /// <param name="encoding">Encoding to use</param>
        /// <returns>Byte array representation of the string</returns>
        private static byte[] GetBytes(string s, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8.GetBytes(s),
                StringEncoding.UTF16LE => Encoding.Unicode.GetBytes(s),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetBytes(s),
                StringEncoding.ASCII => Encoding.ASCII.GetBytes(s),
                StringEncoding.UTF32 => Encoding.UTF32.GetBytes(s),
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetBytes(s),
#endif
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => GetUTF7Bytes(s),
#pragma warning restore SYSLIB0001, CS0618
                _ => throw new ArgumentException("Unsupported encoding")
            };
        }

        /// <summary>
        /// Converts bytes to a string using the specified encoding
        /// </summary>
        /// <param name="bytes">Bytes to convert</param>
        /// <param name="encoding">Encoding to use</param>
        /// <returns>String representation of the bytes</returns>
        private static string GetString(byte[] bytes, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8.GetString(bytes),
                StringEncoding.UTF16LE => Encoding.Unicode.GetString(bytes),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetString(bytes),
                StringEncoding.ASCII => Encoding.ASCII.GetString(bytes),
                StringEncoding.UTF32 => Encoding.UTF32.GetString(bytes),
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetString(bytes),
#endif
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => GetUTF7String(bytes),
#pragma warning restore SYSLIB0001, CS0618
                _ => throw new ArgumentException("Unsupported encoding")
            };
        }

        // UTF-7 encoding methods (obsolete in newer .NET versions)
#pragma warning disable SYSLIB0001, CS0618
        private static byte[] GetUTF7Bytes(string s) => Encoding.UTF7.GetBytes(s);
        private static string GetUTF7String(byte[] bytes) => Encoding.UTF7.GetString(bytes);
#pragma warning restore SYSLIB0001, CS0618
        #endregion
    }
}
#endif
