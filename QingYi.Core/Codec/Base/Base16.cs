using System.Text;
using System;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base16 (hexadecimal) encoding and decoding functionality.
    /// </summary>
    /// <remarks>
    /// Base16 is a binary-to-text encoding scheme that represents binary data in ASCII format
    /// using 16 distinct symbols (0-9 and A-F). This implementation supports both uppercase
    /// and lowercase hexadecimal output.
    /// </remarks>
    public class Base16
    {
        private static readonly uint[] Lookup32Lower = CreateLookup32('x');
        private static readonly uint[] Lookup32Upper = CreateLookup32('X');
        private static readonly byte[] LookupHex = CreateHexLookup();

        private static uint[] CreateLookup32(char format)
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString(format + "2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        private static byte[] CreateHexLookup()
        {
            var result = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                if (i >= '0' && i <= '9')
                    result[i] = (byte)(i - '0');
                else if (i >= 'A' && i <= 'F')
                    result[i] = (byte)(i - 'A' + 10);
                else if (i >= 'a' && i <= 'f')
                    result[i] = (byte)(i - 'a' + 10);
                else
                    result[i] = 0xFF;
            }
            return result;
        }

        /// <summary>
        /// Encodes a string into Base16 format using the specified text encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use for string-to-byte conversion (default: UTF-8).</param>
        /// <returns>The Base16 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            byte[] bytes = GetBytes(input, encoding);
            return Encode(bytes);
        }

        /// <summary>
        /// Encodes a byte array into Base16 format.
        /// </summary>
        /// <param name="bytes">The byte array to encode.</param>
        /// <param name="lowerCase">Whether to use lowercase letters (a-f) instead of uppercase (A-F).</param>
        /// <returns>The Base16 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public static string Encode(byte[] bytes, bool lowerCase = false)
        {
            if (bytes == null) throw new ArgumentNullException(nameof(bytes));
            if (bytes.Length == 0) return string.Empty;

            string result = new string('\0', bytes.Length * 2);
            unsafe
            {
                fixed (byte* bytesPtr = bytes)
                fixed (char* resultPtr = result)
                fixed (uint* lowerPtr = Lookup32Lower)
                fixed (uint* upperPtr = Lookup32Upper)
                {
                    uint* lookup = lowerCase ? lowerPtr : upperPtr;
                    uint* resultUIntPtr = (uint*)resultPtr;
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        resultUIntPtr[i] = lookup[bytesPtr[i]];
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Decodes a Base16 string into the original string using the specified text encoding.
        /// </summary>
        /// <param name="base16String">The Base16 string to decode.</param>
        /// <param name="encoding">The text encoding to use for byte-to-string conversion (default: UTF-8).</param>
        /// <returns>The decoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        /// <exception cref="ArgumentException">Thrown when input contains invalid Base16 characters.</exception>
        public static string Decode(string base16String, StringEncoding encoding = StringEncoding.UTF8) =>
            GetString(Decode(base16String), encoding);

        /// <summary>
        /// Decodes a Base16 string into a byte array.
        /// </summary>
        /// <param name="base16String">The Base16 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when input length is not even or contains invalid Base16 characters.
        /// </exception>
        public static byte[] Decode(string base16String)
        {
            if (base16String == null) throw new ArgumentNullException(nameof(base16String));
            if (base16String.Length % 2 != 0)
                throw new ArgumentException("Base16 string length must be even.");

            if (base16String.Length == 0)
#if NET45 || NET451 || NET452
                return new byte[] { };
#else
                return Array.Empty<byte>();
#endif

            byte[] result = new byte[base16String.Length / 2];
            unsafe
            {
                fixed (char* inputPtr = base16String)
                fixed (byte* resultPtr = result)
                {
                    byte* currentResult = resultPtr;
                    for (int i = 0; i < base16String.Length; i += 2)
                    {
                        char c1 = inputPtr[i];
                        char c2 = inputPtr[i + 1];

                        if (c1 > 255 || c2 > 255)
                            ThrowInvalidCharacter();

                        byte b1 = LookupHex[(byte)c1];
                        byte b2 = LookupHex[(byte)c2];

                        if (b1 == 0xFF || b2 == 0xFF)
                            ThrowInvalidCharacter();

                        *currentResult++ = (byte)(b1 << 4 | b2);
                    }
                }
            }
            return result;
        }

        private static void ThrowInvalidCharacter() => throw new ArgumentException("Invalid Base16 character.");

        private static byte[] GetBytes(string input, StringEncoding encoding) =>
            GetEncoding(encoding).GetBytes(input);

        private static string GetString(byte[] bytes, StringEncoding encoding) =>
            GetEncoding(encoding).GetString(bytes);

        /// <summary>
        /// Gets the System.Text.Encoding instance corresponding to the specified StringEncoding.
        /// </summary>
        /// <param name="encoding">The StringEncoding value to convert.</param>
        /// <returns>The corresponding Encoding instance.</returns>
        /// <exception cref="NotSupportedException">Thrown when an unsupported encoding is specified.</exception>
        private static Encoding GetEncoding(StringEncoding encoding)
        {
            switch (encoding)
            {
                case StringEncoding.UTF8: return Encoding.UTF8;
                case StringEncoding.UTF16LE: return Encoding.Unicode;
                case StringEncoding.UTF16BE: return Encoding.BigEndianUnicode;
                case StringEncoding.ASCII: return Encoding.ASCII;
                case StringEncoding.UTF32: return Encoding.UTF32;
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1: return Encoding.Latin1;
#endif
#pragma warning disable CS0618, SYSLIB0001
                case StringEncoding.UTF7: return Encoding.UTF7;
#pragma warning restore CS0618, SYSLIB0001
                default: throw new NotSupportedException($"Encoding {encoding} is not supported.");
            }
        }
    }

    /// <summary>
    /// Provides extension methods for Base16 encoding and decoding operations on strings.
    /// </summary>
    public static class Base16Extension
    {
        /// <summary>
        /// Encodes a string into Base16 format using the specified text encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use for string-to-byte conversion (default: UTF-8).</param>
        /// <returns>The Base16 encoded string.</returns>
        public static string EncodeBase16(this string input, StringEncoding encoding = StringEncoding.UTF8) =>
            Base16.Encode(input, encoding);

        /// <summary>
        /// Encodes a byte array into Base16 format.
        /// </summary>
        /// <param name="bytes">The byte array to encode.</param>
        /// <param name="lowerCase">Whether to use lowercase letters (a-f) instead of uppercase (A-F).</param>
        /// <returns>The Base16 encoded string.</returns>
        public static string EncodeBase16(byte[] bytes, bool lowerCase = false) =>
            Base16.Encode(bytes, lowerCase);

        /// <summary>
        /// Decodes a Base16 string into the original string using the specified text encoding.
        /// </summary>
        /// <param name="base16String">The Base16 string to decode.</param>
        /// <param name="encoding">The text encoding to use for byte-to-string conversion (default: UTF-8).</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeBase16(this string base16String, StringEncoding encoding = StringEncoding.UTF8) =>
            Base16.Decode(base16String, encoding);

        /// <summary>
        /// Decodes a Base16 string into a byte array.
        /// </summary>
        /// <param name="base16String">The Base16 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        public static byte[] DecodeBase16(this string base16String) =>
            Base16.Decode(base16String);
    }
}