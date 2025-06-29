using System;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable SYSLIB0001, CS0618

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides encoding and decoding functionality for Crockford's Base32 variant.
    /// Crockford's Base32 is designed for human readability and error prevention,
    /// with features like case insensitivity and ambiguous character handling.
    /// </summary>
    public class Base32Crockford
    {
        // The Crockford Base32 alphabet (excluding I, L, O for ambiguity reduction)
        private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        // Lookup table for character decoding (256 entries for all possible byte values)
        private static readonly byte[] CharMap = new byte[256];

        /// <summary>
        /// Static constructor initializes the character mapping table.
        /// Sets up valid characters and handles ambiguous characters (I, L, O).
        /// </summary>
        static Base32Crockford()
        {
            // Initialize all characters as invalid (0xFF)
            for (int i = 0; i < 256; i++) CharMap[i] = 0xFF;

            // Map valid alphabet characters (both upper and lower case)
            for (byte i = 0; i < Alphabet.Length; i++)
            {
                char c = Alphabet[i];
                CharMap[c] = i;
                CharMap[char.ToLowerInvariant(c)] = i;
            }

            // Handle ambiguous characters:
            // Map 'O' and 'o' to 0 (same as '0')
            CharMap['O'] = 0; CharMap['o'] = 0;
            // Map 'I', 'i', 'L', and 'l' to 1 (same as '1')
            CharMap['I'] = 1; CharMap['i'] = 1;
            CharMap['L'] = 1; CharMap['l'] = 1;
        }

        /// <summary>
        /// Gets the character set used for Crockford's Base32 encoding.
        /// </summary>
        /// <returns>The Base32 alphabet string.</returns>
        public override string ToString() => Alphabet;

        /// <summary>
        /// Encodes a string using Crockford's Base32 encoding.
        /// </summary>
        /// <param name="source">The string to encode.</param>
        /// <param name="encodingType">The text encoding to use (default: UTF8).</param>
        /// <returns>The Base32 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
        public static string Encode(string source, StringEncoding encodingType = StringEncoding.UTF8)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source.Length == 0) return string.Empty;

            // Convert string to bytes using specified encoding
            byte[] bytes = GetEncoding(encodingType).GetBytes(source);
            return EncodeBytes(bytes);
        }

        /// <summary>
        /// Decodes a Crockford's Base32 encoded string.
        /// </summary>
        /// <param name="encoded">The Base32 string to decode.</param>
        /// <param name="encodingType">The text encoding to use (default: UTF8).</param>
        /// <returns>The decoded original string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if encoded is null.</exception>
        /// <exception cref="ArgumentException">Thrown if encoded contains invalid characters.</exception>
        public static string Decode(string encoded, StringEncoding encodingType = StringEncoding.UTF8)
        {
            if (encoded == null) throw new ArgumentNullException(nameof(encoded));
            if (encoded.Length == 0) return string.Empty;

            // Decode Base32 to bytes then convert to string
            byte[] bytes = DecodeBytes(encoded);
            return GetEncoding(encodingType).GetString(bytes);
        }

        /// <summary>
        /// Encodes a byte array to Crockford's Base32 string.
        /// </summary>
        /// <param name="input">The byte array to encode.</param>
        /// <returns>The Base32 encoded string.</returns>
        private static unsafe string EncodeBytes(byte[] input)
        {
            int inputLength = input.Length;
            // Calculate output length: ceil(inputBits / 5)
            int outputLength = (inputLength * 8 + 4) / 5;
            char[] output = new char[outputLength];
            int outputIndex = 0;

            // Bit buffer for accumulating bits across byte boundaries
            ulong buffer = 0;
            int bufferBits = 0;

            // Use fixed pointers for maximum performance
            fixed (byte* pInput = input)
            fixed (char* pOutput = output)
            {
                byte* pIn = pInput;
                byte* pEnd = pInput + inputLength;
                char* pOut = pOutput;

                while (pIn < pEnd)
                {
                    // Accumulate 8 bits from current byte
                    buffer = buffer << 8 | *pIn++;
                    bufferBits += 8;

                    // Extract 5-bit chunks while we have enough bits
                    while (bufferBits >= 5)
                    {
                        bufferBits -= 5;
                        byte value = (byte)(buffer >> bufferBits & 0x1F);
                        *pOut++ = Alphabet[value];
                        outputIndex++;
                    }
                }

                // Handle remaining bits (less than 5)
                if (bufferBits > 0)
                {
                    byte value = (byte)(buffer << (5 - bufferBits) & 0x1F);
                    *pOut++ = Alphabet[value];
                    outputIndex++;
                }
            }

            return new string(output, 0, outputIndex);
        }

        /// <summary>
        /// Decodes a Crockford's Base32 string to a byte array.
        /// </summary>
        /// <param name="encoded">The Base32 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        /// <exception cref="ArgumentException">Thrown if invalid characters are found.</exception>
        private static unsafe byte[] DecodeBytes(string encoded)
        {
            // First pass: count valid characters (skip hyphens and whitespace)
            int validCharCount = 0;
            fixed (char* pEncoded = encoded)
            {
                char* p = pEncoded;
                char* pEnd = p + encoded.Length;
                while (p < pEnd)
                {
                    char c = *p++;
                    if (IsIgnoredChar(c)) continue;
                    if (CharMap[c] == 0xFF) throw new ArgumentException("Invalid character: " + c);
                    validCharCount++;
                }
            }

            if (validCharCount == 0) return Array.Empty<byte>();
            // Calculate output length: floor(validCharCount * 5 / 8)
            int outputLength = validCharCount * 5 / 8;
            byte[] output = new byte[outputLength];
            int outputIndex = 0;

            // Bit buffer for accumulating bits across character boundaries
            ulong buffer = 0;
            int bufferBits = 0;

            // Second pass: actual decoding
            fixed (char* pEncoded = encoded)
            fixed (byte* pOutput = output)
            {
                char* p = pEncoded;
                char* pEnd = p + encoded.Length;
                byte* pOut = pOutput;

                while (p < pEnd)
                {
                    char c = *p++;
                    if (IsIgnoredChar(c)) continue;

                    // Accumulate 5 bits from current character
                    buffer = buffer << 5 | CharMap[c];
                    bufferBits += 5;

                    // Extract 8-bit bytes while we have enough bits
                    while (bufferBits >= 8)
                    {
                        bufferBits -= 8;
                        *pOut++ = (byte)(buffer >> bufferBits & 0xFF);
                        outputIndex++;
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Determines if a character should be ignored during decoding.
        /// Crockford's Base32 ignores hyphens and whitespace.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns>True if the character should be ignored.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIgnoredChar(char c) => c == '-' || char.IsWhiteSpace(c);

        /// <summary>
        /// Gets the appropriate text encoding based on the specified encoding type.
        /// </summary>
        /// <param name="encodingType">The encoding type identifier.</param>
        /// <returns>The corresponding Encoding instance.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown encoding types.</exception>
        private static Encoding GetEncoding(StringEncoding encodingType)
        {
            switch (encodingType)
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
                    return Encoding.GetEncoding(28591); // Latin1 (ISO-8859-1)
#endif
                case StringEncoding.ASCII:
                    return Encoding.ASCII;
                case StringEncoding.UTF7:
                    return Encoding.UTF7;
                default:
                    throw new ArgumentOutOfRangeException(nameof(encodingType), "Unknown encoding type specified");
            }
        }
    }
}
