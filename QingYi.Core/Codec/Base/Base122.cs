#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base122 encoding and decoding functionality.
    /// </summary>
    /// <remarks>
    /// Base122 is a binary-to-text encoding scheme that uses 122 different characters
    /// to represent binary data in a more compact form than Base64.
    /// </remarks>
    public sealed class Base122
    {
        /// <summary>
        /// Specifies the variant of Base122 encoding.
        /// </summary>
        public enum Variant
        {
            /// <summary>
            /// Standard Base122 encoding as originally specified.
            /// </summary>
            Standard
        }

        private const int BLOCK_BITS = 56;
        private const int BLOCK_BYTES = 7;
        private const int BLOCK_CHARS = 8;

        private static readonly char[] EncodingTable;
        private static readonly Dictionary<char, byte> DecodingTable;

        static Base122()
        {
            EncodingTable = new char[122];
            DecodingTable = new Dictionary<char, byte>(122);

            int index = 0;
            // Add characters from U+0080 to U+00FF range (skipping control and special characters)
            for (int i = 0x80; i <= 0xFF; i++)
            {
                char c = (char)i;

                // Skip control characters and problematic characters
                if (IsValidBase122Char(c))
                {
                    EncodingTable[index] = c;
                    DecodingTable[c] = (byte)index;
                    index++;
                }
            }

            // Add supplementary characters
            char[] supplementaryChars = {
            '\u2500', '\u2501', '\u2502', '\u2503', '\u2504', '\u2505',
            '\u2506', '\u2507', '\u2508', '\u2509', '\u250A', '\u250B'
        };

            foreach (char c in supplementaryChars)
            {
                if (index >= 122) break;

                EncodingTable[index] = c;
                DecodingTable[c] = (byte)index;
                index++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsValidBase122Char(char c)
        {
            // Skip control characters, double quotes and backslashes
            return !char.IsControl(c) && c != '"' && c != '\\';
        }

        /// <summary>
        /// Returns the character set used for Base122 encoding.
        /// </summary>
        /// <returns>A string containing all 122 characters used in the encoding.</returns>
        public override string ToString() => new string(EncodingTable);

        /// <summary>
        /// Encodes a byte array into a Base122 string using the standard variant.
        /// </summary>
        /// <param name="input">The byte array to encode.</param>
        /// <returns>The Base122 encoded string.</returns>
        public static string Encode(byte[] input) => Encode(input, Variant.Standard);

        /// <summary>
        /// Encodes a byte array into a Base122 string using the specified variant.
        /// </summary>
        /// <param name="input">The byte array to encode.</param>
        /// <param name="variant">The encoding variant to use.</param>
        /// <returns>The Base122 encoded string.</returns>
        public static unsafe string Encode(byte[] input, Variant variant)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length == 0) return string.Empty;

            int outputLength = (input.Length + BLOCK_BYTES - 1) / BLOCK_BYTES * BLOCK_CHARS;
            char[] outputBuffer = new char[outputLength];

            fixed (byte* inputPtr = input)
            fixed (char* outputPtr = outputBuffer)
            fixed (char* tablePtr = EncodingTable)
            {
                byte* inPtr = inputPtr;
                char* outPtr = outputPtr;

                int remaining = input.Length;
                while (remaining >= BLOCK_BYTES)
                {
                    EncodeBlock(inPtr, outPtr, tablePtr);
                    inPtr += BLOCK_BYTES;
                    outPtr += BLOCK_CHARS;
                    remaining -= BLOCK_BYTES;
                }

                if (remaining > 0)
                {
                    byte* tempBlock = stackalloc byte[BLOCK_BYTES];
                    Buffer.MemoryCopy(inPtr, tempBlock, BLOCK_BYTES, remaining);
                    EncodeBlock(tempBlock, outPtr, tablePtr);
                }
            }

            return new string(outputBuffer);
        }

        private static unsafe void EncodeBlock(byte* input, char* output, char* table)
        {
            ulong block = 0;

            // Load 7 bytes as a 56-bit integer (big-endian)
            for (int i = 0; i < BLOCK_BYTES; i++)
            {
                block = (block << 8) | input[i];
            }

            // Extract eight 7-bit groups
            for (int i = 0; i < BLOCK_CHARS; i++)
            {
                int shift = BLOCK_BITS - (i + 1) * 7;
                byte value = (byte)((block >> shift) & 0x7F);
                output[i] = table[value];
            }
        }

        /// <summary>
        /// Encodes a string into a Base122 string using the specified text encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use for converting the string to bytes.</param>
        /// <returns>The Base122 encoded string.</returns>
        public static string Encode(string input, StringEncoding encoding) =>
            Encode(GetBytes(input, encoding));

        /// <summary>
        /// Decodes a Base122 string into a byte array using the standard variant.
        /// </summary>
        /// <param name="input">The Base122 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        public static byte[] Decode(string input) => Decode(input, Variant.Standard);

        /// <summary>
        /// Decodes a Base122 string into a byte array using the specified variant.
        /// </summary>
        /// <param name="input">The Base122 string to decode.</param>
        /// <param name="variant">The decoding variant to use.</param>
        /// <returns>The decoded byte array.</returns>
        public static unsafe byte[] Decode(string input, Variant variant)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length == 0) return Array.Empty<byte>();
            if (input.Length % BLOCK_CHARS != 0)
                throw new ArgumentException("Invalid Base122 input length");

            int outputLength = input.Length / BLOCK_CHARS * BLOCK_BYTES;
            byte[] outputBuffer = new byte[outputLength];

            fixed (char* inputPtr = input)
            fixed (byte* outputPtr = outputBuffer)
            {
                char* inPtr = inputPtr;
                byte* outPtr = outputPtr;

                int blocks = input.Length / BLOCK_CHARS;
                for (int i = 0; i < blocks; i++)
                {
                    DecodeBlock(inPtr, outPtr);
                    inPtr += BLOCK_CHARS;
                    outPtr += BLOCK_BYTES;
                }
            }

            return outputBuffer;
        }

        private static unsafe void DecodeBlock(char* input, byte* output)
        {
            ulong block = 0;

            for (int i = 0; i < BLOCK_CHARS; i++)
            {
                char c = input[i];
                if (!DecodingTable.TryGetValue(c, out byte value))
                    throw new FormatException($"Invalid Base122 character: {c}");

                block = (block << 7) | value;
            }

            // Extract 7 bytes
            for (int i = 0; i < BLOCK_BYTES; i++)
            {
                int shift = BLOCK_BITS - (i + 1) * 8;
                output[i] = (byte)((block >> shift) & 0xFF);
            }
        }

        /// <summary>
        /// Decodes a Base122 string into the original string using the specified text encoding.
        /// </summary>
        /// <param name="input">The Base122 string to decode.</param>
        /// <param name="encoding">The text encoding to use for converting the bytes to a string.</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeToString(string input, StringEncoding encoding) =>
            GetString(Decode(input), encoding);

        private static byte[] GetBytes(string input, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8.GetBytes(input),
                StringEncoding.UTF16LE => Encoding.Unicode.GetBytes(input),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetBytes(input),
                StringEncoding.ASCII => Encoding.ASCII.GetBytes(input),
                StringEncoding.UTF32 => Encoding.UTF32.GetBytes(input),
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetBytes(input),
#endif
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => Encoding.UTF7.GetBytes(input),
#pragma warning restore SYSLIB0001, CS0618
                _ => throw new NotSupportedException("Unsupported encoding")
            };
        }

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
                StringEncoding.UTF7 => Encoding.UTF7.GetString(bytes),
#pragma warning restore SYSLIB0001, CS0618
                _ => throw new NotSupportedException("Unsupported encoding")
            };
        }
    }
}
#endif
