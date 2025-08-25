#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
using System;
using System.Text;

#pragma warning disable SYSLIB0001, CS0618

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Implements Base32 encoding/decoding using the Extended Hex alphabet (RFC 4648 §7).
    /// This variant uses digits 0-9 and letters A-V for a contiguous hexadecimal-like encoding.
    /// </summary>
    public class Base32ExtendedHex
    {
        // Extended Hex Base32 alphabet (0-9, A-V)
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV";

        // Padding character as specified in RFC 4648
        private const char PaddingChar = '=';

        // Lookup table for decoding characters (maps ASCII values to 5-bit values)
        private static readonly byte[] DecodeMap = new byte[256];

        /// <summary>
        /// Static constructor initializes the decoding lookup table.
        /// </summary>
        static Base32ExtendedHex()
        {
            // Initialize all entries as invalid (0xFF)
            Array.Fill(DecodeMap, (byte)0xFF);

            // Map valid alphabet characters to their 5-bit values
            for (byte i = 0; i < Alphabet.Length; i++)
                DecodeMap[Alphabet[i]] = i;

            // Special value for padding character (0xFE)
            DecodeMap[PaddingChar] = 0xFE;
        }

        /// <summary>
        /// Gets the Extended Hex Base32 alphabet used for encoding.
        /// </summary>
        /// <returns>The Base32 alphabet string.</returns>
        public override string ToString() => Alphabet;

        /// <summary>
        /// Encodes a string using Extended Hex Base32 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The Base32 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Convert string to bytes using specified encoding
            byte[] bytes = GetBytes(input, encoding);
            return ConvertToBase32(bytes);
        }

        /// <summary>
        /// Decodes an Extended Hex Base32 encoded string.
        /// </summary>
        /// <param name="base32">The Base32 string to decode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The decoded original string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if base32 is null.</exception>
        /// <exception cref="ArgumentException">Thrown for invalid Base32 strings.</exception>
        public static string Decode(string base32, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (base32 == null) throw new ArgumentNullException(nameof(base32));

            // Convert Base32 to bytes then to string using specified encoding
            byte[] bytes = ConvertFromBase32(base32);
            return GetString(bytes, encoding);
        }

        /// <summary>
        /// Converts a byte array to Extended Hex Base32 string.
        /// </summary>
        /// <param name="input">The byte array to encode.</param>
        /// <returns>The Base32 encoded string.</returns>
        private static unsafe string ConvertToBase32(byte[] input)
        {
            if (input.Length == 0) return string.Empty;

            // Calculate output length (5 bits per character)
            int base32Length = (input.Length * 8 + 4) / 5;

            // Calculate padding needed to make length multiple of 8
            int paddingLength = (8 - base32Length % 8) % 8;

            // Allocate output buffer (including padding)
            char[] output = new char[base32Length + paddingLength];

            fixed (byte* inputPtr = input)
            fixed (char* outputPtr = output)
            {
                byte* currentInput = inputPtr;
                byte* endInput = inputPtr + input.Length;
                char* currentOutput = outputPtr;

                // Bit buffer for accumulating bits across byte boundaries
                ulong buffer = 0;
                int bitsInBuffer = 0;

                while (currentInput < endInput)
                {
                    // Accumulate 8 bits from current byte
                    buffer = (buffer << 8) | *currentInput++;
                    bitsInBuffer += 8;

                    // Extract 5-bit chunks while we have enough bits
                    while (bitsInBuffer >= 5)
                    {
                        bitsInBuffer -= 5;
                        byte value = (byte)((buffer >> bitsInBuffer) & 0x1F);
                        *currentOutput++ = Alphabet[value];
                    }
                }

                // Handle remaining bits (less than 5)
                if (bitsInBuffer > 0)
                {
                    buffer <<= (5 - bitsInBuffer);
                    byte value = (byte)(buffer & 0x1F);
                    *currentOutput++ = Alphabet[value];
                }

                // Add padding characters if needed
                while (currentOutput < outputPtr + output.Length)
                {
                    *currentOutput++ = PaddingChar;
                }
            }

            return new string(output);
        }

        /// <summary>
        /// Converts an Extended Hex Base32 string to a byte array.
        /// </summary>
        /// <param name="base32">The Base32 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown for invalid padding, invalid characters, or incorrect length.
        /// </exception>
        private static unsafe byte[] ConvertFromBase32(string base32)
        {
            if (base32.Length == 0) return Array.Empty<byte>();

            // Calculate valid length by ignoring padding characters at the end
            int validLength = base32.Length;
            while (validLength > 0 && base32[validLength - 1] == PaddingChar)
                validLength--;

            // Calculate expected output size and padding bits
            int totalBits = validLength * 5;
            int paddingBits = totalBits % 8 == 0 ? 0 : 8 - (totalBits % 8);
            int expectedBytes = (totalBits - paddingBits) / 8;

            byte[] output = new byte[expectedBytes];

            fixed (char* inputPtr = base32)
            fixed (byte* outputPtr = output)
            {
                char* currentInput = inputPtr;
                char* endInput = inputPtr + validLength;
                byte* currentOutput = outputPtr;
                byte* endOutput = outputPtr + expectedBytes;

                // Bit buffer for accumulating bits across character boundaries
                ulong buffer = 0;
                int bitsInBuffer = 0;

                while (currentInput < endInput)
                {
                    char c = *currentInput++;
                    byte value = DecodeMap[c];

                    // Validate character
                    if (value >= 0x20)
                    {
                        if (value == 0xFE)
                            throw new ArgumentException($"Invalid padding position: {c}");
                        throw new ArgumentException($"Invalid Base32 character: {c}");
                    }

                    // Accumulate 5 bits from current character
                    buffer = (buffer << 5) | value;
                    bitsInBuffer += 5;

                    // Extract 8-bit bytes while we have enough bits
                    while (bitsInBuffer >= 8 && currentOutput < endOutput)
                    {
                        bitsInBuffer -= 8;
                        *currentOutput++ = (byte)((buffer >> bitsInBuffer) & 0xFF);
                    }
                }

                // Validate padding bits
                if (bitsInBuffer != paddingBits)
                    throw new ArgumentException("Invalid padding");

                if (paddingBits > 0)
                {
                    ulong mask = (1UL << paddingBits) - 1;
                    if ((buffer & mask) != 0)
                        throw new ArgumentException("Invalid padding");
                }

                if (currentOutput != endOutput)
                    throw new ArgumentException("Invalid padding");
            }

            return output;
        }

        /// <summary>
        /// Gets the bytes of a string using the specified encoding.
        /// </summary>
        private static byte[] GetBytes(string input, StringEncoding encoding) =>
            GetEncoding(encoding).GetBytes(input);

        /// <summary>
        /// Gets a string from bytes using the specified encoding.
        /// </summary>
        private static string GetString(byte[] bytes, StringEncoding encoding) =>
            GetEncoding(encoding).GetString(bytes);

        /// <summary>
        /// Gets the Encoding instance for the specified encoding type.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown for unknown encoding types.
        /// </exception>
        private static Encoding GetEncoding(StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8,
                StringEncoding.UTF16LE => Encoding.Unicode,
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode,
                StringEncoding.UTF32 => Encoding.UTF32,
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1,
#endif
                StringEncoding.UTF7 => Encoding.UTF7,
                StringEncoding.ASCII => Encoding.ASCII,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(encoding),
                    encoding,
                    "Unknown encoding type specified")
            };
        }
    }
}
#endif
