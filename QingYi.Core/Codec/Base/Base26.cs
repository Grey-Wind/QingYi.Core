#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base26 encoding and decoding functionality.
    /// </summary>
    /// <remarks>
    /// Base26 is an encoding scheme that represents binary data using the 26 letters of the English alphabet.
    /// This implementation supports both uppercase and lowercase letters and optional minimum length padding.
    /// </remarks>
    public class Base26
    {
        private readonly bool _useUpperCase;
        private readonly int _minLength;
        private readonly string _charSet;
        private readonly byte[] _charMap = new byte[128]; // ASCII lookup table

        /// <summary>
        /// Initializes a new instance of the Base26 encoder/decoder.
        /// </summary>
        /// <param name="useUpperCase">Whether to use uppercase letters (default: true).</param>
        /// <param name="minLength">Minimum length of encoded output (padded with '=' if needed).</param>
        public Base26(bool useUpperCase = true, int minLength = 0)
        {
            _useUpperCase = useUpperCase;
            _minLength = minLength;
            _charSet = _useUpperCase ? "ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "abcdefghijklmnopqrstuvwxyz";

            // Initialize character mapping table
            for (int i = 0; i < _charMap.Length; i++)
            {
                _charMap[i] = 0xFF; // 0xFF indicates invalid character
            }

            for (byte idx = 0; idx < 26; idx++)
            {
                _charMap[_charSet[idx]] = idx;
            }
        }

        /// <summary>
        /// Returns the character set used for encoding.
        /// </summary>
        /// <returns>The current character set string.</returns>
        public override string ToString() => _charSet;

        #region Byte Array Encoding/Decoding
        /// <summary>
        /// Encodes a byte array into Base26 format.
        /// </summary>
        /// <param name="data">The byte array to encode.</param>
        /// <returns>The Base26 encoded string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public string Encode(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return string.Empty;

            List<byte> digits = new List<byte>();
            unsafe
            {
                fixed (byte* ptr = data)
                {
                    byte[] buffer = new byte[data.Length];
                    fixed (byte* bufPtr = buffer)
                    {
                        Buffer.MemoryCopy(ptr, bufPtr, data.Length, data.Length);
                        byte* start = bufPtr;
                        int currentLen = data.Length;

                        while (currentLen > 0)
                        {
                            int remainder = 0;
                            bool allZero = true;

                            for (int i = 0; i < currentLen; i++)
                            {
                                int temp = (remainder << 8) | start[i];
                                start[i] = (byte)(temp / 26);
                                remainder = temp % 26;

                                if (start[i] != 0) allZero = false;
                            }

                            digits.Add((byte)remainder);

                            if (allZero) break;

                            while (currentLen > 0 && *start == 0)
                            {
                                start++;
                                currentLen--;
                            }
                        }
                    }
                }
            }

            digits.Reverse();
            return BuildString(digits);
        }

        /// <summary>
        /// Decodes a Base26 string back to a byte array.
        /// </summary>
        /// <param name="base26">The Base26 string to decode.</param>
        /// <returns>The decoded byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        /// <exception cref="FormatException">Thrown when input contains invalid Base26 characters.</exception>
        public byte[] Decode(string base26)
        {
            if (base26 == null) throw new ArgumentNullException(nameof(base26));
            if (base26.Length == 0) return Array.Empty<byte>();

            // Skip leading padding characters '='
            int startIndex = 0;
            while (startIndex < base26.Length && base26[startIndex] == '=')
                startIndex++;

            if (startIndex == base26.Length)
                return Array.Empty<byte>();

            // Convert to digit sequence
            List<byte> digits = new List<byte>(base26.Length - startIndex);
            for (int i = startIndex; i < base26.Length; i++)
            {
                char c = base26[i];
                // Stop decoding if padding character encountered
                if (c == '=') break;

                if (c >= 128 || _charMap[c] == 0xFF)
                    throw new FormatException($"Invalid Base26 character: '{c}'");

                digits.Add(_charMap[c]);
            }

            // Big integer multiplication conversion
            List<byte> result = new List<byte>(digits.Count * 2);
            foreach (byte digit in digits)
            {
                int carry = digit;
                for (int i = 0; i < result.Count; i++)
                {
                    int temp = result[i] * 26 + carry;
                    result[i] = (byte)(temp & 0xFF);
                    carry = temp >> 8;
                }

                while (carry > 0)
                {
                    result.Add((byte)(carry & 0xFF));
                    carry >>= 8;
                }
            }

            result.Reverse();
            return result.ToArray();
        }
        #endregion

        #region String Encoding/Decoding
        /// <summary>
        /// Encodes a string into Base26 format using UTF-8 encoding.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <returns>The Base26 encoded string.</returns>
        public string Encode(string text) => Encode(text, StringEncoding.UTF8);

        /// <summary>
        /// Encodes a string into Base26 format using the specified text encoding.
        /// </summary>
        /// <param name="text">The string to encode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The Base26 encoded string.</returns>
        public string Encode(string text, StringEncoding encoding)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));
            return Encode(GetBytes(text, encoding));
        }

        /// <summary>
        /// Decodes a Base26 string back to the original string using UTF-8 encoding.
        /// </summary>
        /// <param name="base26">The Base26 string to decode.</param>
        /// <returns>The decoded string.</returns>
        public string DecodeToString(string base26) => DecodeToString(base26, StringEncoding.UTF8);

        /// <summary>
        /// Decodes a Base26 string back to the original string using the specified text encoding.
        /// </summary>
        /// <param name="base26">The Base26 string to decode.</param>
        /// <param name="encoding">The text encoding to use.</param>
        /// <returns>The decoded string.</returns>
        public string DecodeToString(string base26, StringEncoding encoding)
        {
            if (base26 == null) throw new ArgumentNullException(nameof(base26));
            byte[] data = Decode(base26);
            return GetString(data, encoding);
        }
        #endregion

        #region Private Helper Methods
        private string BuildString(List<byte> digits)
        {
            StringBuilder sb = new StringBuilder(digits.Count);
            foreach (byte digit in digits)
            {
                sb.Append(_charSet[digit]);
            }

            // Add leading padding '='
            if (sb.Length < _minLength)
            {
                sb.Insert(0, new string('=', _minLength - sb.Length));
            }

            return sb.ToString();
        }

        private static byte[] GetBytes(string text, StringEncoding encoding)
        {
            switch (encoding)
            {
                case StringEncoding.UTF8:
                    return Encoding.UTF8.GetBytes(text);
                case StringEncoding.UTF16LE:
                    return Encoding.Unicode.GetBytes(text);
                case StringEncoding.UTF16BE:
                    return Encoding.BigEndianUnicode.GetBytes(text);
                case StringEncoding.ASCII:
                    return Encoding.ASCII.GetBytes(text);
                case StringEncoding.UTF32:
                    return Encoding.UTF32.GetBytes(text);
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1:
                    return Encoding.Latin1.GetBytes(text);
#endif
#pragma warning disable SYSLIB0001, CS0618
                case StringEncoding.UTF7:
                    return Encoding.UTF7.GetBytes(text);
#pragma warning restore SYSLIB0001, CS0618
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), "Unsupported encoding format");
            }
        }

        private static string GetString(byte[] data, StringEncoding encoding)
        {
            switch (encoding)
            {
                case StringEncoding.UTF8:
                    return Encoding.UTF8.GetString(data);
                case StringEncoding.UTF16LE:
                    return Encoding.Unicode.GetString(data);
                case StringEncoding.UTF16BE:
                    return Encoding.BigEndianUnicode.GetString(data);
                case StringEncoding.ASCII:
                    return Encoding.ASCII.GetString(data);
                case StringEncoding.UTF32:
                    return Encoding.UTF32.GetString(data);
#if NET6_0_OR_GREATER
                case StringEncoding.Latin1:
                    return Encoding.Latin1.GetString(data);
#endif
#pragma warning disable SYSLIB0001, CS0618
                case StringEncoding.UTF7:
                    return Encoding.UTF7.GetString(data);
#pragma warning restore SYSLIB0001, CS0618
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding), "Unsupported encoding format");
            }
        }
        #endregion
    }
}
#endif
