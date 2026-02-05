using QingYi.Core.Interfaces;
using System;
using System.Security.Cryptography;

namespace QingYi.Core.Crypto
{
    public class AesCrypto : ICrypto
    {
        #region 常量
        private const int AES_BLOCK_SIZE = 16;
        private const int AES_IV_SIZE = 16;
        private const int DEFAULT_TAG_SIZE = 16;

        private static readonly byte[] EmptyByteArray = [];
        #endregion

        /// <summary>
        /// Extended encryption mode.
        /// </summary>
        public enum ExtendedCipherMode
        {
            /// <summary>
            /// CBC (Cipher Block Chaining) is a widely-used encryption mode that enhances the security of block ciphers. It operates by combining each plaintext block with the previous ciphertext block before encryption, using an initialization vector (IV) for the first block. This chaining mechanism ensures that identical plaintext blocks produce different ciphertexts, making patterns harder to detect. CBC provides strong confidentiality but requires sequential processing and proper IV management to avoid vulnerabilities. It remains a fundamental choice for secure data transmission and storage in various applications.
            /// </summary>
            CBC,
            /// <summary>
            /// ECB (Electronic Codebook) mode is a basic and straightforward method for applying a block cipher to encrypt data. In this mode, the input plaintext is divided into fixed-size blocks, each of which is independently encrypted using the same secret key. This approach allows for parallel processing and random access to ciphertext blocks, making it operationally simple. However, ECB has a significant security weakness: identical plaintext blocks always produce identical ciphertext blocks when encrypted with the same key. As a result, patterns in the plaintext—such as repeated sequences or structured data—can remain visible in the ciphertext, potentially leaking information. Due to this limitation, ECB is generally unsuitable for encrypting large or sensitive datasets, and it is not recommended for use in modern cryptographic applications where stronger modes like CBC or GCM are preferred.
            /// </summary>
            ECB,
            /// <summary>
            /// CFB (Cipher Feedback) mode is a symmetric-key block cipher mode of operation that turns a block cipher into a self-synchronizing stream cipher. In CFB mode, the previous ciphertext block is encrypted using the key, and the result is XORed with the current plaintext block to produce ciphertext. This process creates a feedback loop where ciphertext depends on earlier encrypted data, making it suitable for encrypting streaming data (e.g., network communication) where data arrives in real-time. CFB allows for partial block processing and supports error propagation, meaning a transmission error in one ciphertext block affects subsequent decryption until synchronization is regained. However, like other feedback modes, it requires an initialization vector (IV) to ensure security and uniqueness.
            /// </summary>
            CFB,
            /// <summary>
            /// OFB (Output Feedback) mode is a symmetric-key block cipher mode of operation that transforms a block cipher into a synchronous stream cipher. In OFB mode, the encryption process does not directly apply the cipher to plaintext data. Instead, a keystream is generated independently by repeatedly encrypting an initialization vector (IV). This keystream is then XORed with the plaintext to produce ciphertext (or vice versa for decryption). The key feature of OFB is that any bit error in the ciphertext affects only the corresponding bit in the decrypted plaintext, making it suitable for environments where transmission errors may occur but propagation is undesirable. However, like other stream cipher modes, it requires a unique IV for each encryption to maintain security. OFB ensures confidentiality and allows preprocessing of keystream, but it does not provide authentication or integrity protection. It is commonly used in scenarios where error propagation must be avoided, such as in audio or video streaming over unreliable channels.
            /// </summary>
            OFB,
            /// <summary>
            /// CTS (Cypher Text Stealing) is a symmetric block cipher mode of operation designed to handle data of any length without requiring rigid padding to the block size. Unlike standard modes such as CBC (Cipher Block Chaining), which require the plaintext to be padded to a multiple of the block length, CTS efficiently processes the final partial block by "stealing" ciphertext from the previous block. This eliminates the need for extra padding bytes while maintaining security and integrity. CTS is particularly useful in scenarios where data length varies or where padding overhead is undesirable, such as in disk encryption or network protocols. It ensures full encryption of all input data without expansion, making it both efficient and secure for real-world applications.
            /// </summary>
            CTS,
            /// <summary>
            /// GCM (Galois/Counter Mode) is an efficient and secure authenticated encryption algorithm that combines the Counter (CTR) mode for data confidentiality with a Galois field-based authentication mechanism to ensure data integrity, all in a single pass over the data for high performance in modern applications like TLS and IPSec.
            /// </summary>
            GCM
        }

        #region 字段
        private readonly Aes _aes;
        private readonly AesGcm? _aesGcm;
        private byte[] _key;
        private bool _isGcmMode;
        private bool _disposed;
        private ExtendedCipherMode _extendedMode;
        #endregion

        #region attributes
        public string AlgorithmName => _isGcmMode ? "AES-GCM" : $"AES-{KeySize}-{_extendedMode}";
        public int KeySize => _key.Length * 8;
        public bool IsAuthenticatedEncryption => _isGcmMode;
        public CipherMode Mode
        {
            get
            {
                // 映射
                return _extendedMode switch
                {
                    ExtendedCipherMode.CBC => CipherMode.CBC,
                    ExtendedCipherMode.ECB => CipherMode.ECB,
                    ExtendedCipherMode.CFB => CipherMode.CFB,
                    ExtendedCipherMode.OFB => CipherMode.OFB,
                    ExtendedCipherMode.CTS => CipherMode.CTS,
                    ExtendedCipherMode.GCM => CipherMode.CBC,
                    _ => CipherMode.CBC
                };
            }
        }

        public PaddingMode Padding => _aes.Padding;
        public int BlockSize => AES_BLOCK_SIZE;
        public int TagSizeInBytes => _isGcmMode ? DEFAULT_TAG_SIZE : 0;
        #endregion
    }
}
