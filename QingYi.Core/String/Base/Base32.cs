using System;

namespace QingYi.Core.String.Base
{
    public static class Base32
    {
        public static string Encode(string input, StringEncoding encoding = StringEncoding.UTF8) => Base32RFC4648.Encode(input, encoding);

        public static string Decode(string input, StringEncoding encoding = StringEncoding.UTF8) => Base32RFC4648.Decode(input, encoding);

        [Flags]
        public enum Alphabet
        {
            RFC4648,
            Crockford,
            ExtendHex,
            GeoHash,
            WordSafe,
            zBase32,
        }
    }

    public static class Base32Extension
    {
        public static string EncodeBase32(this string input, Base32.Alphabet alphabet = Base32.Alphabet.RFC4648, StringEncoding encoding = StringEncoding.UTF8)
        {
            switch (alphabet)
            {
                case Base32.Alphabet.Crockford:
                    return Base32Crockford.Encode(input, encoding);
                case Base32.Alphabet.ExtendHex:
                    return Base32ExtendedHex.Encode(input, encoding);
                case Base32.Alphabet.GeoHash:
                    return Base32GeoHash.Encode(input, encoding);
                case Base32.Alphabet.WordSafe:
                    return Base32WordSafe.Encode(input, encoding);
                case Base32.Alphabet.zBase32:
                    return Base32z.Encode(input, encoding);
                case Base32.Alphabet.RFC4648:
                default:
                    return Base32.Encode(input, encoding);
            }
        }

        public static string DecodeBase32(this string input, Base32.Alphabet alphabet = Base32.Alphabet.RFC4648, StringEncoding encoding = StringEncoding.UTF8)
        {
            switch (alphabet)
            {
                case Base32.Alphabet.Crockford:
                    return Base32Crockford.Decode(input, encoding);
                case Base32.Alphabet.ExtendHex:
                    return Base32ExtendedHex.Decode(input, encoding);
                case Base32.Alphabet.GeoHash:
                    return Base32GeoHash.Decode(input, encoding);
                case Base32.Alphabet.WordSafe:
                    return Base32WordSafe.Decode(input, encoding);
                case Base32.Alphabet.zBase32:
                    return Base32z.Decode(input, encoding);
                case Base32.Alphabet.RFC4648:
                default:
                    return Base32.Decode(input, encoding);
            }
        }
    }
}
