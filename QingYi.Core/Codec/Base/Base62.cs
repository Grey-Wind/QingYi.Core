#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base62 encoding and decoding functionality
    /// </summary>
    public class Base62
    {
#nullable enable
        // Base62 character set (0-9, A-Z, a-z)
        private const string Characters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        // Latin1 encoding instance for efficient byte operations
        private static readonly Encoding s_latin1Encoding = GetLatin1Encoding();

        /// <summary>
        /// Calculates the encoded length for a given input length
        /// </summary>
        /// <param name="inputLength">Length of input bytes</param>
        /// <returns>Required output length for Base62 encoding</returns>
        private static int GetEncodedLength(int inputLength)
        {
            int bits = inputLength * 8;
            return (bits + 5) / 6; // Ceiling division by 6
        }

#if NET6_0_OR_GREATER
        /// <summary>
        /// Gets Latin1 encoding instance (built-in in .NET 6+)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Encoding GetLatin1Encoding() => Encoding.Latin1;
#else
        /// <summary>
        /// Gets Latin1 encoding instance (code page 28591)
        /// </summary>
        private static Encoding GetLatin1Encoding() => Encoding.GetEncoding(28591);
#endif

        /// <summary>
        /// Encodes a string to Base62 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base62 encoded string</returns>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            byte[]? rentedBuffer = null;

            try
            {
                // Calculate maximum possible byte count and rent buffer
                var byteCount = GetMaxByteCount(input, encoding);
                rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
                var bytes = rentedBuffer.AsSpan();
                var written = GetBytes(input, bytes, encoding);
                return Encode(bytes.Slice(0, written));
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        /// <summary>
        /// Converts string to bytes using specified encoding (unsafe optimized version)
        /// </summary>
        private static unsafe int GetBytes(string input, Span<byte> destination, StringEncoding encoding)
        {
            fixed (char* pInput = input)
            fixed (byte* pDest = destination)
            {
                return encoding switch
                {
                    StringEncoding.UTF8 => Encoding.UTF8.GetBytes(pInput, input.Length, pDest, destination.Length),
                    StringEncoding.UTF16LE => Encoding.Unicode.GetBytes(pInput, input.Length, pDest, destination.Length),
                    StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetBytes(pInput, input.Length, pDest, destination.Length),
                    StringEncoding.ASCII => Encoding.ASCII.GetBytes(pInput, input.Length, pDest, destination.Length),
                    StringEncoding.UTF32 => Encoding.UTF32.GetBytes(pInput, input.Length, pDest, destination.Length),
#if NET6_0_OR_GREATER
                    StringEncoding.Latin1 => s_latin1Encoding.GetBytes(pInput, input.Length, pDest, destination.Length),
#endif
                    _ => throw new NotSupportedException("Unsupported encoding")
                };
            }
        }

        /// <summary>
        /// Encodes binary data to a Base62 string
        /// </summary>
        /// <param name="input">Binary data to encode</param>
        /// <returns>Base62 encoded string</returns>
        public static unsafe string Encode(ReadOnlySpan<byte> input)
        {
            if (input.IsEmpty) return string.Empty;

            int outputLength = GetEncodedLength(input.Length);
            char[]? rentedArray = null;

            try
            {
                // Rent character array for output
                rentedArray = ArrayPool<char>.Shared.Rent(outputLength);
                Span<char> output = rentedArray;

                fixed (byte* pInput = input)
                fixed (char* pOutput = output)
                {
                    byte* currentInput = pInput;
                    char* currentOutput = pOutput;
                    int remaining = input.Length;
                    int outputIndex = 0;

                    // Bit buffer for efficient encoding
                    ulong buffer = 0;
                    int bitsInBuffer = 0;

                    while (remaining > 0 || bitsInBuffer > 0)
                    {
                        // Fill buffer with input bytes
                        while (bitsInBuffer < 24 && remaining > 0)
                        {
                            buffer = (buffer << 8) | *currentInput++;
                            bitsInBuffer += 8;
                            remaining--;
                        }

                        // Extract 6 bits at a time
                        int take = Math.Min(6, bitsInBuffer);
                        if (take == 0) break;

                        int index = (int)(((uint)(buffer >> (bitsInBuffer - take))) & ((1 << take) - 1));
                        index <<= (6 - take);
                        *currentOutput++ = Characters[index];
                        outputIndex++;

                        bitsInBuffer -= take;
                    }

                    return new string(pOutput, 0, outputIndex);
                }
            }
            finally
            {
                if (rentedArray != null)
                    ArrayPool<char>.Shared.Return(rentedArray);
            }
        }

        /// <summary>
        /// Decodes a Base62 string to text using the specified encoding
        /// </summary>
        /// <param name="base62">Base62 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string Decode(string base62, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (string.IsNullOrEmpty(base62)) return string.Empty;

            byte[]? rentedBuffer = null;
            try
            {
                var maxByteCount = GetMaxByteCount(base62);
                rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
                var bytes = rentedBuffer.AsSpan();
                var written = DecodeInternal(base62, bytes);
                return GetString(bytes[..written], encoding);
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        /// <summary>
        /// Decodes a Base62 string to binary data as Span&lt;byte&gt;
        /// </summary>
        /// <param name="base62">Base62 encoded string</param>
        /// <returns>Decoded binary data as Span&lt;byte&gt;</returns>
        public static Span<byte> DecodeToSpanByte(string base62)
        {
            if (string.IsNullOrEmpty(base62)) return Span<byte>.Empty;

            byte[]? rentedBuffer = null;
            try
            {
                var maxByteCount = GetMaxByteCount(base62);
                rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount);
                var bytes = rentedBuffer.AsSpan();
                var written = DecodeInternal(base62, bytes);
                return bytes[..written];
            }
            finally
            {
                if (rentedBuffer != null)
                    ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }

        /// <summary>
        /// Decodes a Base62 string to binary data
        /// </summary>
        /// <param name="base62">Base62 encoded string</param>
        /// <returns>Decoded binary data</returns>
        public static byte[] Decode(string base62) => DecodeToSpanByte(base62).ToArray();

        /// <summary>
        /// Converts bytes to string using specified encoding
        /// </summary>
        private static unsafe string GetString(ReadOnlySpan<byte> bytes, StringEncoding encoding)
        {
            fixed (byte* pBytes = bytes)
            {
                return encoding switch
                {
                    StringEncoding.UTF8 => Encoding.UTF8.GetString(pBytes, bytes.Length),
                    StringEncoding.UTF16LE => Encoding.Unicode.GetString(pBytes, bytes.Length),
                    StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetString(pBytes, bytes.Length),
                    StringEncoding.ASCII => Encoding.ASCII.GetString(pBytes, bytes.Length),
                    StringEncoding.UTF32 => Encoding.UTF32.GetString(pBytes, bytes.Length),
#if NET6_0_OR_GREATER
                    StringEncoding.Latin1 => s_latin1Encoding.GetString(pBytes, bytes.Length),
#endif
                    _ => throw new NotSupportedException("Unsupported encoding")
                };
            }
        }

        /// <summary>
        /// Internal Base62 decoding implementation
        /// </summary>
        private static unsafe int DecodeInternal(string base62, Span<byte> output)
        {
            fixed (char* pInput = base62)
            fixed (byte* pOutput = output)
            {
                char* currentChar = pInput;
                byte* currentByte = pOutput;
                int outputIndex = 0;
                ulong buffer = 0;
                int bits = 0;

                for (int i = 0; i < base62.Length; i++)
                {
                    char c = *currentChar++;
                    int value = Characters.IndexOf(c);
                    if (value < 0) throw new ArgumentException("Invalid Base62 character: " + c);

                    // Shift buffer and add new value
#pragma warning disable 0675
                    buffer = (buffer << 6) | (ulong)value;
#pragma warning restore 0675
                    bits += 6;

                    // Extract complete bytes when we have enough bits
                    while (bits >= 8)
                    {
                        bits -= 8;
                        *currentByte++ = (byte)(buffer >> bits);
                        outputIndex++;
                        buffer &= (1UL << bits) - 1;
                    }
                }

                // Handle remaining bits (if any)
                if (bits > 0)
                {
                    *currentByte++ = (byte)(buffer << (8 - bits));
                    outputIndex++;
                }

                return outputIndex;
            }
        }

        /// <summary>
        /// Gets maximum byte count for a given character count and encoding
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetMaxByteCount(int charCount, StringEncoding encoding) => encoding switch
        {
            StringEncoding.UTF8 => Encoding.UTF8.GetMaxByteCount(charCount),
            StringEncoding.UTF16LE => Encoding.Unicode.GetMaxByteCount(charCount),
            StringEncoding.UTF16BE => Encoding.BigEndianUnicode.GetMaxByteCount(charCount),
            StringEncoding.ASCII => Encoding.ASCII.GetMaxByteCount(charCount),
            StringEncoding.UTF32 => Encoding.UTF32.GetMaxByteCount(charCount),
#if NET6_0_OR_GREATER
            StringEncoding.Latin1 => charCount,
#endif
            _ => throw new NotSupportedException("Unsupported encoding")
        };

        /// <summary>
        /// Gets maximum byte count for a string with specified encoding
        /// </summary>
        private static int GetMaxByteCount(string input, StringEncoding encoding)
            => GetMaxByteCount(input.Length, encoding);

        /// <summary>
        /// Estimates maximum byte count for a Base62 string
        /// </summary>
        private static int GetMaxByteCount(string base62)
            => (int)Math.Floor(base62.Length * 6 / 8.0);

        /// <summary>
        /// Gets the Base62 character set used for encoding
        /// </summary>
        /// <returns>The Base62 character set string</returns>
        public override string ToString() => Characters;
#nullable restore
    }

    /// <summary>
    /// Provides extension methods for Base62 encoding/decoding
    /// </summary>
    public static class Base62Extension
    {
        /// <summary>
        /// Encodes a string to Base62 using the specified text encoding
        /// </summary>
        /// <param name="input">String to encode</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Base62 encoded string</returns>
        public static string EncodeBase62(this string input, StringEncoding encoding = StringEncoding.UTF8) => Base62.Encode(input, encoding);

        /// <summary>
        /// Encodes binary data to a Base62 string
        /// </summary>
        /// <param name="input">Binary data to encode</param>
        /// <returns>Base62 encoded string</returns>
        public static unsafe string EncodeBase62(this ReadOnlySpan<byte> input) => Base62.Encode(input);

        /// <summary>
        /// Decodes a Base62 string to text using the specified encoding
        /// </summary>
        /// <param name="base62">Base62 encoded string</param>
        /// <param name="encoding">Text encoding to use (default: UTF-8)</param>
        /// <returns>Decoded string</returns>
        public static string DecodeBase62(this string base62, StringEncoding encoding = StringEncoding.UTF8) => Base62.Decode(base62, encoding);

        /// <summary>
        /// Decodes a Base62 string to binary data
        /// </summary>
        /// <param name="base62">Base62 encoded string</param>
        /// <returns>Decoded binary data</returns>
        public static byte[] DecodeBase62(this string base62) => Base62.Decode(base62);

        /// <summary>
        /// Decodes a Base62 string to binary data as Span&lt;byte&gt;
        /// </summary>
        /// <param name="base62">Base62 encoded string</param>
        /// <returns>Decoded binary data as Span&lt;byte&gt;</returns>
        public static Span<byte> DecodeBase62ToSpanByte(this string base62) => Base62.DecodeToSpanByte(base62);
    }
}
#endif
