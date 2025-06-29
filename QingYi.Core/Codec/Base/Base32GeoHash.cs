#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
using System;
using System.Text;

#pragma warning disable SYSLIB0001, CS0618

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base32 encoding/decoding using the Geohash alphabet.
    /// This variant uses a modified Base32 alphabet (0123456789bcdefghjkmnpqrstuvwxyz)
    /// specifically designed for geohashing applications.
    /// </summary>
    public class Base32GeoHash
    {
        // Geohash-specific Base32 alphabet (no 'a', 'i', 'l', 'o')
        private const string Base32Chars = "0123456789bcdefghjkmnpqrstuvwxyz";

        // Pre-calculated encoding table
        private static readonly char[] EncodeTable = Base32Chars.ToCharArray();

        // Decoding lookup table (128 ASCII characters)
        private static readonly byte[] DecodeTable = new byte[128];

        /// <summary>
        /// Static constructor initializes the decoding lookup table.
        /// </summary>
        static Base32GeoHash()
        {
            // Mark all characters as invalid initially
            Array.Fill(DecodeTable, (byte)0xFF);

            // Map valid Geohash characters to their 5-bit values
            for (byte i = 0; i < EncodeTable.Length; i++)
            {
                DecodeTable[EncodeTable[i]] = i;
            }
        }

        /// <summary>
        /// Gets the character set used for Geohash Base32 encoding.
        /// </summary>
        /// <returns>The Base32 alphabet string.</returns>
        public override string ToString() => Base32Chars;

        /// <summary>
        /// Encodes a string using Geohash Base32 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The Base32 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Convert string to bytes using specified encoding
            byte[] bytes = GetBytes(input, encoding);
            int byteCount = bytes.Length;

            // Calculate output size: ceil(inputBits / 5)
            int bitCount = byteCount * 8;
            int charCount = (bitCount + 4) / 5;
            char[] result = new char[charCount];

            // Use unsafe context for maximum performance
            unsafe
            {
                fixed (byte* bytesPtr = bytes)
                fixed (char* resultPtr = result)
                {
                    byte* currentByte = bytesPtr;
                    byte* endByte = bytesPtr + byteCount;
                    char* currentChar = resultPtr;

                    // Bit buffer for accumulating bits across byte boundaries
                    int buffer = 0;
                    int bufferBits = 0;

                    while (currentByte < endByte)
                    {
                        // Accumulate 8 bits from current byte
                        buffer = (buffer << 8) | *currentByte++;
                        bufferBits += 8;

                        // Extract 5-bit chunks while we have enough bits
                        while (bufferBits >= 5)
                        {
                            bufferBits -= 5;
                            int index = (buffer >> bufferBits) & 0x1F;
                            *currentChar++ = EncodeTable[index];
                        }
                    }

                    // Handle remaining bits (less than 5)
                    if (bufferBits > 0)
                    {
                        int index = (buffer << (5 - bufferBits)) & 0x1F;
                        *currentChar++ = EncodeTable[index];
                    }
                }
            }

            return new string(result);
        }

        /// <summary>
        /// Decodes a Geohash Base32 encoded string.
        /// </summary>
        /// <param name="base32Input">The Base32 string to decode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The decoded original string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if base32Input is null.</exception>
        /// <exception cref="FormatException">Thrown for invalid Base32 characters.</exception>
        public static string Decode(string base32Input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (base32Input == null)
                throw new ArgumentNullException(nameof(base32Input));

            int charCount = base32Input.Length;

            // Calculate output size: floor(inputBits / 8)
            int bitCount = charCount * 5;
            int byteCount = bitCount / 8;
            byte[] bytes = new byte[byteCount];

            // Use unsafe context for maximum performance
            unsafe
            {
                fixed (char* inputPtr = base32Input)
                fixed (byte* bytesPtr = bytes)
                {
                    char* currentChar = inputPtr;
                    char* endChar = inputPtr + charCount;
                    byte* currentByte = bytesPtr;
                    byte* endByte = bytesPtr + byteCount;

                    // Bit buffer for accumulating bits across character boundaries
                    int buffer = 0;
                    int bufferBits = 0;

                    while (currentChar < endChar)
                    {
                        char c = *currentChar++;

                        // Validate character is in Geohash alphabet
                        if (c >= DecodeTable.Length || DecodeTable[c] == 0xFF)
                            throw new FormatException($"Invalid Base32 character: '{c}'");

                        // Accumulate 5 bits from current character
                        int value = DecodeTable[c];
                        buffer = (buffer << 5) | value;
                        bufferBits += 5;

                        // Extract 8-bit bytes while we have enough bits
                        while (bufferBits >= 8 && currentByte < endByte)
                        {
                            bufferBits -= 8;
                            *currentByte++ = (byte)((buffer >> bufferBits) & 0xFF);
                        }
                    }
                }
            }

            return GetString(bytes, encoding);
        }

        /// <summary>
        /// Converts a string to bytes using the specified encoding.
        /// </summary>
        private static byte[] GetBytes(string input, StringEncoding encoding)
        {
            Encoding encoder = encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8,
                StringEncoding.UTF16LE => Encoding.Unicode,
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode,
                StringEncoding.UTF32 => Encoding.UTF32,
                StringEncoding.UTF7 => Encoding.UTF7,
                StringEncoding.ASCII => Encoding.ASCII,
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1,
#endif
                _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
            };
            return encoder.GetBytes(input);
        }

        /// <summary>
        /// Converts bytes to a string using the specified encoding.
        /// </summary>
        private static string GetString(byte[] bytes, StringEncoding encoding)
        {
            Encoding decoder = encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8,
                StringEncoding.UTF16LE => Encoding.Unicode,
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode,
                StringEncoding.UTF32 => Encoding.UTF32,
                StringEncoding.UTF7 => Encoding.UTF7,
                StringEncoding.ASCII => Encoding.ASCII,
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1,
#endif
                _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
            };
            return decoder.GetString(bytes);
        }
    }
}
#endif
