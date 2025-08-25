#if NET5_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base52 encoding and decoding functionality
    /// </summary>
    public class Base52
    {
        /// <summary>
        /// Base52 character set (26 uppercase letters + 26 lowercase letters)
        /// </summary>
        public const string CharacterSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// Padding character used for incomplete blocks
        /// </summary>
        public const char PaddingChar = '=';

        private const int BlockSize = 5;       // Number of characters per encoded block
        private const int ByteBlockSize = 3;   // Number of bytes per raw data block
        private static readonly byte[] CharToIndexMap = new byte[128]; // Character to value mapping
        private static readonly bool CharMapInitialized = InitializeCharMap();

        /// <summary>
        /// Initializes the character to index mapping table
        /// </summary>
        /// <returns>Always returns true</returns>
        private static bool InitializeCharMap()
        {
            // Mark all characters as invalid initially
            for (int i = 0; i < CharToIndexMap.Length; i++)
                CharToIndexMap[i] = 0xFF;

            // Populate valid Base52 characters
            for (byte i = 0; i < CharacterSet.Length; i++)
                CharToIndexMap[CharacterSet[i]] = i;

            return true;
        }

        /// <summary>
        /// Returns the Base52 character set
        /// </summary>
        /// <returns>The Base52 character set string</returns>
        public override string ToString() => CharacterSet;

        /// <summary>
        /// Encodes binary data to a Base52 string
        /// </summary>
        /// <param name="data">Binary data to encode</param>
        /// <returns>Base52 encoded string</returns>
        public unsafe string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            int length = data.Length;
            int fullBlocks = length / ByteBlockSize;
            int remainder = length % ByteBlockSize;

            // Calculate output length: (n/3)*5 + (remainder? 2/3/0) + padding
            int outputLength = fullBlocks * BlockSize;
            switch (remainder)
            {
                case 1: outputLength += 2; break; // 1 byte becomes 2 chars
                case 2: outputLength += 3; break; // 2 bytes become 3 chars
            }
            // Add padding if needed
            int padding = (BlockSize - (outputLength % BlockSize)) % BlockSize;
            outputLength += padding;

            char[] output = new char[outputLength];
            fixed (byte* inputPtr = data)
            fixed (char* outputPtr = output)
            fixed (char* charSetPtr = CharacterSet)
            {
                char* op = outputPtr;
                byte* ip = inputPtr;

                // Process complete 3-byte blocks
                for (int i = 0; i < fullBlocks; i++, ip += 3)
                {
                    // Combine 3 bytes into 24-bit value
                    uint value = (uint)(*ip << 16) | (uint)(ip[1] << 8) | ip[2];

                    // Split into 5 Base52 characters (52^4, 52^3, 52^2, 52^1, 52^0)
                    *op++ = charSetPtr[value / 7311616];          // 52^4
                    *op++ = charSetPtr[(value / 140608) % 52];    // 52^3
                    *op++ = charSetPtr[(value / 2704) % 52];      // 52^2
                    *op++ = charSetPtr[(value / 52) % 52];
                    *op++ = charSetPtr[value % 52];
                }

                // Process remaining bytes (1 or 2 bytes)
                if (remainder > 0)
                {
                    uint value = (uint)(*ip << 16);
                    if (remainder == 2) value |= (uint)(ip[1] << 8);

                    // First two characters are always present
                    *op++ = charSetPtr[value / 7311616];
                    *op++ = charSetPtr[(value / 140608) % 52];

                    // Third character for 2-byte remainder
                    if (remainder == 2)
                    {
                        *op++ = charSetPtr[(value / 2704) % 52];
                    }
                }

                // Add padding characters
                for (int i = 0; i < padding; i++)
                    outputPtr[outputLength - 1 - i] = PaddingChar;
            }

            return new string(output);
        }

        /// <summary>
        /// Decodes a Base52 string to binary data
        /// </summary>
        /// <param name="encoded">Base52 encoded string</param>
        /// <returns>Decoded binary data</returns>
        /// <exception cref="FormatException">Thrown for invalid Base52 strings</exception>
        public unsafe byte[] Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
                return Array.Empty<byte>();

            // Validate length is multiple of block size (5)
            if (encoded.Length % BlockSize != 0)
                throw new FormatException("Base52 string length must be a multiple of 5");

            int length = encoded.Length;
            int padding = CalculatePadding(encoded, length);
            int fullBlocks = (length - BlockSize) / BlockSize;
            int lastBlockLength = BlockSize - padding;
            int outputLength = fullBlocks * ByteBlockSize + GetOutputSize(lastBlockLength);

            byte[] output = new byte[outputLength];
            fixed (char* inputPtr = encoded)
            fixed (byte* outputPtr = output)
            {
                byte* op = outputPtr;
                char* ip = inputPtr;

                // Process complete blocks
                for (int i = 0; i < fullBlocks; i++, ip += BlockSize)
                {
                    uint value = 0;
                    // Combine 5 characters into 32-bit value
                    for (int j = 0; j < BlockSize; j++)
                    {
                        value *= 52;
                        value += GetCharValue(ip[j]);
                    }

                    // Split into 3 bytes
                    *op++ = (byte)(value >> 16);
                    *op++ = (byte)(value >> 8);
                    *op++ = (byte)value;
                }

                // Process last (possibly partial) block
                uint lastValue = 0;
                for (int j = 0; j < lastBlockLength; j++)
                {
                    lastValue *= 52;
                    lastValue += GetCharValue(ip[j]);
                }

                // Extract bytes based on characters in last block
                switch (lastBlockLength)
                {
                    case 5: // Full block (3 bytes)
                        *op++ = (byte)(lastValue >> 16);
                        *op++ = (byte)(lastValue >> 8);
                        *op++ = (byte)lastValue;
                        break;

                    case 3: // 2 bytes
                        *op++ = (byte)(lastValue >> 8);
                        *op++ = (byte)lastValue;
                        break;

                    case 2: // 1 byte
                        *op++ = (byte)lastValue;
                        break;
                }
            }

            return output;
        }

        #region String Encoding Overloads
        /// <summary>
        /// Encodes a string to Base52 using the specified text encoding
        /// </summary>
        /// <param name="text">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base52 encoded string</returns>
        public string Encode(string text, StringEncoding encoding = StringEncoding.UTF8)
            => Encode(GetBytes(text, encoding));

        /// <summary>
        /// Decodes a Base52 string to text using the specified encoding
        /// </summary>
        /// <param name="base52">Base52 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public string Decode(string base52, StringEncoding encoding = StringEncoding.UTF8)
            => GetString(Decode(base52), encoding);

        /// <summary>
        /// Static method to encode text to Base52
        /// </summary>
        public static string EncodeText(string text, StringEncoding encoding = StringEncoding.UTF8)
            => new Base52().Encode(text, encoding);

        /// <summary>
        /// Static method to decode Base52 to text
        /// </summary>
        public static string DecodeText(string base52, StringEncoding encoding = StringEncoding.UTF8)
            => new Base52().Decode(base52, encoding);
        #endregion

        #region Private Helper Methods
        /// <summary>
        /// Calculates the number of padding characters
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculatePadding(string encoded, int length)
        {
            int padding = 0;
            for (int i = length - 1; i >= 0 && encoded[i] == PaddingChar; i--)
                padding++;

            // Validate padding count (0, 2, or 3)
            if (padding != 0 && padding != 2 && padding != 3)
                throw new FormatException("Invalid padding format");

            return padding;
        }

        /// <summary>
        /// Gets the output size in bytes for a given number of characters
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetOutputSize(int charsInBlock)
            => charsInBlock switch
            {
                5 => 3, // 3 bytes (full block)
                3 => 2, // 2 bytes
                2 => 1, // 1 byte
                _ => throw new FormatException("Invalid character block size")
            };

        /// <summary>
        /// Gets the numerical value of a Base52 character
        /// </summary>
        /// <exception cref="FormatException">Thrown for invalid characters</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte GetCharValue(char c)
        {
            if (c >= CharToIndexMap.Length || CharToIndexMap[c] == 0xFF)
                throw new FormatException($"Invalid character: '{c}'");
            return CharToIndexMap[c];
        }

        /// <summary>
        /// Converts string to bytes using specified encoding
        /// </summary>
        private static byte[] GetBytes(string text, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8.GetBytes(text),
                StringEncoding.UTF16LE => Encoding.Unicode.GetBytes(text),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetBytes(text),
                StringEncoding.ASCII => Encoding.ASCII.GetBytes(text),
                StringEncoding.UTF32 => Encoding.UTF32.GetBytes(text),
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetBytes(text),
#endif
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => GetUTF7Bytes(text),
#pragma warning restore SYSLIB0001, CS0618
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };
        }

        /// <summary>
        /// Converts bytes to string using specified encoding
        /// </summary>
        private static string GetString(byte[] data, StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8.GetString(data),
                StringEncoding.UTF16LE => Encoding.Unicode.GetString(data),
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetString(data),
                StringEncoding.ASCII => Encoding.ASCII.GetString(data),
                StringEncoding.UTF32 => Encoding.UTF32.GetString(data),
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1.GetString(data),
#endif
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => GetUTF7String(data),
#pragma warning restore SYSLIB0001, CS0618
                _ => throw new ArgumentOutOfRangeException(nameof(encoding))
            };
        }

        // UTF-7 encoding methods (obsolete in newer .NET versions)
#pragma warning disable SYSLIB0001, CS0618
        private static byte[] GetUTF7Bytes(string text) => Encoding.UTF7.GetBytes(text);
        private static string GetUTF7String(byte[] data) => Encoding.UTF7.GetString(data);
#pragma warning restore SYSLIB0001, CS0618
        #endregion
    }
}
#endif