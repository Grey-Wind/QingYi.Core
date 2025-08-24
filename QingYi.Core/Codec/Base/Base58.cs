using System.Text;
using System;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base58 encoding and decoding functionality
    /// </summary>
    public unsafe class Base58
    {
        // Base58 character set (excludes similar looking characters: 0, O, I, l)
        private const string Base58Chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        // Lookup table for character to value mapping
        private static readonly int[] Alphabet = new int[256];
        // Flag indicating if the alphabet has been initialized
        private static readonly bool AlphabetInitialized;

        /// <summary>
        /// Static constructor initializes the alphabet lookup table
        /// </summary>
        static Base58()
        {
            if (AlphabetInitialized) return;

            // Initialize all entries as invalid (-1)
            for (int i = 0; i < Alphabet.Length; i++)
                Alphabet[i] = -1;

            // Populate valid Base58 characters
            for (int i = 0; i < Base58Chars.Length; i++)
                Alphabet[Base58Chars[i]] = i;

            AlphabetInitialized = true;
        }

        /// <summary>
        /// Gets the Base58 character set used for encoding
        /// </summary>
        /// <returns>The Base58 character set string</returns>
        public override string ToString() => Base58Chars;

        /// <summary>
        /// Encodes a string to Base58 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base58 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var bytes = GetBytes(input, encoding);
            return Encode(bytes);
        }

        /// <summary>
        /// Decodes a Base58 string to text using the specified encoding
        /// </summary>
        /// <param name="input">Base58 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        public static string Decode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var bytes = DecodeToBytes(input);
            return GetString(bytes, encoding);
        }

        /// <summary>
        /// Encodes binary data to a Base58 string
        /// </summary>
        /// <param name="input">Binary data to encode</param>
        /// <returns>Base58 encoded string</returns>
        internal static unsafe string Encode(byte[] input)
        {
            if (input.Length == 0) return string.Empty;

            // Count leading zero bytes (will be represented as leading '1's)
            int leadingZeros = 0;
            while (leadingZeros < input.Length && input[leadingZeros] == 0)
                leadingZeros++;

            fixed (byte* inputPtr = input)
            {
                int length = input.Length;
                // Calculate output buffer size (log58(256) ≈ 1.38)
                int count = (length - leadingZeros) * 138 / 100 + 1;
                byte[] temp = new byte[count];
                int outputIndex = count;

                fixed (byte* tempPtr = temp)
                {
                    // Process each byte (except leading zeros)
                    for (int i = leadingZeros; i < length; i++)
                    {
                        int carry = inputPtr[i];
                        int j = count - 1;
                        // Convert to Base58 by repeated division
                        while (carry != 0 || j >= outputIndex)
                        {
                            carry += tempPtr[j] << 8;
                            tempPtr[j] = (byte)(carry % 58);
                            carry /= 58;
                            j--;
                        }
                        outputIndex = j + 1;
                    }
                }

                // Skip leading zeros in the temporary buffer
                int startIndex = outputIndex;
                while (startIndex < count && temp[startIndex] == 0)
                    startIndex++;

                // Allocate result buffer (leading '1's + converted bytes)
                char* resultPtr = stackalloc char[leadingZeros + (count - startIndex)];

                // Add leading '1's for leading zeros
                for (int i = 0; i < leadingZeros; i++)
                    resultPtr[i] = '1';

                // Add converted bytes
                for (int i = leadingZeros; i < leadingZeros + count - startIndex; i++)
                    resultPtr[i] = Base58Chars[temp[startIndex + i - leadingZeros]];

                return new string(resultPtr, 0, leadingZeros + count - startIndex);
            }
        }

        /// <summary>
        /// Decodes a Base58 string to binary data
        /// </summary>
        /// <param name="input">Base58 encoded string</param>
        /// <returns>Decoded binary data</returns>
        /// <exception cref="FormatException">Thrown for invalid Base58 characters</exception>
        internal static unsafe byte[] DecodeToBytes(string input)
        {
            if (input.Length == 0)
#if NET45 || NET451 || NET452
                return new byte[] { };
#else
                return Array.Empty<byte>();
#endif

            // Count leading '1's (representing leading zero bytes)
            int leadingOnes = 0;
            while (leadingOnes < input.Length && input[leadingOnes] == '1')
                leadingOnes++;

            fixed (char* inputPtr = input)
            {
                int length = input.Length;
                byte[] indices = new byte[length - leadingOnes];

                // Convert characters to their numerical values
                for (int i = leadingOnes; i < length; i++)
                {
                    char c = inputPtr[i];
                    int value = c < 0 || c >= Alphabet.Length ? -1 : Alphabet[c];
                    if (value == -1)
                        throw new FormatException($"Invalid Base58 character '{c}'");
                    indices[i - leadingOnes] = (byte)value;
                }

                // Calculate output buffer size (log256(58) ≈ 0.733)
                int count = (length - leadingOnes) * 733 / 1000 + 1;
                byte[] temp = new byte[count];
                int outputIndex = count;

                fixed (byte* indicesPtr = indices, tempPtr = temp)
                {
                    // Process each Base58 digit
                    for (int i = 0; i < indices.Length; i++)
                    {
                        int carry = indicesPtr[i];
                        int j = count - 1;
                        // Convert back to bytes by repeated multiplication
                        while (carry != 0 || j >= outputIndex)
                        {
                            carry += tempPtr[j] * 58;
                            tempPtr[j] = (byte)(carry & 0xFF);
                            carry >>= 8;
                            j--;
                        }
                        outputIndex = j + 1;
                    }
                }

                // Skip leading zeros in the temporary buffer
                int startIndex = outputIndex;
                while (startIndex < count && temp[startIndex] == 0)
                    startIndex++;

                // Prepare final result (leading zeros + converted bytes)
                byte[] result = new byte[leadingOnes + (count - startIndex)];

                // Set leading zeros
                for (int i = 0; i < leadingOnes; i++)
                    result[i] = 0;

                // Copy converted bytes
                Buffer.BlockCopy(temp, startIndex, result, leadingOnes, count - startIndex);
                return result;
            }
        }

        /// <summary>
        /// Gets the encoding object for the specified encoding type
        /// </summary>
        /// <param name="encoding">Encoding type</param>
        /// <returns>Encoding object</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for unsupported encoding types</exception>
        private static Encoding GetEncoding(StringEncoding encoding)
        {
            switch (encoding)
            {
                case StringEncoding.UTF8:
                    return Encoding.UTF8;
                case StringEncoding.UTF16LE:
                    return Encoding.Unicode;
                case StringEncoding.UTF16BE:
                    return Encoding.BigEndianUnicode;
                case StringEncoding.UTF32:
                    return Encoding.UTF32;
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1:
                    return Encoding.GetEncoding(28591);
#endif
                case StringEncoding.ASCII:
                    return Encoding.ASCII;
#pragma warning disable CS0618, SYSLIB0001
                case StringEncoding.UTF7:
                    return Encoding.UTF7;
#pragma warning restore CS0618, SYSLIB0001
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Converts string to bytes using specified encoding
        /// </summary>
        private static byte[] GetBytes(string input, StringEncoding encoding) => GetEncoding(encoding).GetBytes(input);

        /// <summary>
        /// Converts bytes to string using specified encoding
        /// </summary>
        private static string GetString(byte[] bytes, StringEncoding encoding) => GetEncoding(encoding).GetString(bytes);
    }

    /// <summary>
    /// Provides extension methods for Base58 encoding/decoding
    /// </summary>
    public static class Base58Extension
    {
        /// <summary>
        /// Encodes a string to Base58 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base58 encoded string</returns>
        public static string Encode(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base58.Encode(input, encoding);

        /// <summary>
        /// Decodes a Base58 string to text using the specified encoding
        /// </summary>
        /// <param name="input">Base58 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string Decode(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base58.Decode(input, encoding);

        /// <summary>
        /// Encodes binary data to a Base58 string
        /// </summary>
        /// <param name="input">Binary data to encode</param>
        /// <returns>Base58 encoded string</returns>
        public static string Encode(this byte[] input) => Base58.Encode(input);

        /// <summary>
        /// Decodes a Base58 string to binary data
        /// </summary>
        /// <param name="input">Base58 encoded string</param>
        /// <returns>Decoded binary data</returns>
        public static byte[] Decode(this string input) => Base58.DecodeToBytes(input);
    }
}
