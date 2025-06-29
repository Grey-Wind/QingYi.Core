#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
using System;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base56 encoding and decoding functionality
    /// </summary>
    public unsafe class Base56
    {
        // Base56 character set (excludes easily confused characters: 0,1,O,I,l)
        private const string Base56Chars = "23456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        // Character to index mapping for decoding
        private static readonly int[] CharToIndexMap = new int[128];

        /// <summary>
        /// Static constructor initializes the character to index mapping
        /// </summary>
        static Base56()
        {
            // Initialize all entries as invalid (-1)
            Array.Fill(CharToIndexMap, -1);
            // Populate valid Base56 characters
            for (int i = 0; i < Base56Chars.Length; i++)
            {
                CharToIndexMap[Base56Chars[i]] = i;
            }
        }

        /// <summary>
        /// Gets the Base56 character set used for encoding
        /// </summary>
        /// <returns>The Base56 character set string</returns>
        public override string ToString() => Base56Chars;

        /// <summary>
        /// Encodes a string to Base56 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base56 encoded string</returns>
        public static string EncodeString(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            var bytes = GetEncoding(encoding).GetBytes(input);
            return Encode(bytes);
        }

        /// <summary>
        /// Decodes a Base56 string to text using the specified encoding
        /// </summary>
        /// <param name="base56String">Base56 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeString(string base56String, StringEncoding encoding = StringEncoding.UTF8)
        {
            var bytes = Decode(base56String);
            return GetEncoding(encoding).GetString(bytes);
        }

        /// <summary>
        /// Gets the encoding object for the specified encoding type
        /// </summary>
        /// <param name="encoding">Encoding type</param>
        /// <returns>Encoding object</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for unsupported encoding types</exception>
        private static Encoding GetEncoding(StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8,
                StringEncoding.UTF16LE => Encoding.Unicode,
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode,
                StringEncoding.ASCII => Encoding.ASCII,
                StringEncoding.UTF32 => Encoding.UTF32,
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1,
#endif
#pragma warning disable 0618, SYSLIB0001
                StringEncoding.UTF7 => Encoding.UTF7,
#pragma warning restore 0618, SYSLIB0001
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };
        }

        /// <summary>
        /// Encodes binary data to a Base56 string
        /// </summary>
        /// <param name="input">Binary data to encode</param>
        /// <returns>Base56 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        public static unsafe string Encode(byte[] input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length == 0) return string.Empty;

            // Count leading zero bytes (will be represented as leading '2's)
            int leadingZeros = CountLeadingZeros(input);
            int dataLength = input.Length - leadingZeros;

            // Special case: all zeros
            if (dataLength == 0) return new string(Base56Chars[0], leadingZeros);

            fixed (byte* pInput = input)
            {
                // Estimate output size (1.4x input length) + 1 for safety
                int outputSize = (int)(dataLength * 1.4) + 1;
                char* outputBuffer = stackalloc char[outputSize];
                int outputIndex = outputSize;

                // Buffer for intermediate calculations
                int bufferSize = dataLength * 2;
                int* buffer = stackalloc int[bufferSize];
                int bufferIndex = 0;

                // Process each byte (except leading zeros)
                for (int i = leadingZeros; i < input.Length; i++)
                {
                    int carry = pInput[i];
                    // Process existing buffer entries
                    for (int j = 0; j < bufferIndex; j++)
                    {
                        carry += buffer[j] << 8;
                        buffer[j] = carry % 56;
                        carry /= 56;
                    }

                    // Process remaining carry
                    while (carry > 0)
                    {
                        buffer[bufferIndex++] = carry % 56;
                        carry /= 56;
                    }
                }

                // Prepare final result (leading zeros + converted bytes)
                int totalLength = leadingZeros + bufferIndex;
                char* result = stackalloc char[totalLength];
                int resultIndex = 0;

                // Add leading zeros (represented as '2's)
                for (int i = 0; i < leadingZeros; i++)
                {
                    result[resultIndex++] = Base56Chars[0];
                }

                // Add converted bytes (in reverse order)
                for (int i = bufferIndex - 1; i >= 0; i--)
                {
                    result[resultIndex++] = Base56Chars[buffer[i]];
                }

                return new string(result, 0, totalLength);
            }
        }

        /// <summary>
        /// Decodes a Base56 string to binary data
        /// </summary>
        /// <param name="input">Base56 encoded string</param>
        /// <returns>Decoded binary data</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        /// <exception cref="FormatException">Thrown for invalid Base56 characters</exception>
        public static unsafe byte[] Decode(string input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length == 0) return Array.Empty<byte>();

            // Count leading '2's (representing zero bytes)
            int leadingZeros = 0;
            while (leadingZeros < input.Length && input[leadingZeros] == Base56Chars[0])
            {
                leadingZeros++;
            }

            fixed (char* pInput = input)
            {
                // Estimate output size (0.73x input length) + 1 for safety
                int bufferSize = (int)(input.Length * 0.73) + 1;
                byte* buffer = stackalloc byte[bufferSize];
                int bufferIndex = 0;

                // Process each character (except leading '2's)
                for (int i = leadingZeros; i < input.Length; i++)
                {
                    char c = pInput[i];
                    // Validate character
                    int digit = c < 128 ? CharToIndexMap[c] : -1;
                    if (digit == -1) throw new FormatException($"Invalid character '{c}'");

                    int carry = digit;
                    // Process existing buffer entries
                    for (int j = 0; j < bufferIndex; j++)
                    {
                        carry += buffer[j] * 56;
                        buffer[j] = (byte)(carry & 0xFF);
                        carry >>= 8;
                    }

                    // Process remaining carry
                    while (carry > 0)
                    {
                        buffer[bufferIndex++] = (byte)(carry & 0xFF);
                        carry >>= 8;
                    }
                }

                // Prepare final result (leading zeros + converted bytes)
                byte[] result = new byte[leadingZeros + bufferIndex];

                // Set leading zeros
                for (int i = 0; i < leadingZeros; i++)
                {
                    result[i] = 0;
                }

                // Add converted bytes (in reverse order)
                for (int i = 0; i < bufferIndex; i++)
                {
                    result[leadingZeros + bufferIndex - 1 - i] = buffer[i];
                }

                return result;
            }
        }

        /// <summary>
        /// Counts the number of leading zero bytes in the input
        /// </summary>
        /// <param name="input">Byte array to analyze</param>
        /// <returns>Number of leading zero bytes</returns>
        private static int CountLeadingZeros(byte[] input)
        {
            int count = 0;
            foreach (byte b in input)
            {
                if (b != 0) break;
                count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Provides extension methods for Base56 encoding/decoding
    /// </summary>
    public static class Base56Extension
    {
        /// <summary>
        /// Encodes a string to Base56 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base56 encoded string</returns>
        public static string EncodeBase56(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base56.EncodeString(input, encoding);

        /// <summary>
        /// Decodes a Base56 string to text using the specified encoding
        /// </summary>
        /// <param name="input">Base56 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeBase56(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base56.DecodeString(input, encoding);

        /// <summary>
        /// Encodes binary data to a Base56 string
        /// </summary>
        /// <param name="input">Binary data to encode</param>
        /// <returns>Base56 encoded string</returns>
        public static string EncodeBase56(this byte[] input) => Base56.Encode(input);

        /// <summary>
        /// Decodes a Base56 string to binary data
        /// </summary>
        /// <param name="input">Base56 encoded string</param>
        /// <returns>Decoded binary data</returns>
        public static byte[] DecodeBase56(this string input) => Base56.Decode(input);
    }
}
#endif
