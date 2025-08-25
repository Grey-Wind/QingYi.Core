#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base94 encoding and decoding functionality
    /// </summary>
    /// <remarks>
    /// Base94 encoding scheme uses 94 printable ASCII characters (33-126) to encode binary data.
    /// It processes 3 bytes at a time into 4 characters, with padding using '=' characters.
    /// </remarks>
    public class Base94
    {
        /// <summary>
        /// Base94 character set (ASCII characters 33-126)
        /// </summary>
        private const string Base94Chars = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        /// <summary>
        /// Lookup table for character to value mapping
        /// </summary>
        private static readonly sbyte[] CharToValueMap = new sbyte[128];

        /// <summary>
        /// Static constructor initializes the character to value mapping table
        /// </summary>
        static Base94()
        {
            // Initialize with -1 indicating invalid characters
            Array.Fill(CharToValueMap, (sbyte)-1);

            // Populate valid character mappings
            for (int i = 0; i < Base94Chars.Length; i++)
            {
                char c = Base94Chars[i];
                if (c < 128) CharToValueMap[c] = (sbyte)i;
            }
        }

        /// <summary>
        /// Gets the Encoding instance for the specified encoding type
        /// </summary>
        /// <param name="encoding">Encoding type enum value</param>
        /// <returns>Corresponding Encoding instance</returns>
        private static Encoding GetEncoding(StringEncoding encoding)
        {
            switch (encoding)
            {
                case StringEncoding.UTF8: return Encoding.UTF8;
                case StringEncoding.UTF16LE: return Encoding.Unicode;
                case StringEncoding.UTF16BE: return Encoding.BigEndianUnicode;
                case StringEncoding.ASCII: return Encoding.ASCII;
                case StringEncoding.UTF32: return Encoding.UTF32;
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1: return Encoding.Latin1;
#endif
#pragma warning disable SYSLIB0001, CS0618
                case StringEncoding.UTF7:
                    return Encoding.UTF7;
#pragma warning restore SYSLIB0001, CS0618
                default: return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Encodes a byte array to Base94 string
        /// </summary>
        /// <param name="data">Byte array to encode</param>
        /// <returns>Base94 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input data is null</exception>
        public static unsafe string Encode(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return string.Empty;

            // Calculate output length (4 characters per 3 bytes)
            int outputLength = (int)Math.Ceiling(data.Length / 3.0) * 4;
            return string.Create(outputLength, data, (chars, state) =>
            {
                fixed (byte* pData = state)
                fixed (char* pChars = chars)
                {
                    byte* input = pData;
                    char* output = pChars;
                    int remaining = state.Length;

                    // Process complete 3-byte groups
                    while (remaining >= 3)
                    {
                        // Combine 3 bytes into 24-bit value
                        uint value = (uint)(*input++) << 16;
                        value |= (uint)(*input++) << 8;
                        value |= *input++;
                        remaining -= 3;

                        // Split into 4 Base94 characters
                        *output++ = Base94Chars[(int)(value / 830584)];   // 94^3
                        *output++ = Base94Chars[(int)(value / 8836) % 94]; // 94^2
                        *output++ = Base94Chars[(int)(value / 94) % 94];
                        *output++ = Base94Chars[(int)(value % 94)];
                    }

                    // Handle remaining bytes (1 or 2)
                    if (remaining > 0)
                    {
                        uint value = 0;
                        if (remaining == 1)
                        {
                            // Single remaining byte - output 2 chars + 2 padding
                            value = (uint)(*input++) << 16;
                            *output++ = Base94Chars[(int)(value / 830584)];
                            *output++ = Base94Chars[(int)(value / 8836) % 94];
                            *output++ = '=';
                            *output++ = '=';
                        }
                        else // remaining == 2
                        {
                            // Two remaining bytes - output 3 chars + 1 padding
                            value = (uint)(*input++) << 16;
                            value |= (uint)(*input++) << 8;
                            *output++ = Base94Chars[(int)(value / 830584)];
                            *output++ = Base94Chars[(int)(value / 8836) % 94];
                            *output++ = Base94Chars[(int)(value / 94) % 94];
                            *output++ = '=';
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Encodes a string to Base94 string using specified encoding
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base94 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input text is null</exception>
        public static string Encode(string text, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            byte[] data = GetEncoding(encoding).GetBytes(text);
            return Encode(data);
        }

        /// <summary>
        /// Decodes a Base94 string to byte array
        /// </summary>
        /// <param name="base94">Base94 encoded string</param>
        /// <returns>Decoded byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when input string is null</exception>
        /// <exception cref="ArgumentException">Thrown for invalid Base94 strings</exception>
        public static unsafe byte[] Decode(string base94)
        {
            if (base94 == null) throw new ArgumentNullException(nameof(base94));
            if (base94.Length == 0) return Array.Empty<byte>();
            if (base94.Length % 4 != 0) throw new ArgumentException("Base94 string length must be multiple of 4");

            // Calculate output length (accounting for padding)
            int groupCount = base94.Length / 4;
            int padding = base94[^1] == '=' ? (base94[^2] == '=' ? 2 : 1) : 0;
            int outputLength = groupCount * 3 - padding;
            byte[] result = new byte[outputLength];

            fixed (char* pBase94 = base94)
            fixed (byte* pResult = result)
            {
                char* input = pBase94;
                byte* output = pResult;

                // Process each 4-character group
                for (int i = 0; i < groupCount; i++)
                {
                    uint value = 0;
                    int validChars = 4;

                    // Process characters in current group
                    for (int j = 0; j < 4; j++)
                    {
                        char c = *input++;
                        if (c == '=')
                        {
                            // Padding character found
                            if (j < 2) throw new ArgumentException("Invalid padding position");
                            validChars = j;
                            break;
                        }

                        // Validate character range
                        if (c >= 128 || CharToValueMap[c] == -1)
                            throw new ArgumentException($"Invalid character: '{c}' (0x{(int)c:X4})");

                        value = value * 94 + (uint)CharToValueMap[c];
                    }

                    // Output bytes based on valid characters
                    if (validChars >= 2) *output++ = (byte)(value >> 16);
                    if (validChars >= 3) *output++ = (byte)(value >> 8);
                    if (validChars >= 4) *output++ = (byte)value;
                }
            }

            return result;
        }

        /// <summary>
        /// Decodes a Base94 string to string using specified encoding
        /// </summary>
        /// <param name="base94">Base94 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeToString(string base94, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] data = Decode(base94);
            return GetEncoding(encoding).GetString(data);
        }

        /// <summary>
        /// Returns the Base94 character set
        /// </summary>
        /// <returns>String containing all Base94 characters</returns>
        public override string ToString() => Base94Chars;
    }
}
#endif
