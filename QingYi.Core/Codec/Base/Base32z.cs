using System.Text;
using System;

#pragma warning disable SYSLIB0001, CS0618

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Implements z-base-32 encoding/decoding, a human-friendly Base32 variant.
    /// This encoding is optimized for readability and error prevention in user-facing strings.
    /// </summary>
    public class Base32z
    {
        // z-base-32 alphabet (optimized for human use)
        private const string ZBase32Chars = "ybndrfg8ejkmcpqxot1uwisza345h769";

        // Reverse lookup table for decoding (maps characters to 5-bit values)
        private static readonly byte[] ReverseTable = new byte[128];

        /// <summary>
        /// Static constructor initializes the decoding lookup table.
        /// </summary>
        static Base32z()
        {
            // Initialize all entries as invalid (0xFF)
            for (int i = 0; i < ReverseTable.Length; i++)
                ReverseTable[i] = 0xFF;

            // Map valid z-base-32 characters to their 5-bit values
            for (byte i = 0; i < ZBase32Chars.Length; i++)
            {
                char c = ZBase32Chars[i];
                if (c >= ReverseTable.Length)
                    throw new InvalidOperationException("Invalid character in Z-Base-32 charset.");
                ReverseTable[c] = i;
            }
        }

        /// <summary>
        /// Gets the character set used for z-base-32 encoding.
        /// </summary>
        /// <returns>The z-base-32 alphabet string.</returns>
        public override string ToString() => ZBase32Chars;

        /// <summary>
        /// Encodes a string using z-base-32 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The z-base-32 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        public static string Encode(string input, StringEncoding encoding)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            // Convert string to bytes using specified encoding
            byte[] bytes = GetBytes(input, encoding);
            return EncodeToString(bytes);
        }

        /// <summary>
        /// Decodes a z-base-32 encoded string.
        /// </summary>
        /// <param name="base32">The z-base-32 string to decode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The decoded original string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if base32 is null.</exception>
        /// <exception cref="ArgumentException">Thrown for invalid z-base-32 strings.</exception>
        public static string Decode(string base32, StringEncoding encoding)
        {
            if (base32 == null)
                throw new ArgumentNullException(nameof(base32));

            // Convert z-base-32 to bytes then to string using specified encoding
            byte[] bytes = DecodeToBytes(base32);
            return GetString(bytes, encoding);
        }

        /// <summary>
        /// Gets bytes from string using specified encoding.
        /// </summary>
        private static byte[] GetBytes(string input, StringEncoding encoding)
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
                case StringEncoding.UTF7:
                    encoder = Encoding.UTF7;
                    break;
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
            return encoder.GetBytes(input);
        }

        /// <summary>
        /// Gets string from bytes using specified encoding.
        /// </summary>
        private static string GetString(byte[] bytes, StringEncoding encoding)
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
                case StringEncoding.UTF7:
                    decoder = Encoding.UTF7;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding));
            }
            return decoder.GetString(bytes);
        }

        /// <summary>
        /// Converts bytes to z-base-32 string.
        /// </summary>
        private static string EncodeToString(byte[] bytes)
        {
            if (bytes.Length == 0)
                return string.Empty;

            // Calculate output length: ceil(bitCount/5)
            int byteCount = bytes.Length;
            int outputLength = (byteCount * 8 + 4) / 5;
            char[] output = new char[outputLength];

            // Bit buffer for accumulating bits across byte boundaries
            ulong buffer = 0;
            int bitsInBuffer = 0;
            int outputPos = 0;

            // Use unsafe context for maximum performance
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    byte* current = ptr;
                    byte* end = ptr + byteCount;

                    while (current < end)
                    {
                        // Accumulate 8 bits from current byte
                        buffer = buffer << 8 | *current++;
                        bitsInBuffer += 8;

                        // Extract 5-bit chunks while we have enough bits
                        while (bitsInBuffer >= 5)
                        {
                            int index = (int)(buffer >> (bitsInBuffer - 5) & 0x1F);
                            output[outputPos++] = ZBase32Chars[index];
                            bitsInBuffer -= 5;
                            buffer &= (1UL << bitsInBuffer) - 1; // Mask off used bits
                        }
                    }
                }
            }

            // Handle remaining bits (less than 5)
            if (bitsInBuffer > 0)
            {
                buffer <<= 5 - bitsInBuffer;
                int index = (int)(buffer & 0x1F);
                output[outputPos++] = ZBase32Chars[index];
            }

            return new string(output);
        }

        /// <summary>
        /// Converts z-base-32 string to bytes.
        /// </summary>
        private static byte[] DecodeToBytes(string base32)
        {
            if (base32.Length == 0)
#if NET45 || NET451 || NET452
                return new byte[] { };
#else
                return Array.Empty<byte>();
#endif

            // Calculate output size: ceil(inputBits / 8)
            int inputLength = base32.Length;
            int bitCount = inputLength * 5;
            int byteCount = (bitCount + 7) / 8;
            byte[] output = new byte[byteCount];

            // Bit buffer for accumulating bits across character boundaries
            ulong buffer = 0;
            int bitsInBuffer = 0;
            int outputPos = 0;

            // Use unsafe context for maximum performance
            unsafe
            {
                fixed (char* inputPtr = base32)
                {
                    char* current = inputPtr;
                    char* end = inputPtr + inputLength;

                    while (current < end)
                    {
                        char c = *current++;

                        // Validate character is in z-base-32 alphabet
                        if (c >= ReverseTable.Length || ReverseTable[c] == 0xFF)
                            throw new ArgumentException($"Invalid character '{c}' in Base32 string.");

                        // Accumulate 5 bits from current character
                        byte value = ReverseTable[c];
                        buffer = buffer << 5 | value;
                        bitsInBuffer += 5;

                        // Extract 8-bit bytes while we have enough bits
                        while (bitsInBuffer >= 8)
                        {
                            byte b = (byte)(buffer >> (bitsInBuffer - 8));
                            output[outputPos++] = b;
                            bitsInBuffer -= 8;
                            buffer &= (1UL << bitsInBuffer) - 1; // Mask off used bits
                        }
                    }
                }
            }

            // Handle remaining bits (less than 8)
            if (bitsInBuffer > 0)
            {
                buffer <<= 8 - bitsInBuffer;
                output[outputPos++] = (byte)buffer;
            }

            // Trim output array if we didn't fill it completely
            if (outputPos < output.Length)
                Array.Resize(ref output, outputPos);

            return output;
        }
    }
}
