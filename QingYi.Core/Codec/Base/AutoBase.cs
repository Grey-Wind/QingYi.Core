#if !NET463
using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Provides methods for encoding and decoding data using a custom base conversion
    /// with a user-defined character set.
    /// </summary>
    /// <remarks>
    /// This implementation supports arbitrary base conversions using BigInteger
    /// for handling large numbers. The character set must contain unique characters
    /// to ensure unambiguous encoding/decoding.
    /// </remarks>
    public class AutoBase
    {
        private readonly string _characterSet;
        private readonly int _base;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoBase"/> class with the specified character set.
        /// </summary>
        /// <param name="characterSet">The set of characters to use for encoding/decoding. Must contain unique characters.</param>
        /// <exception cref="ArgumentException">
        /// Thrown when:
        /// <list type="bullet">
        /// <item><description>The character set has fewer than 2 characters</description></item>
        /// <item><description>The character set contains duplicate characters</description></item>
        /// </list>
        /// </exception>
        public AutoBase(string characterSet)
        {
            if (characterSet.Length < 2)
                throw new ArgumentException("Character set must have at least 2 characters.");
            if (characterSet.Distinct().Count() != characterSet.Length)
                throw new ArgumentException("Character set contains duplicate characters.");

            _characterSet = characterSet;
            _base = characterSet.Length;
        }

        /// <summary>
        /// Encodes a byte array into a string using the configured character set.
        /// </summary>
        /// <param name="data">The byte array to encode. Can be empty or null.</param>
        /// <returns>
        /// An encoded string representation of the input data using the configured character set.
        /// Returns an empty string if the input is null or empty.
        /// </returns>
        /// <remarks>
        /// The encoding process:
        /// <list type="number">
        /// <item><description>Converts bytes to a BigInteger (little-endian with sign handling)</description></item>
        /// <item><description>Performs base conversion using the character set</description></item>
        /// <item><description>Builds the output string from remainders</description></item>
        /// </list>
        /// </remarks>
        public string Encode(byte[] data)
        {
            if (data == null || data.Length == 0)
                return string.Empty;

            // Handle special case for single zero byte
            if (data.Length == 1 && data[0] == 0)
                return _characterSet[0].ToString();

            // Convert byte array to BigInteger (requires little-endian byte order)
            BigInteger number = new BigInteger(data.Reverse().ToArray());

            // Add leading zero byte if needed to ensure positive representation
            if (data[data.Length - 1] > 0x7F)
            {
                number = new BigInteger(data.Concat(new byte[] { 0 }).Reverse().ToArray());
            }

            // Convert to target base
            StringBuilder result = new StringBuilder();
            while (number > 0)
            {
                BigInteger remainder;
                number = BigInteger.DivRem(number, _base, out remainder);
                result.Insert(0, _characterSet[(int)remainder]);
            }

            return result.ToString();
        }

        /// <summary>
        /// Decodes an encoded string back to the original byte array.
        /// </summary>
        /// <param name="encoded">The encoded string to decode. Can be null or empty.</param>
        /// <returns>
        /// The decoded byte array. Returns an empty array if input is null or empty.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the input string contains characters not present in the character set.</exception>
        /// <remarks>
        /// The decoding process:
        /// <list type="number">
        /// <item><description>Converts characters back to a BigInteger</description></item>
        /// <item><description>Converts BigInteger to little-endian byte array</description></item>
        /// <item><description>Adjusts endianness and removes padding if necessary</description></item>
        /// </list>
        /// </remarks>
        public byte[] Decode(string encoded)
        {
            if (string.IsNullOrEmpty(encoded))
#if NET45 || NET451 || NET452
                return new byte[] { };
#else
                return Array.Empty<byte>();
#endif

            // Convert string back to BigInteger
            BigInteger number = 0;
            foreach (char c in encoded)
            {
                int value = _characterSet.IndexOf(c);
                if (value < 0)
                    throw new ArgumentException($"Invalid character '{c}' in input.");

                number = number * _base + value;
            }

            // Convert BigInteger to byte array
            byte[] bytes = number.ToByteArray();

            // Reverse byte order (BigInteger uses little-endian, we need big-endian)
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            // Remove possible leading zero byte
            if (bytes.Length > 1 && bytes[0] == 0)
            {
                bytes = bytes.Skip(1).ToArray();
            }

            return bytes;
        }
    }
}
#endif
