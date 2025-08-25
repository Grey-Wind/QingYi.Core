using System;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base92 encoding and decoding functionality
    /// </summary>
    public class Base92
    {
        /// <summary>
        /// Base92 character set (94 printable ASCII characters)
        /// </summary>
        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$%&()*+,./:;<=>?@[]^_`{|}~'";

        /// <summary>
        /// Character mapping table (ASCII value to Base92 index)
        /// </summary>
        private static readonly byte[] CHAR_MAP = new byte[256];

        /// <summary>
        /// Static constructor initializes the character mapping table
        /// </summary>
        static Base92()
        {
            // Initialize character mapping table (0xFF indicates invalid character)
            for (int i = 0; i < CHAR_MAP.Length; i++)
            {
                CHAR_MAP[i] = 0xFF;
            }

            // Populate valid character mappings
            for (byte idx = 0; idx < ALPHABET.Length; idx++)
            {
                char c = ALPHABET[idx];
                CHAR_MAP[c] = idx;
            }
        }

        /// <summary>
        /// Returns the Base92 character set
        /// </summary>
        /// <returns>String containing all Base92 characters</returns>
        public override string ToString() => ALPHABET;

        #region Encoding Methods

        /// <summary>
        /// Encodes a byte array to Base92 string
        /// </summary>
        /// <param name="data">Byte array to encode</param>
        /// <returns>Base92 encoded string</returns>
        public static string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            unsafe
            {
                int inLen = data.Length;
                // Maximum output length estimate: (input bytes * 8 / 6.5) + 2
                int maxOutLen = (int)Math.Ceiling(inLen * 8 / 6.5) + 2;
                char* output = stackalloc char[maxOutLen];
                char* outPtr = output;

                fixed (byte* inPtr = data)
                {
                    byte* inEnd = inPtr + inLen;
                    byte* inP = inPtr;

                    uint bitBuffer = 0;  // Accumulator for bits
                    int bitCount = 0;    // Number of bits currently in buffer

                    while (inP < inEnd)
                    {
                        // Add next byte to the buffer
                        bitBuffer = (bitBuffer << 8) | *inP++;
                        bitCount += 8;

                        // Process when we have at least 13 bits
                        while (bitCount >= 13)
                        {
                            bitCount -= 13;
                            uint value = (bitBuffer >> bitCount) & 0x1FFF; // Extract 13 bits

                            // Split into two Base92 characters
                            uint idx1 = value / 92;
                            uint idx2 = value % 92;
                            *outPtr++ = ALPHABET[(int)idx1];
                            *outPtr++ = ALPHABET[(int)idx2];
                        }
                    }

                    // Handle remaining bits
                    if (bitCount > 0)
                    {
                        // Shift remaining bits to high 13 bits
                        bitBuffer <<= (13 - bitCount);
                        uint value = bitBuffer & 0x1FFF;

                        // Output 1 or 2 characters depending on value
                        if (bitCount > 7 || value >= 92)
                        {
                            uint idx1 = value / 92;
                            uint idx2 = value % 92;
                            *outPtr++ = ALPHABET[(int)idx1];
                            *outPtr++ = ALPHABET[(int)idx2];
                        }
                        else
                        {
                            *outPtr++ = ALPHABET[(int)value];
                        }
                    }
                }

                return new string(output, 0, (int)(outPtr - output));
            }
        }

        /// <summary>
        /// Encodes a string to Base92 string using specified encoding
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base92 encoded string</returns>
        public static string Encode(string text, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            Encoding enc = GetEncoding(encoding);
            byte[] data = enc.GetBytes(text);
            return Encode(data);
        }

        #endregion

        #region Decoding Methods

        /// <summary>
        /// Decodes a Base92 string to byte array
        /// </summary>
        /// <param name="base92">Base92 encoded string</param>
        /// <returns>Decoded byte array</returns>
        /// <exception cref="FormatException">Thrown when invalid Base92 characters are encountered</exception>
        public static byte[] Decode(string base92)
        {
            if (string.IsNullOrEmpty(base92))
#if NET45 || NET451 || NET452
                return new byte[] { };
#else
                return Array.Empty<byte>();
#endif

            unsafe
            {
                int inLen = base92.Length;
                // Maximum output buffer size: (input chars * 13 + 7) / 8
                int maxOutLen = (inLen * 13 + 7) / 8;
                byte[] outputArray = new byte[maxOutLen];
                int actualOutLen = 0;

                fixed (char* inPtr = base92)
                fixed (byte* outPtr = outputArray)
                {
                    char* inEnd = inPtr + inLen;
                    char* inP = inPtr;
                    byte* outP = outPtr;

                    uint bitBuffer = 0;  // Accumulator for bits
                    int bitCount = 0;     // Number of bits currently in buffer
                    bool lastBlockIsSingle = false;

                    // Process complete character pairs
                    while (inP < inEnd)
                    {
                        // Get first character value
                        char c1 = *inP++;
                        byte v1 = CHAR_MAP[c1];
                        if (v1 == 0xFF)
                            throw new FormatException($"Invalid Base92 character: '{c1}' (0x{(byte)c1:X2})");

                        // Check if there's a second character
                        if (inP >= inEnd)
                        {
                            // Last character is single
                            lastBlockIsSingle = true;
                            bitBuffer = (bitBuffer << 7) | v1;
                            bitCount += 7;
                            break;
                        }

                        // Get second character value
                        char c2 = *inP++;
                        byte v2 = CHAR_MAP[c2];
                        if (v2 == 0xFF)
                            throw new FormatException($"Invalid Base92 character: '{c2}' (0x{(byte)c2:X2})");

                        // Combine two characters into 13-bit value
                        uint value = (uint)(v1 * 92 + v2);
                        bitBuffer = (bitBuffer << 13) | value;
                        bitCount += 13;

                        // Extract complete bytes
                        while (bitCount >= 8)
                        {
                            bitCount -= 8;
                            *outP++ = (byte)(bitBuffer >> bitCount);
                            actualOutLen++;
                        }
                    }

                    // Handle last character block (if single)
                    if (lastBlockIsSingle)
                    {
                        // Extract remaining bytes (max 1)
                        if (bitCount >= 8)
                        {
                            bitCount -= 8;
                            *outP++ = (byte)(bitBuffer >> bitCount);
                            actualOutLen++;
                        }

                        // Check remaining bits
                        if (bitCount > 0)
                        {
                            uint mask = (1u << bitCount) - 1;
                            if ((bitBuffer & mask) != 0)
                            {
                                throw new FormatException(
                                    $"Invalid padding: {bitCount} extra bits with non-zero value (0x{bitBuffer & mask:X})");
                            }
                        }
                    }
                }

                // Return byte array with actual length
                if (actualOutLen == outputArray.Length)
                    return outputArray;

                byte[] result = new byte[actualOutLen];
                Buffer.BlockCopy(outputArray, 0, result, 0, actualOutLen);
                return result;
            }
        }

        /// <summary>
        /// Decodes a Base92 string to string using specified encoding
        /// </summary>
        /// <param name="base92">Base92 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeToString(string base92, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] data = Decode(base92);
            Encoding enc = GetEncoding(encoding);
            return enc.GetString(data);
        }

        #endregion

        #region Encoding Helper Methods

        /// <summary>
        /// Gets the Encoding object corresponding to the specified encoding enum
        /// </summary>
        /// <param name="encoding">Encoding enum value</param>
        /// <returns>Encoding object</returns>
        /// <exception cref="ArgumentException">Thrown for unsupported encoding values</exception>
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
                case StringEncoding.ASCII:
                    return Encoding.ASCII;
                case StringEncoding.UTF32:
                    return Encoding.UTF32;
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1:
                    return Encoding.Latin1;
#endif
#pragma warning disable SYSLIB0001, CS0618
                case StringEncoding.UTF7:
                    return Encoding.UTF7;
#pragma warning restore SYSLIB0001, CS0618
                default:
                    throw new ArgumentException("Unsupported encoding", nameof(encoding));
            }
        }

        #endregion
    }
}
