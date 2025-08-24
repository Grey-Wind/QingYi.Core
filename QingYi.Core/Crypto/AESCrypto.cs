#if !BROWSER
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace QingYi.Core.Crypto
{
    /// <summary>
    /// Provides AES (Advanced Encryption Standard) encryption and decryption functionality
    /// </summary>
    /// <remarks>
    /// Implements the ICrypto interface for symmetric encryption using AES algorithm.
    /// Supports various cipher modes (CBC by default) and padding schemes (PKCS7 by default).
    /// </remarks>
    public class AESCrypto : ICrypto
    {
#if NET6_0_OR_GREATER
        private readonly Aes _aesProvider = Aes.Create();
#else
        private readonly AesManaged _aesProvider = new AesManaged();
#endif

        /// <summary>
        /// Gets or sets the encryption key (must be 16, 24 or 32 bytes)
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when key length is invalid</exception>
        public byte[] Key
        {
            get => _aesProvider.Key;
            set
            {
                // Validate AES key length (128/192/256 bits)
                if (value == null || (value.Length != 16 && value.Length != 24 && value.Length != 32))
                    throw new ArgumentException("AES key must be 16, 24 or 32 bytes (128, 192, 256 bits)");
                _aesProvider.Key = value;
            }
        }

        /// <summary>
        /// Gets or sets the initialization vector (must be 16 bytes)
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when IV length is invalid</exception>
        public byte[] IV
        {
            get => _aesProvider.IV;
            set
            {
                // Validate IV length (16 bytes)
                if (value == null || value.Length != 16)
                    throw new ArgumentException("IV must be 16 bytes");
                _aesProvider.IV = value;
            }
        }

        /// <summary>
        /// Initializes a new instance with default CBC mode and PKCS7 padding
        /// </summary>
        public AESCrypto() : this(CipherMode.CBC, PaddingMode.PKCS7) { }

        /// <summary>
        /// Initializes a new instance with specified cipher mode and padding mode
        /// </summary>
        /// <param name="cipherMode">Symmetric algorithm cipher mode</param>
        /// <param name="paddingMode">Symmetric algorithm padding mode</param>
        public AESCrypto(CipherMode cipherMode, PaddingMode paddingMode)
        {
            _aesProvider.Mode = cipherMode;
            _aesProvider.Padding = paddingMode;
            GenerateKeyIV();
        }

        /// <summary>
        /// Generates a new random key and initialization vector
        /// </summary>
        public void GenerateKeyIV()
        {
            _aesProvider.GenerateKey();
            _aesProvider.GenerateIV();
        }

        /// <summary>
        /// Encrypts plain data using AES algorithm
        /// </summary>
        /// <param name="plainData">Data to encrypt</param>
        /// <returns>Encrypted data as byte array</returns>
        public byte[] Encrypt(byte[] plainData)
        {
            if (plainData == null || plainData.Length == 0)
                return Array.Empty<byte>();

            using (var encryptor = _aesProvider.CreateEncryptor())
            using (var ms = new MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    cs.Write(plainData, 0, plainData.Length);
                    cs.FlushFinalBlock();
                }
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Decrypts encrypted data using AES algorithm
        /// </summary>
        /// <param name="encryptedData">Data to decrypt</param>
        /// <returns>Decrypted data as byte array</returns>
        public byte[] Decrypt(byte[] encryptedData)
        {
            if (encryptedData == null || encryptedData.Length == 0)
                return Array.Empty<byte>();

            using (var decryptor = _aesProvider.CreateDecryptor())
            using (var ms = new MemoryStream(encryptedData))
            {
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (var result = new MemoryStream())
                    {
                        cs.CopyTo(result);
                        return result.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Encrypts data from input stream and writes to output stream
        /// </summary>
        /// <param name="input">Input stream containing plain data</param>
        /// <param name="output">Output stream for encrypted data</param>
        public void Encrypt(Stream input, Stream output)
        {
            using (var encryptor = _aesProvider.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
            {
                input.CopyTo(cryptoStream);
                cryptoStream.FlushFinalBlock();
            }
        }

        /// <summary>
        /// Decrypts data from input stream and writes to output stream
        /// </summary>
        /// <param name="input">Input stream containing encrypted data</param>
        /// <param name="output">Output stream for decrypted data</param>
        public void Decrypt(Stream input, Stream output)
        {
            using (var decryptor = _aesProvider.CreateDecryptor())
            using (var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
            {
                cryptoStream.CopyTo(output);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        /// <summary>
        /// Encrypts data from ReadOnlySpan using AES algorithm
        /// </summary>
        /// <param name="source">Data to encrypt as ReadOnlySpan</param>
        /// <returns>Encrypted data as byte array</returns>
        public byte[] Encrypt(ReadOnlySpan<byte> source)
        {
            using (var encryptor = _aesProvider.CreateEncryptor())
            {
                return encryptor.TransformFinalBlock(source.ToArray(), 0, source.Length);
            }
        }

        /// <summary>
        /// Decrypts data from ReadOnlySpan using AES algorithm
        /// </summary>
        /// <param name="source">Data to decrypt as ReadOnlySpan</param>
        /// <returns>Decrypted data as byte array</returns>
        public byte[] Decrypt(ReadOnlySpan<byte> source)
        {
            using (var decryptor = _aesProvider.CreateDecryptor())
            {
                return decryptor.TransformFinalBlock(source.ToArray(), 0, source.Length);
            }
        }
#endif

        /// <summary>
        /// Releases all resources used by the AESCrypto instance
        /// </summary>
        public void Dispose()
        {
            _aesProvider?.Dispose();
        }
    }

    /// <summary>
    /// Provides static helper methods for AES encryption/decryption
    /// </summary>
    public static class AESCryptoHelper
    {
        #region Static Methods - Byte Array Operations

        /// <summary>
        /// Encrypts data using specified key and IV
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <returns>Encrypted data as byte array</returns>
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = new AESCrypto())
            {
                aes.Key = key;
                aes.IV = iv;
                return aes.Encrypt(data);
            }
        }

        /// <summary>
        /// Decrypts data using specified key and IV
        /// </summary>
        /// <param name="data">Data to decrypt</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <returns>Decrypted data as byte array</returns>
        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = new AESCrypto())
            {
                aes.Key = key;
                aes.IV = iv;
                return aes.Decrypt(data);
            }
        }
        #endregion

        #region Static Methods - Stream Operations

        /// <summary>
        /// Encrypts data from input stream to output stream using specified key and IV
        /// </summary>
        /// <param name="input">Input stream containing plain data</param>
        /// <param name="output">Output stream for encrypted data</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        public static void Encrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            using (var aes = new AESCrypto())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Encrypt(input, output);
            }
        }

        /// <summary>
        /// Decrypts data from input stream to output stream using specified key and IV
        /// </summary>
        /// <param name="input">Input stream containing encrypted data</param>
        /// <param name="output">Output stream for decrypted data</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        public static void Decrypt(Stream input, Stream output, byte[] key, byte[] iv)
        {
            using (var aes = new AESCrypto())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Decrypt(input, output);
            }
        }
        #endregion

        #region Static Methods - Span Operations
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Encrypts data from ReadOnlySpan using specified key and IV
        /// </summary>
        /// <param name="data">Data to encrypt as ReadOnlySpan</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <returns>Encrypted data as byte array</returns>
        public static byte[] Encrypt(ReadOnlySpan<byte> data, byte[] key, byte[] iv)
        {
            using (var aes = new AESCrypto())
            {
                aes.Key = key;
                aes.IV = iv;
                return aes.Encrypt(data);
            }
        }

        /// <summary>
        /// Decrypts data from ReadOnlySpan using specified key and IV
        /// </summary>
        /// <param name="data">Data to decrypt as ReadOnlySpan</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <returns>Decrypted data as byte array</returns>
        public static byte[] Decrypt(ReadOnlySpan<byte> data, byte[] key, byte[] iv)
        {
            using (var aes = new AESCrypto())
            {
                aes.Key = key;
                aes.IV = iv;
                return aes.Decrypt(data);
            }
        }
#endif
        #endregion

        #region Static Methods - String Operations

        /// <summary>
        /// Encrypts data using specified key and IV
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="key">Encryption key</param>
        /// <param name="iv">Initialization vector</param>
        /// <returns>Encrypted data as byte array</returns>
        public static byte[] Encrypt(byte[] data, string key, string iv)
        {
            byte[] keyB = FormattingKeyIV(key, 32); // 32 字节密钥对应 AES-256
            byte[] ivB = FormattingKeyIV(iv, 16);   // 16 字节 IV 对应 AES

            using (var aes = new AESCrypto())
            {
                aes.Key = keyB;
                aes.IV = ivB;
                return aes.Encrypt(data);
            }
        }

        /// <summary>
        /// Decrypts data using specified key and IV
        /// </summary>
        /// <param name="data">Data to decrypt</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <returns>Decrypted data as byte array</returns>
        public static byte[] Decrypt(byte[] data, string key, string iv)
        {
            byte[] keyB = FormattingKeyIV(key, 32); // 32 字节密钥对应 AES-256
            byte[] ivB = FormattingKeyIV(iv, 16);   // 16 字节 IV 对应 AES

            using (var aes = new AESCrypto())
            {
                aes.Key = keyB;
                aes.IV = ivB;
                return aes.Decrypt(data);
            }
        }

        /// <summary>
        /// Encrypts data using specified key and IV
        /// </summary>
        /// <param name="data">Data to encrypt</param>
        /// <param name="key">Encryption key</param>
        /// <param name="iv">Initialization vector</param>
        /// <returns>Encrypted data as byte array</returns>
        public static byte[] Encrypt(string data, string key, string iv)
        {
            byte[] keyB = FormattingKeyIV(key, 32); // 32 字节密钥对应 AES-256
            byte[] ivB = FormattingKeyIV(iv, 16);   // 16 字节 IV 对应 AES
            byte[] dataB = Encoding.UTF8.GetBytes(data);

            using (var aes = new AESCrypto())
            {
                aes.Key = keyB;
                aes.IV = ivB;
                return aes.Encrypt(dataB);
            }
        }

        /// <summary>
        /// Decrypts data using specified key and IV
        /// </summary>
        /// <param name="data">Data to decrypt</param>
        /// <param name="key">Encryption key (16/24/32 bytes)</param>
        /// <param name="iv">Initialization vector (16 bytes)</param>
        /// <returns>Decrypted data as byte array</returns>
        public static string Decrypt(string data, string key, string iv)
        {
            byte[] keyB = FormattingKeyIV(key, 32); // 32 字节密钥对应 AES-256
            byte[] ivB = FormattingKeyIV(iv, 16);   // 16 字节 IV 对应 AES
            byte[] dataB = Encoding.UTF8.GetBytes(data);

            using (var aes = new AESCrypto())
            {
                aes.Key = keyB;
                aes.IV = ivB;
                return Encoding.UTF8.GetString(aes.Decrypt(dataB));
            }
        }
        #endregion

        /// <summary>
        /// Converts a hexadecimal string to a byte array.<br></br>
        /// 将十六进制字符串转换为字节数组。
        /// </summary>
        /// <param name="hex">
        /// The hexadecimal string to convert.<br></br>
        /// 要转换的十六进制字符串。
        /// </param>
        /// <param name="expectedLength">
        /// The desired byte length.<br></br>
        /// 所需的字节长度。
        /// </param>
        /// <returns>
        /// The corresponding byte array.<br></br>
        /// 对应的字节数组。
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the length of the <paramref name="hex"/> string is not an even number.<br></br>
        /// 当<paramref name="hex"/>字符串的长度不是偶数时抛出。
        /// </exception>
        private static byte[] FormattingKeyIV(string hex, int expectedLength)
        {
            if (string.IsNullOrWhiteSpace(hex))
            {
                throw new ArgumentException("Hex string cannot be null or empty.", nameof(hex));
            }

            hex = Regex.Replace(hex, @"[^0-9A-Fa-f]", string.Empty);

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex string must have an even length.", nameof(hex));
            }

            if (hex.Length > expectedLength * 2)
            {
                hex = hex.Substring(0, expectedLength * 2);
            }
            else if (hex.Length < expectedLength * 2)
            {
                hex = hex.PadLeft(expectedLength * 2, '0');
            }

            byte[] bytes = new byte[expectedLength];

            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }

            return bytes;
        }
    }
}
#endif
