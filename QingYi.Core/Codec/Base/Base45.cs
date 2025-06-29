using System;
using System.Text;

#pragma warning disable CA1510, CS0618, SYSLIB0001, IDE0300, IDE0301

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base45 encoding and decoding functionality
    /// </summary>
    public unsafe class Base45
    {
        // Base45 character set (45 characters)
        private const string EncodingTable = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
        // Decoding lookup table (maps characters to their values)
        private static readonly byte[] DecodingTable = new byte[256];

        /// <summary>
        /// Static constructor initializes the decoding table
        /// </summary>
        static Base45()
        {
            // Initialize all entries as invalid (0xFF)
            for (int i = 0; i < DecodingTable.Length; i++)
                DecodingTable[i] = 0xFF;

            // Populate valid characters in the decoding table
            for (byte i = 0; i < EncodingTable.Length; i++)
            {
                char c = EncodingTable[i];
                DecodingTable[c] = i;
            }
        }

        /// <summary>
        /// Gets the Base45 character set used for encoding
        /// </summary>
        /// <returns>The Base45 character set string</returns>
        public override string ToString() => EncodingTable;

        /// <summary>
        /// Encodes a string to Base45 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base45 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            byte[] bytes = GetEncoding(encoding).GetBytes(input);
            return EncodeBytes(bytes);
        }

        /// <summary>
        /// Decodes a Base45 string to text using the specified encoding
        /// </summary>
        /// <param name="input">Base45 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null</exception>
        /// <exception cref="ArgumentException">Thrown for invalid Base45 strings</exception>
        public static string Decode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            byte[] bytes = DecodeString(input);
            return GetEncoding(encoding).GetString(bytes);
        }

        /// <summary>
        /// Encodes binary data to a Base45 string
        /// </summary>
        /// <param name="b">Binary data to encode</param>
        /// <returns>Base45 encoded string</returns>
        private static string EncodeBytes(byte[] b)
        {
            int inputLength = b.Length;
            // Calculate output length: 2 bytes become 3 chars, 1 byte becomes 2 chars
            int outputLength = inputLength / 2 * 3 + (inputLength % 2 == 1 ? 2 : 0);
            char[] output = new char[outputLength];

            unsafe
            {
                fixed (byte* inputPtr = b)
                fixed (char* outputPtr = output)
                {
                    byte* ip = inputPtr;
                    char* op = outputPtr;

                    // Process complete 2-byte pairs
                    for (int i = 0; i < inputLength - 1; i += 2)
                    {
                        // Combine 2 bytes into a 16-bit value
                        int value = *ip++ << 8 | *ip++;
                        // Split into 3 Base45 digits
                        int d3 = value % 45;
                        value /= 45;
                        int d2 = value % 45;
                        value /= 45;
                        int d1 = value;

                        // Convert digits to characters
                        *op++ = EncodingTable[d1];
                        *op++ = EncodingTable[d2];
                        *op++ = EncodingTable[d3];
                    }

                    // Process remaining single byte if input length is odd
                    if (inputLength % 2 == 1)
                    {
                        int value = *ip;
                        // Split into 2 Base45 digits
                        int d2 = value % 45;
                        value /= 45;
                        int d1 = value;

                        // Convert digits to characters
                        *op++ = EncodingTable[d1];
                        *op++ = EncodingTable[d2];
                    }
                }
            }
            return new string(output);
        }

        /// <summary>
        /// Decodes a Base45 string to binary data
        /// </summary>
        /// <param name="input">Base45 encoded string</param>
        /// <returns>Decoded binary data</returns>
        /// <exception cref="ArgumentException">Thrown for invalid Base45 strings</exception>
        /// <exception cref="FormatException">Thrown for invalid Base45 characters or values</exception>
        private static byte[] DecodeString(string input)
        {
            int inputLength = input.Length;
            if (inputLength == 0) return Array.Empty<byte>();

            int remainder = inputLength % 3;
            // Base45 requires length to be multiple of 3 or remainder 2
            if (remainder == 1)
                throw new ArgumentException("Invalid Base45 string length.");

            // Calculate output length: 3 chars become 2 bytes, 2 chars become 1 byte
            int outputLength = inputLength / 3 * 2 + (remainder == 2 ? 1 : 0);
            byte[] output = new byte[outputLength];

            unsafe
            {
                fixed (char* inputPtr = input)
                fixed (byte* outputPtr = output)
                {
                    char* inPtr = inputPtr;
                    byte* outPtr = outputPtr;

                    // Process complete triplets
                    for (int i = 0; i < inputLength - remainder; i += 3)
                    {
                        // Get values for each character
                        int d1 = GetValue(*inPtr++);
                        int d2 = GetValue(*inPtr++);
                        int d3 = GetValue(*inPtr++);

                        // Combine into original value
                        int value = d1 * 45 * 45 + d2 * 45 + d3;
                        if (value > 0xFFFF) throw new FormatException("Invalid Base45 triplet.");

                        // Split into 2 bytes
                        *outPtr++ = (byte)(value >> 8);
                        *outPtr++ = (byte)value;
                    }

                    // Process remaining pair if exists
                    if (remainder == 2)
                    {
                        int d1 = GetValue(*inPtr++);
                        int d2 = GetValue(*inPtr++);

                        int value = d1 * 45 + d2;
                        if (value > 0xFF) throw new FormatException("Invalid Base45 pair.");
                        *outPtr++ = (byte)value;
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Gets the numerical value of a Base45 character
        /// </summary>
        /// <param name="c">Character to decode</param>
        /// <returns>Numerical value of the character</returns>
        /// <exception cref="FormatException">Thrown for invalid Base45 characters</exception>
        private static int GetValue(char c)
        {
            // Only ASCII characters are valid
            if (c > 255)
                throw new FormatException($"Invalid character '{(int)c}'");

            byte v = DecodingTable[c]; // Get decoded value

            if (v == 0xFF) // 0xFF marks invalid characters
                throw new FormatException($"Invalid character '{c}'");

            return v;
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
                case StringEncoding.UTF7:
                    return Encoding.UTF7;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Provides extension methods for Base45 encoding/decoding of strings
    /// </summary>
    public static class Base45Extension
    {
        /// <summary>
        /// Encodes a string to Base45 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base45 encoded string</returns>
        public static string EncodeBase45(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base45.Encode(input, encoding);

        /// <summary>
        /// Decodes a Base45 string to text using the specified encoding
        /// </summary>
        /// <param name="input">Base45 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeBase45(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base45.Decode(input, encoding);
    }
}
