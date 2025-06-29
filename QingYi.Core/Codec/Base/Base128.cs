#if NET5_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    using System;
    using System.Text;

    /// <summary>
    /// Provides Base128 encoding and decoding functionality.
    /// </summary>
    /// <remarks>
    /// Base128 is a binary-to-binary encoding scheme that efficiently packs 7-bit chunks
    /// into bytes, with the last byte indicating padding information.
    /// This is different from text-based encodings like Base64 as it produces binary output.
    /// </remarks>
    public static class Base128
    {
        /// <summary>
        /// Encodes binary data into Base128 format.
        /// </summary>
        /// <param name="input">The byte array to encode.</param>
        /// <returns>
        /// A byte array in Base128 format where each byte contains 7 bits of original data,
        /// with the last byte indicating the number of padding bits (0-6).
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public static byte[] Encode(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (input.Length == 0)
                return new byte[1] { 0 }; // Only padding information

            int inputBitCount = input.Length * 8;
            int numChunks = (inputBitCount + 6) / 7; // Round up
            byte[] output = new byte[numChunks + 1]; // Last byte stores padding bits

            ulong bitBuffer = 0;
            int bufferLength = 0;
            int paddingBits = 0;

            unsafe
            {
                fixed (byte* pInput = input)
                fixed (byte* pOutput = output)
                {
                    byte* pIn = pInput;
                    byte* pInEnd = pIn + input.Length;
                    byte* pOut = pOutput;

                    for (int i = 0; i < numChunks; i++)
                    {
                        if (bufferLength < 7)
                        {
                            if (pIn < pInEnd)
                            {
                                bitBuffer = (bitBuffer << 8) | *pIn++;
                                bufferLength += 8;
                            }
                            else
                            {
                                // End of input, pad with zero bits
                                paddingBits = 7 - bufferLength;
                                bitBuffer <<= paddingBits;
                                bufferLength += paddingBits;
                            }
                        }

                        // Extract 7 bits
                        int shift = bufferLength - 7;
                        byte chunk = (byte)((bitBuffer >> shift) & 0x7F);
                        *pOut++ = chunk;

                        // Update buffer
                        bitBuffer &= (1UL << shift) - 1;
                        bufferLength -= 7;
                    }

                    // Store padding bits count
                    output[output.Length - 1] = (byte)paddingBits;
                }
            }

            return output;
        }

        /// <summary>
        /// Decodes Base128 encoded data back to its original form.
        /// </summary>
        /// <param name="encoded">The Base128 encoded byte array.</param>
        /// <returns>The decoded byte array.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown when input is null, empty, or contains invalid padding information.
        /// </exception>
        public static byte[] Decode(byte[] encoded)
        {
            if (encoded == null || encoded.Length < 1)
                throw new ArgumentException("Invalid encoded data.");

            int paddingBits = encoded[encoded.Length - 1];
            if (paddingBits < 0 || paddingBits > 6)
                throw new ArgumentException("Invalid padding bits.");

            int numChunks = encoded.Length - 1;

            // Handle empty input case
            if (numChunks == 0)
                return Array.Empty<byte>();

            int totalBits = numChunks * 7 - paddingBits;
            int outputLength = totalBits / 8;
            byte[] output = new byte[outputLength];

            ulong bitBuffer = 0;
            int bufferLength = 0;

            unsafe
            {
                fixed (byte* pEncoded = encoded)
                fixed (byte* pOutput = output)
                {
                    byte* pIn = pEncoded;
                    byte* pInEnd = pIn + numChunks;
                    byte* pOut = pOutput;
                    byte* pOutEnd = pOut + outputLength;

                    while (pIn < pInEnd)
                    {
                        // Add explicit cast to ulong
#pragma warning disable CS0675 // Bitwise-or operator used on a sign-extended operand
                        bitBuffer = (bitBuffer << 7) | (ulong)(*pIn++ & 0x7F);
#pragma warning restore CS0675 // Bitwise-or operator used on a sign-extended operand
                        bufferLength += 7;

                        // Extract complete bytes
                        while (bufferLength >= 8 && pOut < pOutEnd)
                        {
                            int shift = bufferLength - 8;
                            byte value = (byte)((bitBuffer >> shift) & 0xFF);
                            *pOut++ = value;
                            bitBuffer &= (1UL << shift) - 1;
                            bufferLength -= 8;
                        }
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Encodes a string into Base128 format using the specified text encoding.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="encoding">The text encoding to use for string-to-byte conversion.</param>
        /// <returns>Base128 encoded byte array.</returns>
        /// <exception cref="ArgumentNullException">Thrown when input is null.</exception>
        public static byte[] EncodeString(string input, StringEncoding encoding)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            Encoding encoder = GetEncoding(encoding);
            byte[] bytes = encoder.GetBytes(input);
            return Encode(bytes);
        }

        /// <summary>
        /// Decodes Base128 encoded data back to a string using the specified text encoding.
        /// </summary>
        /// <param name="encoded">The Base128 encoded byte array.</param>
        /// <param name="encoding">The text encoding to use for byte-to-string conversion.</param>
        /// <returns>The decoded string.</returns>
        public static string DecodeToString(byte[] encoded, StringEncoding encoding)
        {
            byte[] decodedBytes = Decode(encoded);
            Encoding decoder = GetEncoding(encoding);
            return decoder.GetString(decodedBytes);
        }

        /// <summary>
        /// Gets the System.Text.Encoding instance corresponding to the specified StringEncoding.
        /// </summary>
        /// <param name="encoding">The StringEncoding value to convert.</param>
        /// <returns>The corresponding Encoding instance.</returns>
        /// <exception cref="ArgumentException">Thrown when an unsupported encoding is specified.</exception>
        private static Encoding GetEncoding(StringEncoding encoding)
        {
            return encoding switch
            {
                StringEncoding.UTF8 => Encoding.UTF8,
                StringEncoding.UTF16LE => Encoding.Unicode,
                StringEncoding.UTF16BE => Encoding.BigEndianUnicode,
                StringEncoding.ASCII => Encoding.ASCII,
                StringEncoding.UTF32 => Encoding.UTF32,
#if NET6_0_OR_GREATER
                StringEncoding.Latin1 => Encoding.Latin1,
#endif
#pragma warning disable SYSLIB0001, CS0618
                StringEncoding.UTF7 => Encoding.UTF7,
#pragma warning restore SYSLIB0001, CS0618
                _ => throw new ArgumentException("Unsupported encoding.", nameof(encoding))
            };
        }
    }
}
#endif
