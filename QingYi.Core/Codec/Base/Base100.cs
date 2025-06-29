using System;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base100 encoding and decoding functionality using emoji characters.
    /// </summary>
    public class Base100
    {
        /// <summary>
        /// Gets the complete character set string used for Base100 encoding (256 emoji characters).
        /// </summary>
        public static string CharacterSet => GenerateCharacterSet();

        /// <summary>
        /// Overrides ToString() to return the Base100 character set.
        /// </summary>
        /// <returns>The Base100 character set string.</returns>
        public override string ToString() => CharacterSet;

        #region Encoding Methods
        /// <summary>
        /// Encodes a byte array into a Base100 string.
        /// </summary>
        /// <param name="data">The byte array to encode.</param>
        /// <returns>The Base100 encoded string.</returns>
        public static string Encode(byte[] data) => EncodeBytes(data);

        /// <summary>
        /// Encodes a text string into a Base100 string using the specified encoding.
        /// </summary>
        /// <param name="text">The text string to encode.</param>
        /// <param name="encoding">The text encoding to use (default is UTF-8).</param>
        /// <returns>The Base100 encoded string.</returns>
        public static string Encode(string text, StringEncoding encoding = StringEncoding.UTF8)
        {
            byte[] bytes = GetEncoding(encoding).GetBytes(text);
            return EncodeBytes(bytes);
        }
        #endregion

        #region Decoding Methods
        /// <summary>
        /// Decodes a Base100 string into a byte array.
        /// </summary>
        /// <param name="base100Text">The Base100 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        public static byte[] Decode(string base100Text) => DecodeToBytes(base100Text);

        /// <summary>
        /// Decodes a Base100 string into a text string using the specified encoding.
        /// </summary>
        /// <param name="base100Text">The Base100 string to decode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The decoded text string.</returns>
        public static string Decode(string base100Text, StringEncoding encoding)
        {
            byte[] bytes = DecodeToBytes(base100Text);
            return GetEncoding(encoding).GetString(bytes);
        }
        #endregion

        #region Core Encoding/Decoding Implementation
        private static unsafe string EncodeBytes(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return string.Empty;

            int charCount = data.Length * 2;
            char[] buffer = new char[charCount];

            fixed (byte* pData = data)
            fixed (char* pBuffer = buffer)
            {
                byte* src = pData;
                char* dest = pBuffer;

                for (int i = 0; i < data.Length; i++)
                {
                    byte b = *src++;
                    *dest++ = (char)0xD83C;        // High surrogate fixed value
                    *dest++ = (char)(0xDF00 + b);  // Low surrogate calculation
                }
            }

            return new string(buffer);
        }

        private static unsafe byte[] DecodeToBytes(string base100Text)
        {
            if (base100Text == null) throw new ArgumentNullException(nameof(base100Text));
            if (base100Text.Length == 0) return Array.Empty<byte>();
            if (base100Text.Length % 2 != 0)
                throw new ArgumentException("Base100 text length must be even", nameof(base100Text));

            int byteCount = base100Text.Length / 2;
            byte[] result = new byte[byteCount];

            fixed (char* pText = base100Text)
            fixed (byte* pResult = result)
            {
                char* src = pText;
                byte* dest = pResult;

                for (int i = 0; i < byteCount; i++)
                {
                    char high = *src++;
                    char low = *src++;

                    if (high != 0xD83C || low < 0xDF00 || low > 0xDFFF)
                        throw new FormatException($"Invalid Base100 character pair at position: {i * 2}");

                    *dest++ = (byte)(low - 0xDF00);
                }
            }

            return result;
        }
        #endregion

        #region Encoding Conversion Helpers
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
                    throw new NotSupportedException($"Unsupported encoding: {encoding}");
            }
        }
        #endregion

        #region Character Set Generation
        private static unsafe string GenerateCharacterSet()
        {
            const int charCount = 256 * 2;
            char[] chars = new char[charCount];

            fixed (char* pChars = chars)
            {
                char* ptr = pChars;
                for (int i = 0; i < 256; i++)
                {
                    *ptr++ = (char)0xD83C;
                    *ptr++ = (char)(0xDF00 + i);
                }
            }

            return new string(chars);
        }
        #endregion
    }
}
