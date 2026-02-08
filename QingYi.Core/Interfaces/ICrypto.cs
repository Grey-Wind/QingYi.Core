using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Interfaces
{
    /// <summary>
    /// Unified abstraction interface for symmetric encryption algorithms (AES, 3DES, DES, SM4, ChaCha20, etc.)
    /// Supports multiple padding modes, various input/output formats, streaming processing, authenticated encryption, etc.
    /// </summary>
    public interface ICrypto : IDisposable
    {
#nullable enable
        /// <summary>
        /// Name of the current encryption algorithm (e.g. AES, AES-256-GCM, ChaCha20-Poly1305, TripleDES, SM4, etc.)
        /// </summary>
        string AlgorithmName { get; }

        /// <summary>
        /// Key size in bits (e.g. 128, 192, 256)
        /// </summary>
        int KeySize { get; }

        /// <summary>
        /// The key currently in use (read-only)
        /// </summary>
        ReadOnlyMemory<byte> Key { get; }

        /// <summary>
        /// Indicates whether the current mode supports authenticated encryption (AEAD modes such as GCM, CCM, ChaCha20-Poly1305)
        /// </summary>
        bool IsAuthenticatedEncryption { get; }

        /// <summary>
        /// Current cipher mode (CBC, ECB, CFB, OFB, CTS, GCM, CCM, etc.)
        /// </summary>
        CipherMode Mode { get; }

        /// <summary>
        /// Current padding mode (only effective in non-AEAD modes such as CBC, CFB, etc.)
        /// </summary>
        PaddingMode Padding { get; }

        /// <summary>
        /// Block size in bytes — typically 16 bytes for AES/SM4, 8 bytes for DES/3DES
        /// </summary>
        int BlockSize { get; }

        /// <summary>
        /// Length of the authentication tag in bytes (only meaningful in AEAD modes; common values: 12, 13, 14, 15, 16 bytes)
        /// </summary>
        int TagSizeInBytes { get; }

        // ───────────────────────────────────────────────
        // Core: byte[] → byte[]  (most common scenario)
        // ───────────────────────────────────────────────

        /// <summary>
        /// Encrypts the plaintext using the current key, IV, and optional associated data (for AEAD modes).
        /// </summary>
        /// <param name="plaintext">The data to encrypt</param>
        /// <param name="iv">Initialization vector / nonce (must match algorithm requirements)</param>
        /// <param name="associatedData">Optional additional authenticated data (AAD) — only used in AEAD modes</param>
        /// <returns>The encrypted ciphertext (without IV or tag)</returns>
        byte[] Encrypt(byte[] plaintext, byte[] iv, byte[]? associatedData = null);

        /// <summary>
        /// Decrypts the ciphertext using the current key, IV, optional associated data, and authentication tag (if AEAD).
        /// </summary>
        /// <param name="ciphertext">The encrypted data to decrypt</param>
        /// <param name="iv">Initialization vector / nonce used during encryption</param>
        /// <param name="associatedData">Optional additional authenticated data (must match encryption)</param>
        /// <param name="authenticationTag">Authentication tag (required for AEAD modes)</param>
        /// <returns>The decrypted plaintext</returns>
        byte[] Decrypt(byte[] ciphertext, byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null);

        // Buffer-friendly overloads (help reduce GC pressure)

        /// <summary>
        /// Encrypts data from source span into destination span (zero-allocation friendly).
        /// </summary>
        /// <param name="plaintext">Input data to encrypt</param>
        /// <param name="iv">Initialization vector / nonce</param>
        /// <param name="destination">Buffer to write the ciphertext into</param>
        /// <param name="bytesWritten">Number of bytes written to destination</param>
        /// <param name="associatedData">Optional AAD for AEAD modes</param>
        void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, ReadOnlySpan<byte> associatedData = default);

        /// <summary>
        /// Decrypts data from source span into destination span (zero-allocation friendly).
        /// </summary>
        /// <param name="ciphertext">Input ciphertext to decrypt</param>
        /// <param name="iv">Initialization vector / nonce</param>
        /// <param name="destination">Buffer to write the plaintext into</param>
        /// <param name="bytesWritten">Number of bytes written to destination</param>
        /// <param name="associatedData">Optional AAD used during encryption</param>
        /// <param name="authenticationTag">Authentication tag (required for AEAD)</param>
        void Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, ReadOnlySpan<byte> associatedData = default, ReadOnlySpan<byte> authenticationTag = default);

        // ───────────────────────────────────────────────
        // String convenience methods (default encoding: UTF-8)
        // ───────────────────────────────────────────────

        /// <summary>
        /// Encrypts a UTF-8 string and returns the result as Base64-encoded string.
        /// </summary>
        string EncryptToBase64(string plaintext, byte[] iv, byte[]? associatedData = null);

        /// <summary>
        /// Encrypts a UTF-8 string and returns the result as lowercase hex string.
        /// </summary>
        string EncryptToHex(string plaintext, byte[] iv, byte[]? associatedData = null);

        /// <summary>
        /// Decrypts Base64-encoded ciphertext back to the original UTF-8 string.
        /// </summary>
        string DecryptFromBase64(string base64Ciphertext, byte[] iv, byte[]? associatedData = null, byte[]? tagBase64 = null);

        /// <summary>
        /// Decrypts hex-encoded ciphertext back to the original UTF-8 string.
        /// </summary>
        string DecryptFromHex(string hexCiphertext, byte[] iv, byte[]? associatedData = null, string? tagHex = null);

        // ───────────────────────────────────────────────
        // Streaming / large file processing
        // ───────────────────────────────────────────────

        /// <summary>
        /// Creates a transform object for incremental / streaming encryption.
        /// </summary>
        ICryptoTransform CreateEncryptor(byte[] iv, byte[]? associatedData = null);

        /// <summary>
        /// Creates a transform object for incremental / streaming decryption.
        /// </summary>
        ICryptoTransform CreateDecryptor(byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null);

        /// <summary>
        /// Asynchronously encrypts data from one stream to another, with optional progress reporting.
        /// </summary>
        Task EncryptAsync(Stream plaintextStream, Stream ciphertextStream, byte[] iv,
            byte[]? associatedData = null, IProgress<long>? progress = null, CancellationToken ct = default);

        /// <summary>
        /// Asynchronously decrypts data from one stream to another, with optional progress reporting.
        /// </summary>
        Task DecryptAsync(Stream ciphertextStream, Stream plaintextStream, byte[] iv,
            byte[]? associatedData = null, byte[]? authenticationTag = null,
            IProgress<long>? progress = null, CancellationToken ct = default);

        // ───────────────────────────────────────────────
        // Random IV / Nonce / key material generation helpers
        // ───────────────────────────────────────────────

        /// <summary>
        /// Generates a cryptographically secure random IV suitable for the current algorithm.
        /// </summary>
        byte[] GenerateIV();

        /// <summary>
        /// Generates a cryptographically secure random nonce (preferred term for AEAD algorithms).
        /// </summary>
        byte[] GenerateNonce();

        /// <summary>
        /// Generates cryptographically secure random bytes of the requested length.
        /// </summary>
        byte[] GenerateRandomBytes(int byteCount);

        // ───────────────────────────────────────────────
        // Recommended business-friendly methods: prefix IV (+ tag when AEAD)
        // ───────────────────────────────────────────────

        /// <summary>
        /// Recommended encryption method: returns IV + ciphertext (+ tag when AEAD) concatenated.
        /// Safe and convenient format for most business applications.
        /// </summary>
        byte[] EncryptWithPrefixIV(byte[] plaintext, byte[]? associatedData = null);

        /// <summary>
        /// Counterpart to EncryptWithPrefixIV — parses IV (fixed length) + Ciphertext + (optional Tag).
        /// </summary>
        byte[] DecryptWithPrefixIV(byte[] combinedData, byte[]? associatedData = null);

        /// <summary>
        /// Encrypts and returns IV + ciphertext (+ tag) as Base64 string.
        /// </summary>
        string EncryptWithPrefixIVToBase64(byte[] plaintext, byte[]? associatedData = null);

        /// <summary>
        /// Decrypts data from Base64 string in the format produced by EncryptWithPrefixIVToBase64.
        /// </summary>
        byte[] DecryptWithPrefixIVFromBase64(string base64Data, byte[]? associatedData = null);

        // ───────────────────────────────────────────────
        // Legacy / special-case compatibility methods
        // ───────────────────────────────────────────────

        /// <summary>
        /// Encryption without IV — only valid in ECB mode (strongly discouraged due to security issues).
        /// </summary>
        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        byte[] EncryptWithoutIV_ECB(byte[] plaintext);

        /// <summary>
        /// Decryption without IV — only valid in ECB mode (strongly discouraged due to security issues).
        /// </summary>
        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        byte[] DecryptWithoutIV_ECB(byte[] ciphertext);

        // ───────────────────────────────────────────────
        // Cleanup & key material zeroization
        // ───────────────────────────────────────────────

#pragma warning disable CS1591 // 缺少对公共可见类型或成员的 XML 注释
        new void Dispose();
#pragma warning restore CS1591 // 缺少对公共可见类型或成员的 XML 注释

        /// <summary>
        /// Immediately zeroizes the key material in memory to reduce the risk of key exposure.
        /// Useful in high-security or forensic-resistant scenarios.
        /// </summary>
        void ClearKey();
#nullable restore
    }
}
