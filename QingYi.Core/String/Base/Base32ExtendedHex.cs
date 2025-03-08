using System;
using System.Text;

namespace QingYi.Core.String.Base
{
    public static class Base32ExtendedHex
    {
        /// <summary>
        /// 字符串编码格式。<br />
        /// String encoding.
        /// </summary>
        public enum StringEncoding
        {
            /// <summary>
            /// UTF-8
            /// </summary>
            UTF8,

            /// <summary>
            /// UTF-16 LE
            /// </summary>
            UTF16LE,

            /// <summary>
            /// UTF-16 BE
            /// </summary>
            UTF16BE,

            /// <summary>
            /// ASCII
            /// </summary>
            ASCII,

            /// <summary>
            /// UTF-32
            /// </summary>
            UTF32,

#if NET6_0_OR_GREATER
            /// <summary>
            /// Latin1
            /// </summary>
            Latin1,
#endif
            /// <summary>
            /// UTF-7
            /// </summary>
            [Obsolete(message: "UTF-7 has been deprecated because it is obsolete.")]
            UTF7,
        }

        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
        private const char PaddingChar = '=';
        private static readonly byte[] DecodeMap = new byte[256];

        static Base32ExtendedHex()
        {
            Array.Fill(DecodeMap, (byte)0xFF);
            for (byte i = 0; i < Alphabet.Length; i++)
                DecodeMap[Alphabet[i]] = i;
            DecodeMap[PaddingChar] = 0xFE;
        }

        public static string Encode(string input, StringEncoding encoding)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            byte[] bytes = GetBytes(input, encoding);
            return ConvertToBase32(bytes);
        }

        public static string Decode(string base32, StringEncoding encoding)
        {
            if (base32 == null) throw new ArgumentNullException(nameof(base32));
            byte[] bytes = ConvertFromBase32(base32);
            return GetString(bytes, encoding);
        }

        private static unsafe string ConvertToBase32(byte[] input)
        {
            if (input.Length == 0) return string.Empty;

            int base32Length = (input.Length * 8 + 4) / 5;
            int paddingLength = (8 - base32Length % 8) % 8;
            char[] output = new char[base32Length + paddingLength];

            fixed (byte* inputPtr = input)
            fixed (char* outputPtr = output)
            {
                byte* currentInput = inputPtr;
                byte* endInput = inputPtr + input.Length;
                char* currentOutput = outputPtr;

                ulong buffer = 0;
                int bitsInBuffer = 0;

                while (currentInput < endInput)
                {
                    buffer = (buffer << 8) | *currentInput++;
                    bitsInBuffer += 8;

                    while (bitsInBuffer >= 5)
                    {
                        bitsInBuffer -= 5;
                        byte value = (byte)((buffer >> bitsInBuffer) & 0x1F);
                        *currentOutput++ = Alphabet[value];
                    }
                }

                if (bitsInBuffer > 0)
                {
                    buffer <<= (5 - bitsInBuffer);
                    byte value = (byte)(buffer & 0x1F);
                    *currentOutput++ = Alphabet[value];
                }

                while (currentOutput < outputPtr + output.Length)
                {
                    *currentOutput++ = PaddingChar;
                }
            }

            return new string(output);
        }

        private static unsafe byte[] ConvertFromBase32(string base32)
        {
            if (base32.Length == 0) return Array.Empty<byte>();

            // Calculate valid length ignoring padding
            int validLength = base32.Length;
            while (validLength > 0 && base32[validLength - 1] == PaddingChar)
                validLength--;

            // Calculate expected bytes and padding bits
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

                ulong buffer = 0;
                int bitsInBuffer = 0;

                while (currentInput < endInput)
                {
                    char c = *currentInput++;
                    byte value = DecodeMap[c];

                    if (value >= 0x20)
                    {
                        if (value == 0xFE)
                            throw new ArgumentException($"Invalid padding position: {c}");
                        throw new ArgumentException($"Invalid Base32 character: {c}");
                    }

                    buffer = (buffer << 5) | value;
                    bitsInBuffer += 5;

                    // Write bytes while we have enough bits
                    while (bitsInBuffer >= 8 && currentOutput < endOutput)
                    {
                        bitsInBuffer -= 8;
                        *currentOutput++ = (byte)((buffer >> bitsInBuffer) & 0xFF);
                    }
                }

                // Verify remaining bits match padding requirements
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

        private static byte[] GetBytes(string input, StringEncoding encoding) => GetEncoding(encoding).GetBytes(input);

        private static string GetString(byte[] bytes, StringEncoding encoding) => GetEncoding(encoding).GetString(bytes);

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
                _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
            };
        }
    }
}
