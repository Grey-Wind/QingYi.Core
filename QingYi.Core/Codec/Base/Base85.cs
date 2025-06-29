#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Buffers;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Enum representing different Base85 encoding variants
    /// </summary>
    public enum Base85Variant
    {
        /// <summary>
        /// Standard Ascii85 encoding (RFC 1924)
        /// </summary>
        Ascii85,

        /// <summary>
        /// ZeroMQ's Z85 variant (RFC 32)
        /// </summary>
        Z85,

        /// <summary>
        /// Git binary patch variant
        /// </summary>
        Git,

        /// <summary>
        /// IPv6 variant (RFC 1924)
        /// </summary>
        IPv6
    }

    /// <summary>
    /// Provides Base85 encoding and decoding functionality for various variants
    /// </summary>
    public static class Base85
    {
        // Variant character mapping tables
        private static readonly Dictionary<Base85Variant, char[]> VariantAlphabets = new()
        {
            [Base85Variant.Ascii85] = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstu".ToCharArray(),
            [Base85Variant.Z85] = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.-:+=^!/*?&<>()[]{}@%$#".ToCharArray(),
            [Base85Variant.Git] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&()*+-;<=>?@^_`{|}~".ToCharArray(),
            [Base85Variant.IPv6] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz!#$%&()*+-;<=>?@^_`{|}~".ToCharArray()
        };

        // Variant-specific special character handling
        private static readonly Dictionary<Base85Variant, (char Zero, char Padding, string Prefix, string Suffix)> VariantOptions = new()
        {
            [Base85Variant.Ascii85] = ('z', '~', "<~", "~>"),
            [Base85Variant.Z85] = ('\0', '\0', "", ""),
            [Base85Variant.Git] = ('\0', '\0', "", ""),
            [Base85Variant.IPv6] = ('\0', '\0', "", "")
        };

        #region Encoding Methods
        /// <summary>
        /// Encodes binary data to Base85 string
        /// </summary>
        /// <param name="data">Binary data to encode</param>
        /// <param name="variant">Base85 variant to use (default: Ascii85)</param>
        /// <returns>Base85 encoded string</returns>
        /// <exception cref="ArgumentNullException">Thrown when input data is null</exception>
        /// <exception cref="ArgumentException">Thrown for Z85 variant when input length isn't multiple of 4</exception>
        public static unsafe string Encode(byte[] data, Base85Variant variant = Base85Variant.Ascii85)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (variant == Base85Variant.Z85 && data.Length % 4 != 0)
                throw new ArgumentException("Z85 requires input length to be multiple of 4");

            var options = VariantOptions[variant];
            var alphabet = VariantAlphabets[variant];
            var prefix = options.Prefix;
            var suffix = options.Suffix;
            var useZero = options.Zero != '\0';

            // Calculate output length
            int blockCount = data.Length / 4;
            int remainingBytes = data.Length % 4;
            int outputLength = prefix.Length +
                              blockCount * 5 +
                              (remainingBytes > 0 ? 5 : 0) +
                              suffix.Length;

            // Rent character array from array pool
            char[] outputBuffer = ArrayPool<char>.Shared.Rent(outputLength);
            int charPos = 0;

            try
            {
                // Add prefix
                for (int i = 0; i < prefix.Length; i++)
                {
                    outputBuffer[charPos++] = prefix[i];
                }

                fixed (byte* pData = data)
                fixed (char* pOutput = outputBuffer)
                {
                    uint* pUint = (uint*)pData;
                    char* currentOut = pOutput + prefix.Length;

                    for (int i = 0; i < blockCount; i++)
                    {
                        if (useZero && pUint[i] == 0)
                        {
                            *currentOut++ = options.Zero;
                            charPos++;
                            continue;
                        }

                        // Convert to big-endian and encode
                        uint block = BitConverter.IsLittleEndian ? ReverseBytes(pUint[i]) : pUint[i];
                        int count = EncodeBlock(block, currentOut, variant);
                        currentOut += count;
                        charPos += count;
                    }

                    if (remainingBytes > 0)
                    {
                        uint lastBlock = 0;
                        byte* pLast = (byte*)pData + blockCount * 4;
                        // Build remaining block in big-endian order
                        for (int i = 0; i < remainingBytes; i++)
                        {
                            lastBlock = (lastBlock << 8) | pLast[i];
                        }
                        lastBlock <<= (32 - remainingBytes * 8);

                        // Convert to big-endian and encode
                        if (BitConverter.IsLittleEndian)
                        {
                            lastBlock = ReverseBytes(lastBlock);
                        }
                        int count = EncodeBlock(lastBlock, currentOut, variant);
                        charPos += count;
                    }
                }

                // Add suffix
                for (int i = 0; i < suffix.Length; i++)
                {
                    outputBuffer[charPos++] = suffix[i];
                }

                return new string(outputBuffer, 0, charPos);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(outputBuffer);
            }
        }

        /// <summary>
        /// Encodes text string to Base85 string
        /// </summary>
        /// <param name="text">Text to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF8)</param>
        /// <param name="variant">Base85 variant to use (default: Ascii85)</param>
        /// <returns>Base85 encoded string</returns>
        public static string Encode(string text, StringEncoding encoding = StringEncoding.UTF8, Base85Variant variant = Base85Variant.Ascii85)
        {
            byte[] data = GetBytes(text, encoding);
            return Encode(data, variant);
        }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Decodes Base85 string to binary data
        /// </summary>
        /// <param name="base85">Base85 encoded string</param>
        /// <param name="variant">Base85 variant to use (default: Ascii85)</param>
        /// <returns>Decoded binary data</returns>
        /// <exception cref="ArgumentNullException">Thrown when input string is null</exception>
        /// <exception cref="FormatException">Thrown for invalid Base85 strings</exception>
        public static byte[] Decode(string base85, Base85Variant variant = Base85Variant.Ascii85)
        {
            if (base85 == null) throw new ArgumentNullException(nameof(base85));

            var options = VariantOptions[variant];
            var alphabet = VariantAlphabets[variant];
            var prefix = options.Prefix;
            var suffix = options.Suffix;

            // Handle prefix/suffix
            if (base85.StartsWith(prefix)) base85 = base85[prefix.Length..];
            if (base85.EndsWith(suffix)) base85 = base85[..^suffix.Length];

            // Clean invalid characters
            base85 = Regex.Replace(base85, @"\s+", "");
            if (variant == Base85Variant.Z85 && base85.Length % 5 != 0)
                throw new FormatException("Z85 requires input length to be multiple of 5");

            // Check invalid length (for non-Z85 variants)
            if (variant != Base85Variant.Z85 && base85.Length % 5 == 1)
                throw new FormatException("Invalid Base85 string length: last block must not be 1 character");

            int blockCount = base85.Length / 5;
            int remainingChars = base85.Length % 5;
            int outputLength = blockCount * 4 + (remainingChars > 0 ? remainingChars - 1 : 0);
            var result = new byte[outputLength];

            unsafe
            {
                fixed (char* pBase85 = base85)
                fixed (byte* pResult = result)
                {
                    uint* pUint = (uint*)pResult;
                    int base85Pos = 0;
                    int outputPos = 0;
                    var zeroChar = options.Zero;
                    var alphabetMap = BuildAlphabetMap(alphabet);

                    for (int i = 0; i < blockCount; i++)
                    {
                        if (zeroChar != '\0' && pBase85[base85Pos] == zeroChar)
                        {
                            *pUint++ = 0;
                            base85Pos++;
                            outputPos += 4;
                            continue;
                        }

                        *pUint++ = DecodeBlock(pBase85 + base85Pos, 5, alphabetMap, variant);
                        base85Pos += 5;
                        outputPos += 4;
                    }

                    if (remainingChars > 0)
                    {
                        uint lastBlock = DecodeBlock(pBase85 + base85Pos, remainingChars, alphabetMap, variant);
                        // Extract bytes from big-endian order
                        for (int i = 0; i < remainingChars - 1; i++)
                        {
                            result[outputPos++] = (byte)(lastBlock >> (24 - i * 8));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Decodes Base85 string to text string
        /// </summary>
        /// <param name="base85">Base85 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF8)</param>
        /// <param name="variant">Base85 variant to use (default: Ascii85)</param>
        /// <returns>Decoded text string</returns>
        public static string DecodeToString(string base85, StringEncoding encoding = StringEncoding.UTF8, Base85Variant variant = Base85Variant.Ascii85)
        {
            byte[] data = Decode(base85, variant);
            return GetString(data, encoding);
        }
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Encodes a 32-bit block to Base85 characters
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int EncodeBlock(uint value, char* output, Base85Variant variant)
        {
            var alphabet = VariantAlphabets[variant];
            char* current = output + 4;  // Write from end to beginning

            // Manual division implementation
            for (int i = 0; i < 5; i++)
            {
                uint remainder = value % 85;
                value /= 85;
                *current-- = alphabet[remainder];
            }

            return 5;
        }

        /// <summary>
        /// Decodes a Base85 character block to 32-bit value
        /// </summary>
        private static unsafe uint DecodeBlock(char* input, int length, byte[] alphabetMap, Base85Variant variant)
        {
            uint result = 0;
            for (int i = 0; i < length; i++)
            {
                char c = input[i];
                if (c < 0 || c >= alphabetMap.Length || alphabetMap[c] == 0xFF)
                    throw new FormatException($"Invalid character '{c}' in Base85 string");

                result = result * 85 + alphabetMap[c];
            }

            // Pad remaining portion
            for (int i = length; i < 5; i++)
            {
                result = result * 85 + 84;
            }

            // Convert to system endianness
            return BitConverter.IsLittleEndian ? ReverseBytes(result) : result;
        }

        /// <summary>
        /// Builds a fast lookup table for character mapping
        /// </summary>
        private static byte[] BuildAlphabetMap(char[] alphabet)
        {
            var map = new byte[256];
            Array.Fill(map, (byte)0xFF);

            for (byte i = 0; i < alphabet.Length; i++)
            {
                char c = alphabet[i];
                map[c] = i;
            }
            return map;
        }

        /// <summary>
        /// Reverses byte order of a 32-bit value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReverseBytes(uint value)
        {
            return (value >> 24) |
                   ((value >> 8) & 0x0000FF00) |
                   ((value << 8) & 0x00FF0000) |
                   (value << 24);
        }

        /// <summary>
        /// Converts text to bytes using specified encoding
        /// </summary>
        private static byte[] GetBytes(string text, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.ASCII => Encoding.ASCII.GetBytes(text),
                StringEncoding.UTF16LE => Encoding.Unicode.GetBytes(text),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetBytes(text),
                StringEncoding.UTF32 => Encoding.UTF32.GetBytes(text),
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => Encoding.UTF7.GetBytes(text),
#pragma warning restore SYSLIB0001, CS0618
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetBytes(text),
#endif
                _ => Encoding.UTF8.GetBytes(text) // UTF8 is default
            };
        }

        /// <summary>
        /// Converts bytes to text using specified encoding
        /// </summary>
        private static string GetString(byte[] data, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.ASCII => Encoding.ASCII.GetString(data),
                StringEncoding.UTF16LE => Encoding.Unicode.GetString(data),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetString(data),
                StringEncoding.UTF32 => Encoding.UTF32.GetString(data),
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => Encoding.UTF7.GetString(data),
#pragma warning restore SYSLIB0001, CS0618
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetString(data),
#endif
                _ => Encoding.UTF8.GetString(data) // UTF8 is default
            };
        }
        #endregion

        #region Special Variant Methods
        /// <summary>
        /// Encodes binary data using Z85 variant
        /// </summary>
        public static string EncodeZ85(byte[] data) => Encode(data, Base85Variant.Z85);

        /// <summary>
        /// Decodes Z85 encoded string to binary data
        /// </summary>
        public static byte[] DecodeZ85(string base85) => Decode(base85, Base85Variant.Z85);

        /// <summary>
        /// Encodes binary data using Git variant
        /// </summary>
        public static string EncodeGit(byte[] data) => Encode(data, Base85Variant.Git);

        /// <summary>
        /// Decodes Git encoded string to binary data
        /// </summary>
        public static byte[] DecodeGit(string base85) => Decode(base85, Base85Variant.Git);
        #endregion
    }
}
#endif
