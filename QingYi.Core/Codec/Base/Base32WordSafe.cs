#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
using System.Text;
using System;

#pragma warning disable SYSLIB0001, CS0618

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Implements Base32 encoding/decoding using a Word-safe alphabet.
    /// This variant is designed to avoid visually similar characters (like 1/l/I, 0/O)
    /// to prevent confusion in human-readable strings.
    /// </summary>
    public class Base32WordSafe
    {
        // Word-safe alphabet (excludes visually similar characters)
        private const string Alphabet = "23456789CFGHJMPQRVWXcfghjmpqrvwx";

        // Lookup table for decoding characters (maps ASCII values to 5-bit values)
        private static readonly byte[] LookupTable = new byte[256];

        // Maximum padding attempts for decoding (per Base32 spec)
        private const int MaxPaddingAttempts = 6;

        /// <summary>
        /// Static constructor initializes the decoding lookup table.
        /// </summary>
        static Base32WordSafe()
        {
            // Initialize all entries as invalid (0xFF)
            Array.Fill(LookupTable, (byte)0xFF);

            // Map valid alphabet characters to their 5-bit values
            for (var i = 0; i < Alphabet.Length; i++)
                LookupTable[Alphabet[i]] = (byte)i;
        }

        /// <summary>
        /// Gets the character set used for Word-safe Base32 encoding.
        /// </summary>
        /// <returns>The Base32 alphabet string with visually distinct characters.</returns>
        public override string ToString() => Alphabet;

        /// <summary>
        /// Encodes a string using Word-safe Base32 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The Base32 encoded string without padding.</returns>
        /// <exception cref="ArgumentNullException">Thrown if input is null.</exception>
        public static string Encode(string input, StringEncoding encoding)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            // Convert string to bytes using specified encoding
            var bytes = GetEncoding(encoding).GetBytes(input);
            return ConvertToBase32(bytes);
        }

        /// <summary>
        /// Decodes a Word-safe Base32 encoded string.
        /// </summary>
        /// <param name="base32Input">The Base32 string to decode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The decoded original string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if base32Input is null.</exception>
        /// <exception cref="ArgumentException">Thrown for invalid Base32 strings.</exception>
        public static string Decode(string base32Input, StringEncoding encoding)
        {
            if (base32Input == null) throw new ArgumentNullException(nameof(base32Input));

            // Convert Base32 to bytes then to string using specified encoding
            var bytes = ConvertFromBase32(base32Input);
            return GetEncoding(encoding).GetString(bytes);
        }

        /// <summary>
        /// Gets the appropriate text encoding based on the specified encoding type.
        /// </summary>
        private static Encoding GetEncoding(StringEncoding encoding) => encoding switch
        {
            StringEncoding.UTF8 => Encoding.UTF8,
            StringEncoding.UTF16LE => new UnicodeEncoding(false, false),
            StringEncoding.UTF16BE => new UnicodeEncoding(true, false),
            StringEncoding.UTF32 => new UTF32Encoding(),
            StringEncoding.UTF7 => Encoding.UTF7,
            StringEncoding.ASCII => Encoding.ASCII,
#if NET6_0_OR_GREATER
            StringEncoding.Latin1 => Encoding.Latin1,
#endif
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };

        /// <summary>
        /// Converts a byte array to Word-safe Base32 string without padding.
        /// </summary>
        private static unsafe string ConvertToBase32(byte[] input)
        {
            if (input.Length == 0) return string.Empty;

            // Calculate output length: ceil(inputBits / 5)
            var outputLength = (input.Length * 8 + 4) / 5;
            var output = new char[outputLength];

            fixed (byte* inputPtr = input)
            fixed (char* outputPtr = output)
            {
                var buffer = 0;        // Accumulator for bits
                var bitsLeft = 0;     // Number of bits remaining in buffer
                var outputIndex = 0;   // Current output position

                for (var i = 0; i < input.Length; i++)
                {
                    // Add 8 bits from current byte to buffer
                    buffer = (buffer << 8) | inputPtr[i];
                    bitsLeft += 8;

                    // Extract 5-bit chunks while we have enough bits
                    while (bitsLeft >= 5)
                    {
                        var value = (buffer >> (bitsLeft - 5)) & 0x1F;
                        outputPtr[outputIndex++] = Alphabet[value];
                        bitsLeft -= 5;
                    }
                }

                // Handle remaining bits (less than 5)
                if (bitsLeft > 0)
                {
                    var value = (buffer << (5 - bitsLeft)) & 0x1F;
                    outputPtr[outputIndex] = Alphabet[value];
                }
            }

            return new string(output);
        }

        /// <summary>
        /// Converts a Word-safe Base32 string to a byte array.
        /// Handles multiple padding possibilities for compatibility.
        /// </summary>
        private static unsafe byte[] ConvertFromBase32(string input)
        {
            if (input.Length == 0) return Array.Empty<byte>();

            // Generate all possible padding combinations (max 6 per Base32 spec)
            var candidates = new string[MaxPaddingAttempts + 1];
            for (int p = 0; p <= MaxPaddingAttempts; p++)
            {
                candidates[p] = input.PadRight(input.Length + p, '=');
                if (candidates[p].Length % 8 != 0)
                    candidates[p] = null; // Only keep valid lengths
            }

            // Try each candidate until one succeeds
            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;

                try
                {
                    return ProcessPaddedString(candidate);
                }
                catch (ArgumentException)
                {
                    // Try next candidate
                }
            }

            throw new ArgumentException("Invalid Base32 string");
        }

        /// <summary>
        /// Processes a padded Base32 string to bytes.
        /// </summary>
        private static unsafe byte[] ProcessPaddedString(string input)
        {
            // Calculate effective length by ignoring padding
            var effectiveLength = input.Length;
            while (effectiveLength > 0 && input[effectiveLength - 1] == '=')
                effectiveLength--;

            // Calculate output length: floor(effectiveBits / 8)
            var outputLength = effectiveLength * 5 / 8;
            var output = new byte[outputLength];

            fixed (char* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                var buffer = 0;        // Accumulator for bits
                var bitsLeft = 0;      // Number of bits remaining in buffer
                var outputIndex = 0;    // Current output position

                for (var i = 0; i < effectiveLength; i++)
                {
                    char c = inputPtr[i];

                    // Validate character is in Word-safe alphabet
                    if (c >= 256 || LookupTable[c] == 0xFF)
                        throw new ArgumentException($"Invalid character: {c}");

                    // Add 5 bits from current character to buffer
                    buffer = (buffer << 5) | LookupTable[c];
                    bitsLeft += 5;

                    // Extract 8-bit bytes while we have enough bits
                    if (bitsLeft >= 8)
                    {
                        bitsLeft -= 8;
                        outputPtr[outputIndex++] = (byte)((buffer >> bitsLeft) & 0xFF);
                    }
                }

                // Validate remaining bits (must be less than 5)
                if (bitsLeft >= 5)
                    throw new ArgumentException("Invalid padding");
            }

            return output;
        }
    }
}
#endif
