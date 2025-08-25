using QingYi.Core.Codec.Base;
using System;

namespace QingYi.Core.Codec.Base
{
    /// <summary>
    /// Base32 codec library (Default alphabet is RFC4648).
    /// Provides encoding and decoding functionality for various Base32 alphabet variants.
    /// </summary>
    public class Base32
    {
        /// <summary>
        /// Encodes a string using Base32 encoding (RFC4648 by default).
        /// </summary>
        /// <param name="input">The string to be encoded.</param>
        /// <param name="encoding">The character encoding to use (default: UTF8).</param>
        /// <returns>The Base32 encoded string.</returns>
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8) => Base32RFC4648.Encode(input, encoding);

        /// <summary>
        /// Decodes a Base32 encoded string (RFC4648 by default).
        /// </summary>
        /// <param name="input">The Base32 encoded string to decode.</param>
        /// <param name="encoding">The character encoding to use (default: UTF8).</param>
        /// <returns>The decoded original string.</returns>
        public static string Decode(string input, StringEncoding encoding = StringEncoding.UTF8) => Base32RFC4648.Decode(input, encoding);

        /// <summary>
        /// Returns the character set used for RFC4648 Base32 encoding.
        /// </summary>
        /// <returns>The Base32 character set string.</returns>
        public override string ToString() => "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        /// <summary>
        /// Enumeration of available Base32 alphabet variants.
        /// </summary>
        [Flags]
        public enum Alphabet
        {
            /// <summary>
            /// RFC 4648 Base32 alphabet (ABCDEFGHIJKLMNOPQRSTUVWXYZ234567).
            /// The most widely used Base32 standard.
            /// </summary>
            RFC4648,

            /// <summary>
            /// Crockford Base32 alphabet (0123456789ABCDEFGHJKMNPQRSTVWXYZ).
            /// Designed for human readability and error prevention.
            /// </summary>
            Crockford,

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            /// <summary>
            /// Extended Hex Base32 alphabet (0123456789ABCDEFGHIJKLMNOPQRSTUV).
            /// Uses contiguous letters for hexadecimal-like representation.
            /// </summary>
            ExtendHex,

            /// <summary>
            /// GeoHash Base32 alphabet (0123456789bcdefghjkmnpqrstuvwxyz).
            /// Used in geohashing systems.
            /// </summary>
            GeoHash,

            /// <summary>
            /// WordSafe Base32 alphabet (23456789CFGHJMPQRVWXcfghjmpqrvwx).
            /// Avoids visually similar characters.
            /// </summary>
            WordSafe,
#endif

            /// <summary>
            /// zBase32 alphabet (ybndrfg8ejkmcpqxot1uwisza345h769).
            /// Optimized for human use and URL safety.
            /// </summary>
            zBase32,
        }
    }

    /// <summary>
    /// Provides extension methods for Base32 encoding/decoding on strings.
    /// </summary>
    public static class Base32Extension
    {
        /// <summary>
        /// Encodes a string using the specified Base32 alphabet variant.
        /// </summary>
        /// <param name="input">The string to encode.</param>
        /// <param name="alphabet">The Base32 alphabet variant to use (default: RFC4648).</param>
        /// <param name="encoding">The character encoding to use (default: UTF8).</param>
        /// <returns>The Base32 encoded string.</returns>
        public static string EncodeBase32(this string input, Base32.Alphabet alphabet = Base32.Alphabet.RFC4648, StringEncoding encoding = StringEncoding.UTF8)
        {
            switch (alphabet)
            {
                case Base32.Alphabet.Crockford:
                    return Base32Crockford.Encode(input, encoding);
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                case Base32.Alphabet.ExtendHex:
                    return Base32ExtendedHex.Encode(input, encoding);
                case Base32.Alphabet.GeoHash:
                    return Base32GeoHash.Encode(input, encoding);
                case Base32.Alphabet.WordSafe:
                    return Base32WordSafe.Encode(input, encoding);
#endif
                case Base32.Alphabet.zBase32:
                    return Base32z.Encode(input, encoding);
                case Base32.Alphabet.RFC4648:
                default:
                    return Base32.Encode(input, encoding);
            }
        }

        /// <summary>
        /// Decodes a Base32 string using the specified alphabet variant.
        /// </summary>
        /// <param name="input">The Base32 encoded string to decode.</param>
        /// <param name="alphabet">The Base32 alphabet variant used (default: RFC4648).</param>
        /// <param name="encoding">The character encoding to use (default: UTF8).</param>
        /// <returns>The decoded original string.</returns>
        public static string DecodeBase32(this string input, Base32.Alphabet alphabet = Base32.Alphabet.RFC4648, StringEncoding encoding = StringEncoding.UTF8)
        {
            switch (alphabet)
            {
                case Base32.Alphabet.Crockford:
                    return Base32Crockford.Decode(input, encoding);
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                case Base32.Alphabet.ExtendHex:
                    return Base32ExtendedHex.Decode(input, encoding);
                case Base32.Alphabet.GeoHash:
                    return Base32GeoHash.Decode(input, encoding);
                case Base32.Alphabet.WordSafe:
                    return Base32WordSafe.Decode(input, encoding);
#endif
                case Base32.Alphabet.zBase32:
                    return Base32z.Decode(input, encoding);
                case Base32.Alphabet.RFC4648:
                default:
                    return Base32.Decode(input, encoding);
            }
        }
    }
}
