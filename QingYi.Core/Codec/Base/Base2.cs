using System.Text;
using System;

#pragma warning disable SYSLIB0001, CS0618, CA1510

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base2 (binary) encoding and decoding functionality.
    /// </summary>
    /// <remarks>
    /// Base2 represents binary data using only '0' and '1' characters.
    /// This implementation supports both little-endian and big-endian byte ordering.
    /// </remarks>
    public static class Base2
    {
        private static readonly char[] PrecomputedBits = new char[256 * 8];
        private static readonly byte[] BitMasks = new byte[8] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

        static Base2()
        {
            // Initialize precomputed bits
            for (int i = 0; i < 256; i++)
            {
                byte b = (byte)i;
                for (int j = 0; j < 8; j++)
                {
                    PrecomputedBits[i * 8 + j] = (b & 0x80 >> j) != 0 ? '1' : '0';
                }
            }
        }

        /// <summary>
        /// Encodes a string into Base2 format with specified encoding and endianness.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="isBigEndian">Whether to use big-endian byte order (default: false).</param>
        /// <returns>The Base2 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public static string Encode(string input, StringEncoding encoding, bool isBigEndian = false)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            byte[] bytes = GetEncodedBytes(input, encoding, isBigEndian);
            return BytesToBase2(bytes);
        }

        /// <summary>
        /// Encodes a string into Base2 format using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="isBigEndian">Whether to use big-endian byte order (default: false).</param>
        /// <returns>The Base2 encoded string.</returns>
        public static string Encode(string input, bool isBigEndian = false) => Encode(input, StringEncoding.UTF8, isBigEndian);

        /// <summary>
        /// Decodes a Base2 string back to the original string with specified encoding and endianness.
        /// </summary>
        /// <param name="base2">The Base2 string to decode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="isBigEndian">Whether the data is in big-endian byte order (default: false).</param>
        /// <returns>The decoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public static string Decode(string base2, StringEncoding encoding, bool isBigEndian = false)
        {
            if (base2 == null) throw new ArgumentNullException(nameof(base2));

            byte[] bytes = Base2ToBytes(base2);
            return GetDecodedString(bytes, encoding, isBigEndian);
        }

        /// <summary>
        /// Decodes a Base2 string back to the original string using UTF-8 encoding.
        /// </summary>
        /// <param name="base2">The Base2 string to decode.</param>
        /// <param name="isBigEndian">Whether the data is in big-endian byte order (default: false).</param>
        /// <returns>The decoded string.</returns>
        public static string Decode(string base2, bool isBigEndian = false)
        {
            return Decode(base2, StringEncoding.UTF8, isBigEndian);
        }

        /// <summary>
        /// Converts a string to bytes using the specified encoding and endianness.
        /// </summary>
        private static byte[] GetEncodedBytes(string input, StringEncoding encoding, bool isBigEndian)
        {
            switch (encoding)
            {
                case StringEncoding.UTF8:
                    return Encoding.UTF8.GetBytes(input);
                case StringEncoding.UTF16LE:
                    return Encoding.Unicode.GetBytes(input);
                case StringEncoding.UTF16BE:
                    return Encoding.BigEndianUnicode.GetBytes(input);
#if !NET461 && !NET462
                case StringEncoding.UTF32:
                    return GetUtf32Bytes(input, isBigEndian);
#endif
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1:
                    return Encoding.GetEncoding(28591).GetBytes(input);
#endif
                case StringEncoding.ASCII:
                    return Encoding.ASCII.GetBytes(input);
                case StringEncoding.UTF7:
                    return Encoding.UTF7.GetBytes(input);
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }

        /// <summary>
        /// Converts bytes back to a string using the specified encoding and endianness.
        /// </summary>
        private static string GetDecodedString(byte[] bytes, StringEncoding encoding, bool isBigEndian)
        {
            if (bytes.Length == 0) return string.Empty;

            switch (encoding)
            {
                case StringEncoding.UTF8:
                    return Encoding.UTF8.GetString(bytes);
                case StringEncoding.UTF16LE:
                    return Encoding.Unicode.GetString(bytes);
                case StringEncoding.UTF16BE:
                    return Encoding.BigEndianUnicode.GetString(bytes);
#if !NET461 && !NET462
                case StringEncoding.UTF32:
                    return GetUtf32String(bytes, isBigEndian);
#endif
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1:
                    return Encoding.GetEncoding(28591).GetString(bytes);
#endif
                case StringEncoding.ASCII:
                    return Encoding.ASCII.GetString(bytes);
                case StringEncoding.UTF7:
                    return Encoding.UTF7.GetString(bytes);
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }

#if !NET461 && !NET462
        /// <summary>
        /// Gets UTF-32 bytes with proper endianness handling.
        /// </summary>
        private static byte[] GetUtf32Bytes(string input, bool isBigEndian)
        {
            byte[] bytes = Encoding.UTF32.GetBytes(input);
            if (isBigEndian != BitConverter.IsLittleEndian)
            {
                SwapEndianness(bytes, 4);
            }
            return bytes;
        }

        /// <summary>
        /// Gets a string from UTF-32 bytes with proper endianness handling.
        /// </summary>
        private static string GetUtf32String(byte[] bytes, bool isBigEndian)
        {
            if (isBigEndian != BitConverter.IsLittleEndian)
            {
                byte[] copy = new byte[bytes.Length];
                Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);
                SwapEndianness(copy, 4);
                return Encoding.UTF32.GetString(copy);
            }
            return Encoding.UTF32.GetString(bytes);
        }

        /// <summary>
        /// Swaps the endianness of the byte array.
        /// </summary>
        private static unsafe void SwapEndianness(byte[] bytes, int elementSize)
        {
            fixed (byte* ptr = bytes)
            {
                byte* end = ptr + bytes.Length;
                for (byte* p = ptr; p < end; p += elementSize)
                {
                    for (int i = 0; i < elementSize / 2; i++)
                    {
                        (p[elementSize - 1 - i], p[i]) = (p[i], p[elementSize - 1 - i]);
                    }
                }
            }
        }
#endif

        /// <summary>
        /// Converts a byte array to its Base2 representation.
        /// </summary>
        internal static unsafe string BytesToBase2(byte[] bytes)
        {
            int byteCount = bytes.Length;
            char[] buffer = new char[byteCount * 8];

            fixed (byte* srcPtr = bytes)
            fixed (char* bufferPtr = buffer)
            fixed (char* precomputedPtr = PrecomputedBits)
            {
                byte* src = srcPtr;
                char* dest = bufferPtr;

                for (int i = 0; i < byteCount; i++)
                {
                    char* precomputed = precomputedPtr + *src++ * 8;
                    Buffer.MemoryCopy(precomputed, dest, 16, 16);
                    dest += 8;
                }
            }

            return new string(buffer);
        }

        /// <summary>
        /// Converts a Base2 string back to a byte array.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when input length is invalid or contains non-binary characters.</exception>
        internal static unsafe byte[] Base2ToBytes(string base2)
        {
            if (base2.Length % 8 != 0)
                throw new ArgumentException("Invalid Base2 string length");

            int byteCount = base2.Length / 8;
            byte[] result = new byte[byteCount];

            fixed (char* inputPtr = base2)
            fixed (byte* resultPtr = result)
            {
                char* src = inputPtr;
                byte* dest = resultPtr;

                for (int i = 0; i < byteCount; i++)
                {
                    byte value = 0;
                    for (int j = 0; j < 8; j++)
                    {
                        char c = *src++;
                        if (c != '0' && c != '1')
                            throw new ArgumentException($"Invalid character '{c}' in Base2 string");

                        value |= (byte)(c - '0' << 7 - j);
                    }
                    *dest++ = value;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Provides extension methods for Base2 encoding and decoding operations.
    /// </summary>
    public static class Base2Extension
    {
        /// <summary>
        /// Encodes a string into Base2 format with specified encoding and endianness.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="isBigEndian">Whether to use big-endian byte order (default: false).</param>
        /// <returns>The Base2 encoded string.</returns>
        public static string EncodeBase2(this string input, StringEncoding encoding, bool isBigEndian = false) => Base2.Encode(input, encoding, isBigEndian);

        /// <summary>
        /// Encodes a string into Base2 format using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="isBigEndian">Whether to use big-endian byte order (default: false).</param>
        /// <returns>The Base2 encoded string.</returns>
        public static string EncodeBase2(this string input, bool isBigEndian = false) => Base2.Encode(input, isBigEndian);

        /// <summary>
        /// Decodes a Base2 string back to the original string with specified encoding and endianness.
        /// </summary>
        /// <param name="input">The Base2 string to decode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <param name="isBigEndian">Whether the data is in big-endian byte order (default: false).</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeBase2(this string input, StringEncoding encoding, bool isBigEndian = false) => Base2.Decode(input, encoding, isBigEndian);

        /// <summary>
        /// Decodes a Base2 string back to the original string using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The Base2 string to decode.</param>
        /// <param name="isBigEndian">Whether the data is in big-endian byte order (default: false).</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeBase2(this string input, bool isBigEndian = false) => Base2.Decode(input, isBigEndian);

        /// <summary>
        /// Encodes a byte array into Base2 format.
        /// </summary>
        /// <param name="bytes">The byte array to encode.</param>
        /// <returns>The Base2 encoded string.</returns>
        public static string EncodeBase2(this byte[] bytes) => Base2.BytesToBase2(bytes);

        /// <summary>
        /// Decodes a Base2 string back to a byte array.
        /// </summary>
        /// <param name="base2">The Base2 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        public static byte[] DecodeBase2(this string base2) => Base2.Base2ToBytes(base2);
    }
}