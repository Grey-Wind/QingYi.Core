#if NET5_0_OR_GREATER
#pragma warning disable CA2014, CS0618, SYSLIB0001
#nullable enable
using System;
using System.Buffers;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides Base36 encoding/decoding functionality.
    /// Base36 uses digits 0-9 and letters A-Z (case insensitive) for a compact alphanumeric representation.
    /// </summary>
    public unsafe class Base36
    {
        // Base36 character set (digits 0-9 followed by A-Z)
        private const string Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // Size of byte lookup table
        private const int ByteSize = 256;

        // Base value for Base36 calculations
        private const int Base = 36;

        // Precomputed powers of 36 for optimization
        private static readonly uint[] _pow36 = { 1, 36, 1296, 46656, 1679616, 60466176, 2176782336 };

        // Reverse lookup table for decoding characters
        private static readonly int[] _reverseBase36Map = new int[ByteSize];

        /// <summary>
        /// Static constructor initializes the decoding lookup table.
        /// </summary>
        static Base36()
        {
            // Initialize all entries as invalid (-1)
            Array.Fill(_reverseBase36Map, -1);

            // Map valid Base36 characters to their values (case insensitive)
            for (var i = 0; i < Base36Chars.Length; i++)
            {
                // Uppercase letters
                _reverseBase36Map[Base36Chars[i]] = i;
                // Lowercase letters
                _reverseBase36Map[char.ToLowerInvariant(Base36Chars[i])] = i;
            }
        }

        /// <summary>
        /// Gets the character set used for Base36 encoding.
        /// </summary>
        /// <returns>The Base36 alphabet string (0-9, A-Z).</returns>
        public override string ToString() => Base36Chars;

        /// <summary>
        /// Encodes a string using Base36 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The Base36 encoded string.</returns>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Convert string to bytes using specified encoding
            var bytes = GetBytes(input, encoding);
            return Encode(bytes);
        }

        /// <summary>
        /// Decodes a Base36 encoded string.
        /// </summary>
        /// <param name="input">The Base36 string to decode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The decoded original string.</returns>
        /// <exception cref="ArgumentException">Thrown for invalid Base36 characters.</exception>
        public static string Decode(string input, StringEncoding encoding = StringEncoding.UTF8)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Convert Base36 to bytes then to string using specified encoding
            var bytes = DecodeToBytes(input);
            return GetString(bytes, encoding);
        }

        /// <summary>
        /// Encodes a byte array to Base36 string.
        /// </summary>
        private static string Encode(byte[] input)
        {
            if (input == null || input.Length == 0) return string.Empty;

            // Calculate output length (overestimate to avoid reallocation)
            int outputLength = CalculateEncodedLength(input);
            char[]? array = null;

            try
            {
                // Use stack allocation for small buffers, pool for large ones
                Span<char> buffer = outputLength <= 1024
                    ? stackalloc char[outputLength]
                    : (array = ArrayPool<char>.Shared.Rent(outputLength));

                fixed (byte* bytesPtr = input)
                fixed (char* resultPtr = &MemoryMarshal.GetReference(buffer))
                {
                    // Perform the actual encoding
                    int actualLength = InternalEncode(bytesPtr, input.Length, resultPtr);
                    return new string(resultPtr, 0, actualLength);
                }
            }
            finally
            {
                // Return array to pool if we used one
                if (array != null)
                    ArrayPool<char>.Shared.Return(array);
            }
        }

        /// <summary>
        /// Decodes a Base36 string to byte array.
        /// </summary>
        private static byte[] DecodeToBytes(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<byte>();

            fixed (char* charsPtr = input)
            {
                return InternalDecode(charsPtr, input.Length);
            }
        }

        /// <summary>
        /// Creates a dynamic method for fast byte array reversal.
        /// </summary>
        private static DynamicMethod CreateReverseBytesMethod()
        {
            var dm = new DynamicMethod(
                "ReverseBytes",
                typeof(void),
                new[] { typeof(byte*), typeof(int) },
                typeof(Base36).Module,
                skipVisibility: true);

            var il = dm.GetILGenerator();

            // Local variables
            il.DeclareLocal(typeof(int)); // i
            il.DeclareLocal(typeof(byte)); // temp

            // Loop labels
            Label loop = il.DefineLabel();
            Label check = il.DefineLabel();

            // Initialize loop counter (i = 0)
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Br_S, check);

            // Loop body
            il.MarkLabel(loop);
            // temp = bytes[i]
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Stloc_1);

            // bytes[i] = bytes[length - i - 1]
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldind_U1);
            il.Emit(OpCodes.Stind_I1);

            // bytes[length - i - 1] = temp
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Sub);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Ldloc_1);
            il.Emit(OpCodes.Stind_I1);

            // i++
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc_0);

            // Loop condition check
            il.MarkLabel(check);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldc_I4_2);
            il.Emit(OpCodes.Div);
            il.Emit(OpCodes.Blt_S, loop);

            il.Emit(OpCodes.Ret);

            return dm;
        }

        // Delegate for fast byte reversal
        private delegate void ReverseBytesDelegate(byte* bytes, int length);

        /// <summary>
        /// Reverses a byte array in place.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ReverseBytes(byte* bytes, int length)
        {
            for (int i = 0; i < length / 2; i++)
            {
                (bytes[length - i - 1], bytes[i]) = (bytes[i], bytes[length - i - 1]);
            }
        }

        /// <summary>
        /// Internal Base36 encoding implementation.
        /// </summary>
        private static int InternalEncode(byte* bytesPtr, int length, char* result)
        {
            // Create working buffer on stack
            Span<byte> buffer = stackalloc byte[length];
            new ReadOnlySpan<byte>(bytesPtr, length).CopyTo(buffer);

            int index = 0;

            while (!buffer.IsEmpty)
            {
                int remainder = 0;

                // Divide buffer by Base (36) and get remainder
                for (int i = 0; i < buffer.Length; i++)
                {
                    int temp = (remainder << 8) | buffer[i];
                    buffer[i] = (byte)(temp / Base);
                    remainder = temp % Base;
                }

                // Map remainder to Base36 character
                result[index++] = Base36Chars[remainder];

                // Trim leading zeros
                int trim = 0;
                while (trim < buffer.Length && buffer[trim] == 0)
                    trim++;

                buffer = buffer[trim..];
            }

            // Validate generated characters
            for (int i = 0; i < index; i++)
            {
                if (!IsValidBase36Char(result[i]))
                    throw new InvalidOperationException("Generated invalid character");
            }

            // Reverse the result (we generated digits from least to most significant)
            Reverse(result, index);

            return index;
        }

        /// <summary>
        /// Validates if a character is in the Base36 alphabet.
        /// </summary>
        private static bool IsValidBase36Char(char c) =>
            (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');

        /// <summary>
        /// Internal Base36 decoding implementation.
        /// </summary>
        private static byte[] InternalDecode(char* charsPtr, int length)
        {
            // Use stack allocation for initial buffer
            Span<byte> result = stackalloc byte[64];
            int resultIndex = 0;

            for (int i = 0; i < length; i++)
            {
                char c = charsPtr[i];

                // Validate character is in Base36 alphabet
                if (c >= _reverseBase36Map.Length || (c = char.ToUpperInvariant(c)) >= _reverseBase36Map.Length)
                {
                    throw new ArgumentException($"Invalid Base36 character: '{c}'");
                }

                int value = _reverseBase36Map[c];
                if (value < 0) throw new ArgumentException($"Invalid Base36 character: '{c}'");

                // Multiply current result by Base and add new value
                int carry = value;
                for (int j = 0; j < resultIndex; j++)
                {
                    carry += result[j] * Base;
                    result[j] = (byte)(carry & 0xFF);
                    carry >>= 8;
                }

                // Handle remaining carry
                while (carry > 0)
                {
                    // Grow buffer if needed
                    if (resultIndex >= result.Length)
                    {
                        Span<byte> newBuffer = stackalloc byte[result.Length * 2];
                        result.CopyTo(newBuffer);
                        result = newBuffer;
                    }

                    if (resultIndex >= result.Length)
                        throw new InvalidOperationException("Buffer growth failed");

                    result[resultIndex++] = (byte)(carry & 0xFF);
                    carry >>= 8;
                }
            }

            if (resultIndex == 0)
                return Array.Empty<byte>();

            // Reverse bytes (we generated them least significant byte first)
            ReverseBytes((byte*)Unsafe.AsPointer(ref result.GetPinnableReference()), resultIndex);

            // Trim leading zeros
            int firstNonZero = 0;
            while (firstNonZero < resultIndex && result[firstNonZero] == 0)
                firstNonZero++;

            int finalLength = resultIndex - firstNonZero;
            if (finalLength == 0)
                return Array.Empty<byte>();

            // Copy to final array
            byte[] output = new byte[finalLength];
            result.Slice(firstNonZero, finalLength).CopyTo(output);
            return output;
        }

        /// <summary>
        /// Grows a buffer by doubling its size.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> GrowBuffer(ref Span<byte> buffer)
        {
            var newBuffer = new Span<byte>(new byte[buffer.Length * 2]);
            buffer.CopyTo(newBuffer);
            buffer = newBuffer;
            return buffer;
        }

        /// <summary>
        /// Gets the specified text encoding.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Encoding GetEncoding(StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8,
                StringEncoding.UTF16LE => Encoding.Unicode,
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode,
                StringEncoding.UTF32 => Encoding.UTF32,
                StringEncoding.UTF7 => Encoding.UTF7,
                StringEncoding.ASCII => Encoding.ASCII,
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1,
#endif
                _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
            };
        }

        /// <summary>
        /// Converts string to bytes using specified encoding.
        /// </summary>
        private static byte[] GetBytes(string input, StringEncoding encoding)
        {
            var enc = GetEncoding(encoding);
            return enc.GetBytes(input);
        }

        /// <summary>
        /// Converts bytes to string using specified encoding.
        /// </summary>
        private static string GetString(byte[] bytes, StringEncoding encoding)
        {
            var enc = GetEncoding(encoding);
            return enc.GetString(bytes);
        }

        /// <summary>
        /// Calculates the maximum possible length of Base36 encoded output.
        /// </summary>
        private static int CalculateEncodedLength(byte[] input)
        {
            int bits = input.Length * 8;
            return (int)Math.Ceiling(bits / Math.Log2(Base)) + 1;
        }

        /// <summary>
        /// Reverses a character array in place.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Reverse(char* arr, int length)
        {
            for (int i = 0; i < length / 2; i++)
            {
                (arr[length - i - 1], arr[i]) = (arr[i], arr[length - i - 1]);
            }
        }
    }

    /// <summary>
    /// Provides extension methods for Base36 encoding/decoding on strings.
    /// </summary>
    public static class Base36Extension
    {
        /// <summary>
        /// Encodes a string using Base36 encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The Base36 encoded string.</returns>
        public static string EncodeBase36(this string input, StringEncoding encoding = StringEncoding.UTF8) =>
            Base36.Encode(input, encoding);

        /// <summary>
        /// Decodes a Base36 encoded string.
        /// </summary>
        /// <param name="input">The Base36 string to decode.</param>
        /// <param name="encoding">The text encoding to use (default: UTF8).</param>
        /// <returns>The decoded original string.</returns>
        public static string DecodeBase36(this string input, StringEncoding encoding = StringEncoding.UTF8) =>
            Base36.Decode(input, encoding);
    }
}
#endif
