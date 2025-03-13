using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System;

namespace QingYi.Core.String.Base
{
    public static class Base32WordSafe
    {
        private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        private const byte Padding = (byte)'=';
        private const int BitsPerChar = 5;
        private const int ChunkSize = 5;

        private static readonly Encoding[] _encodings;
        private static readonly Dictionary<char, byte> _reverseLookup;

        static Base32WordSafe()
        {
            _encodings = new Encoding[6];
            _encodings[(int)StringEncoding.UTF8] = Encoding.UTF8;
            _encodings[(int)StringEncoding.UTF16LE] = Encoding.Unicode;
            _encodings[(int)StringEncoding.UTF16BE] = Encoding.BigEndianUnicode;
            _encodings[(int)StringEncoding.UTF32] = Encoding.UTF32;
            _encodings[(int)StringEncoding.UTF7] = Encoding.UTF7;
            _encodings[(int)StringEncoding.ASCII] = Encoding.ASCII;
#if NET6_0_OR_GREATER
            _encodings[(int)StringEncoding.Latin1] = Encoding.Latin1;
#endif

            _reverseLookup = new Dictionary<char, byte>(Base32Alphabet.Length);
            for (byte i = 0; i < Base32Alphabet.Length; i++)
                _reverseLookup[Base32Alphabet[i]] = i;
        }

        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var bytes = _encodings[(int)encoding].GetBytes(input);
            return Encode(bytes);
        }

        public static string Decode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var bytes = Decode(input.AsSpan());
            return _encodings[(int)encoding].GetString(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe string Encode(byte[] input)
        {
            if (input.Length == 0) return string.Empty;

            // 修正后的长度计算公式
            int bitCount = input.Length * 8;
            int baseLength = (bitCount + 4) / 5;
            int outputLength = (baseLength + 7) & ~7;

            char[]? rentedArray = null;
            Span<char> output = outputLength <= 256
                ? stackalloc char[outputLength]
                : (rentedArray = ArrayPool<char>.Shared.Rent(outputLength));

            try
            {
                fixed (byte* inputPtr = input)
                fixed (char* outputPtr = output)
                {
                    char* currentOutput = outputPtr;
                    byte* currentInput = inputPtr;
                    byte* endInput = inputPtr + input.Length;

                    int buffer = 0;
                    int bitsLeft = 0;
                    int charsWritten = 0;

                    // 关键修复：添加输出缓冲区边界检查
                    while ((currentInput < endInput || bitsLeft > 0) && charsWritten < output.Length)
                    {
                        if (bitsLeft < BitsPerChar && currentInput < endInput)
                        {
                            buffer = (buffer << 8) | *currentInput++;
                            bitsLeft += 8;
                        }

                        int bitsToTake = Math.Min(BitsPerChar, bitsLeft);
                        int val = (buffer >> (bitsLeft - bitsToTake)) & ((1 << bitsToTake) - 1);
                        val <<= (BitsPerChar - bitsToTake);

                        // 再次检查缓冲区边界
                        if (charsWritten >= output.Length)
                            break;

                        *currentOutput++ = Base32Alphabet[val];
                        charsWritten++;
                        bitsLeft -= bitsToTake;
                    }

                    // 填充阶段添加双重保护
                    while (charsWritten % 8 != 0 && charsWritten < output.Length)
                    {
                        *currentOutput++ = (char)Padding;
                        charsWritten++;
                    }

                    // 最终长度验证
                    if (charsWritten != outputLength)
                        throw new InvalidOperationException($"Length mismatch: {charsWritten} vs {outputLength}");

                    return new string(outputPtr, 0, charsWritten);
                }
            }
            finally
            {
                if (rentedArray != null)
                    ArrayPool<char>.Shared.Return(rentedArray);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe byte[] Decode(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty) return Array.Empty<byte>();

            int paddingCount = 0;
            int length = input.Length;
            while (length > 0 && input[length - 1] == Padding)
            {
                paddingCount++;
                length--;
            }

            int outputLength = (length * BitsPerChar) / 8;
            byte[] output = new byte[outputLength];

            fixed (char* inputPtr = input)
            fixed (byte* outputPtr = output)
            {
                char* currentInput = inputPtr;
                byte* currentOutput = outputPtr;
                byte* endOutput = outputPtr + output.Length;

                int buffer = 0;
                int bitsLeft = 0;

                while (currentInput < inputPtr + length)
                {
                    char c = *currentInput++;
                    if (!_reverseLookup.TryGetValue(c, out byte value))
                        throw new FormatException($"Invalid character in Base32 string: {c}");

                    buffer = (buffer << BitsPerChar) | value;
                    bitsLeft += BitsPerChar;

                    while (bitsLeft >= 8 && currentOutput < endOutput)
                    {
                        bitsLeft -= 8;
                        *currentOutput++ = (byte)((buffer >> bitsLeft) & 0xFF);
                    }
                }
            }

            return output;
        }
    }
}
