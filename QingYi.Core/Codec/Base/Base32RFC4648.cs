using System;
using System.Text;

#pragma warning disable CA1510, CS0618, SYSLIB0001, IDE0300, IDE0301

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Implements Base32 encoding/decoding according to RFC 4648 specification.
    /// This is the standard Base32 implementation using A-Z and 2-7 characters with '=' padding.
    /// </summary>
    public class Base32RFC4648
    {
        // Standard Base32 alphabet as defined in RFC 4648
        private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        // Lookup table for decoding characters (maps ASCII values to 5-bit values)
        private static readonly byte[] DecodeTable = new byte[128];

        /// <summary>
        /// Static constructor initializes the decoding lookup table.
        /// </summary>
        static Base32RFC4648()
        {
            // Initialize all entries as invalid (0xFF)
            for (int i = 0; i < DecodeTable.Length; i++)
                DecodeTable[i] = 0xFF;

            // Map valid Base32 characters to their 5-bit values
            for (int i = 0; i < Base32Chars.Length; i++)
                DecodeTable[Base32Chars[i]] = (byte)i;
        }

        /// <summary>
        /// Gets the character set used for RFC 4648 Base32 encoding.
        /// </summary>
        /// <returns>The Base32 alphabet string (A-Z, 2-7).</returns>
        public override string ToString() => Base32Chars;

        /// <summary>
        /// Encodes a string using standard RFC 4648 Base32 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The Base32 encoded string with proper padding.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        public static string Encode(string input, StringEncoding encoding)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Convert string to bytes using specified encoding
            byte[] bytes = GetBytes(input, encoding);
            return EncodeInternal(bytes);
        }

        /// <summary>
        /// Decodes a standard RFC 4648 Base32 encoded string.
        /// </summary>
        /// <param name="input">The Base32 string to decode (must be length multiple of 8).</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The decoded original string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        /// <exception cref="FormatException">
        /// Thrown for invalid length, padding, or characters.
        /// </exception>
        public static string Decode(string input, StringEncoding encoding)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length % 8 != 0)
                throw new FormatException("Input length must be a multiple of 8.");

            // Calculate padding and validate structure
            int paddingCount = CalculatePadding(input);
            int r = GetRemainingBytes(paddingCount);
            int validCharCount = input.Length - paddingCount;
            int validBits = validCharCount * 5 - GetTruncateBits(r);

            if (validBits % 8 != 0)
                throw new FormatException("Invalid data length.");

            // Decode to bytes then convert to string
            byte[] bytes = DecodeInternal(input, validBits / 8, paddingCount);
            return GetString(bytes, encoding);
        }

        /// <summary>
        /// Internal method to encode bytes to Base32 with proper padding.
        /// </summary>
        private static unsafe string EncodeInternal(byte[] bytes)
        {
            // Calculate output length (8 chars per 5 bytes)
            int outputLen = (bytes.Length + 4) / 5 * 8;
            char[] output = new char[outputLen];

            fixed (byte* inputPtr = bytes)
            fixed (char* outputPtr = output)
            {
                byte* pInput = inputPtr;
                char* pOutput = outputPtr;
                int remaining = bytes.Length;

                // Process complete 5-byte blocks (generates 8 chars each)
                while (remaining >= 5)
                {
                    EncodeBlock(pInput, pOutput);
                    pInput += 5;
                    pOutput += 8;
                    remaining -= 5;
                }

                // Handle remaining bytes (1-4 bytes)
                if (remaining > 0)
                    EncodeRemaining(pInput, remaining, pOutput, outputLen - (int)(pOutput - outputPtr));
            }
            return new string(output);
        }

        /// <summary>
        /// Encodes a complete 5-byte block to 8 Base32 characters.
        /// </summary>
        private static unsafe void EncodeBlock(byte* input, char* output)
        {
            // Combine 5 bytes into a 40-bit buffer
            ulong buffer = (ulong)input[0] << 32 | (ulong)input[1] << 24 |
                           (ulong)input[2] << 16 | (ulong)input[3] << 8 | input[4];

            // Extract eight 5-bit chunks (35-0 bits, skipping 5 each time)
            for (int i = 0; i < 8; i++)
                output[i] = Base32Chars[(int)(buffer >> (35 - i * 5) & 0x1F)];
        }

        /// <summary>
        /// Encodes remaining bytes (1-4) with proper padding.
        /// </summary>
        private static unsafe void EncodeRemaining(byte* input, int remaining, char* output, int outputRemaining)
        {
            // Combine remaining bytes into buffer (right-aligned)
            ulong buffer = 0;
            for (int i = 0; i < remaining; i++)
                buffer |= (ulong)input[i] << 8 * (4 - i);

            // Calculate how many characters we need (ceil(bits/5))
            int bits = remaining * 8;
            int charCount = (bits + 4) / 5;
            charCount = Math.Min(charCount, outputRemaining);

            // Write the characters
            for (int i = 0; i < charCount; i++)
                output[i] = Base32Chars[(int)(buffer >> (35 - i * 5) & 0x1F)];

            // Add padding if needed
            for (int i = charCount; i < 8; i++)
                output[i] = '=';
        }

        /// <summary>
        /// Internal method to decode Base32 string to bytes.
        /// </summary>
        private static unsafe byte[] DecodeInternal(string input, int byteCount, int paddingCount)
        {
            byte[] bytes = new byte[byteCount];

            fixed (char* inputPtr = input)
            fixed (byte* outputPtr = bytes)
            {
                char* pInput = inputPtr;
                byte* pOutput = outputPtr;
                int blocks = (input.Length - paddingCount) / 8;

                // Process complete 8-character blocks (5 bytes each)
                for (int i = 0; i < blocks; i++)
                {
                    DecodeBlock(pInput, pOutput);
                    pInput += 8;
                    pOutput += 5;
                }

                // Handle last block with padding if needed
                if (paddingCount > 0)
                    DecodeLastBlock(pInput, pOutput, paddingCount);
            }
            return bytes;
        }

        /// <summary>
        /// Decodes a complete 8-character block to 5 bytes.
        /// </summary>
        private static unsafe void DecodeBlock(char* input, byte* output)
        {
            // Combine eight 5-bit chunks into 40-bit buffer
            ulong buffer = 0;
            for (int i = 0; i < 8; i++)
            {
                char c = input[i];
                if (c >= 128 || DecodeTable[c] == 0xFF)
                    throw new FormatException($"Invalid character '{c}'.");

                buffer = buffer << 5 | DecodeTable[c];
            }

            // Extract 5 bytes from the buffer
            output[0] = (byte)(buffer >> 32);
            output[1] = (byte)(buffer >> 24);
            output[2] = (byte)(buffer >> 16);
            output[3] = (byte)(buffer >> 8);
            output[4] = (byte)buffer;
        }

        /// <summary>
        /// Decodes the last block which may contain padding characters.
        /// </summary>
        private static unsafe void DecodeLastBlock(char* input, byte* output, int paddingCount)
        {
            ulong buffer = 0;
            int validChars = 8 - paddingCount;

            // Combine valid characters into buffer
            for (int i = 0; i < validChars; i++)
            {
                char c = input[i];
                if (c >= 128 || DecodeTable[c] == 0xFF)
                    throw new FormatException($"Invalid character '{c}'.");

                buffer = buffer << 5 | DecodeTable[c];
            }

            // Shift left to account for padding
            buffer <<= paddingCount * 5;

            // Calculate how many bytes we can extract
            int bytesToWrite = validChars * 5 / 8;
            for (int i = 0; i < bytesToWrite; i++)
                output[i] = (byte)(buffer >> (32 - i * 8));
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
        /// Calculates number of padding characters at end of input.
        /// </summary>
        private static int CalculatePadding(string input)
        {
            int paddingCount = 0;
            for (int i = input.Length - 1; i >= 0 && input[i] == '='; i--)
                paddingCount++;
            return paddingCount;
        }

        /// <summary>
        /// Gets remaining bytes based on padding count (RFC 4648 rules).
        /// </summary>
        private static int GetRemainingBytes(int paddingCount)
        {
            switch (paddingCount)
            {
                case 0:
                    return 0;
                case 1:
                    return 4;
                case 3:
                    return 3;
                case 4:
                    return 2;
                case 6:
                    return 1;
                default:
                    throw new FormatException("Invalid padding.");
            }
        }

        /// <summary>
        /// Gets bits to truncate based on remaining bytes (RFC 4648 rules).
        /// </summary>
        private static int GetTruncateBits(int r)
        {
            switch (r)
            {
                case 0:
                    return 0;
                case 1:
                    return 2;
                case 2:
                    return 4;
                case 3:
                    return 1;
                case 4:
                    return 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(r));
            }
        }
    }
}
