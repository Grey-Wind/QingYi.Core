#if !BROWSER
using QingYi.Core.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Crypto
{
    /// <summary>
    /// High-performance 3DES encryption/decryption implementation.
    /// Supports CBC, ECB, CFB, OFB, and CTS modes.
    /// Uses Unsafe operations and Span for memory optimization.
    /// </summary>
    public sealed class TripleDesCrypto : ICrypto, IDisposable
    {
#nullable enable
        /// <summary>
        /// The underlying TripleDES cryptographic algorithm instance.
        /// </summary>
        private readonly TripleDES _algorithm;

        /// <summary>
        /// The encryption/decryption key used by this instance.
        /// </summary>
        private byte[] _key;

        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Shared random number generator for IV and random byte generation.
        /// </summary>
        private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        #region Properties

        /// <summary>
        /// Gets the name of the algorithm ("TripleDES").
        /// </summary>
        public string AlgorithmName => "TripleDES";

        /// <summary>
        /// Gets the key size in bits.
        /// </summary>
        public int KeySize => _algorithm.KeySize;

        /// <summary>
        /// Gets the encryption/decryption key as a read-only memory region.
        /// </summary>
        public ReadOnlyMemory<byte> Key => new(_key);

        /// <summary>
        /// Gets a value indicating whether this algorithm supports Authenticated Encryption with Associated Data (AEAD).
        /// 3DES does not support AEAD.
        /// </summary>
        public bool IsAuthenticatedEncryption => false;

        /// <summary>
        /// Gets the cipher mode (e.g., CBC, ECB, CFB).
        /// </summary>
        public CipherMode Mode => _algorithm.Mode;

        /// <summary>
        /// Gets the padding mode (e.g., PKCS7, None).
        /// </summary>
        public PaddingMode Padding => _algorithm.Padding;

        /// <summary>
        /// Gets the block size in bytes.
        /// </summary>
        public int BlockSize => _algorithm.BlockSize / 8;

        /// <summary>
        /// Gets the authentication tag size in bytes.
        /// Returns 0 because 3DES does not support authentication tags.
        /// </summary>
        public int TagSizeInBytes => 0;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TripleDesCrypto"/> class with the specified key and mode.
        /// </summary>
        /// <param name="key">The encryption/decryption key. Must be 16 or 24 bytes (112 or 168 bits).</param>
        /// <param name="mode">The cipher mode (default is CBC).</param>
        /// <param name="padding">The padding mode (default is PKCS7).</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="key"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="key"/> length is not 16 or 24 bytes.</exception>
        public TripleDesCrypto(byte[] key, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (key.Length != 16 && key.Length != 24)
                throw new ArgumentException("3DES key must be 16 or 24 bytes (112 or 168 bits)", nameof(key));

            _key = new byte[key.Length];
            Buffer.BlockCopy(key, 0, _key, 0, key.Length);

            _algorithm = TripleDES.Create();
            _algorithm.Key = _key;
            _algorithm.Mode = mode;
            _algorithm.Padding = padding;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TripleDesCrypto"/> class with an auto-generated key.
        /// </summary>
        /// <param name="mode">The cipher mode (default is CBC).</param>
        /// <param name="padding">The padding mode (default is PKCS7).</param>
        public TripleDesCrypto(CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            _algorithm = TripleDES.Create();
            _algorithm.Mode = mode;
            _algorithm.Padding = padding;
            _algorithm.GenerateKey();
            _key = _algorithm.Key;
        }

        #endregion

        #region Core Encryption/Decryption Methods

        /// <summary>
        /// Encrypts the specified plaintext data using the provided IV.
        /// </summary>
        /// <param name="plaintext">The plaintext data to encrypt.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>The encrypted ciphertext.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public byte[] Encrypt(byte[] plaintext, byte[] iv, byte[]? associatedData = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");

            ValidateInput(plaintext, iv);

            using var encryptor = _algorithm.CreateEncryptor(_key, iv);
            return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        /// <summary>
        /// Decrypts the specified ciphertext data using the provided IV.
        /// </summary>
        /// <param name="ciphertext">The ciphertext data to decrypt.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <param name="authenticationTag">Authentication tag (not supported for 3DES).</param>
        /// <returns>The decrypted plaintext.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="ciphertext"/> or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> or <paramref name="authenticationTag"/> is provided (not supported).</exception>
        public byte[] Decrypt(byte[] ciphertext, byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");
            if (authenticationTag != null && authenticationTag.Length > 0)
                throw new NotSupportedException("3DES does not support Authentication Tag");

            ValidateInput(ciphertext, iv);

            using var decryptor = _algorithm.CreateDecryptor(_key, iv);
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        /// <summary>
        /// Encrypts the specified plaintext span using the provided IV and writes the result to the destination span.
        /// </summary>
        /// <param name="plaintext">The plaintext data to encrypt as a read-only span.</param>
        /// <param name="iv">The initialization vector (IV) as a read-only span. Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="destination">The span to write the encrypted ciphertext to.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="plaintext"/> is empty, <paramref name="iv"/> length is invalid, or <paramref name="destination"/> is too small.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination,
            out int bytesWritten, ReadOnlySpan<byte> associatedData = default)
        {
            if (!associatedData.IsEmpty)
                throw new NotSupportedException("3DES does not support Associated Data");

            ValidateSpanInput(plaintext, iv);

            int requiredSize = GetOutputSize(plaintext.Length, true);
            if (destination.Length < requiredSize)
                throw new ArgumentException($"Destination too small. Required: {requiredSize}, Actual: {destination.Length}");

            using var encryptor = _algorithm.CreateEncryptor(_key, iv.ToArray());
            bytesWritten = TransformSpan(encryptor, plaintext, destination);
        }

        /// <summary>
        /// Decrypts the specified ciphertext span using the provided IV and writes the result to the destination span.
        /// </summary>
        /// <param name="ciphertext">The ciphertext data to decrypt as a read-only span.</param>
        /// <param name="iv">The initialization vector (IV) as a read-only span. Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="destination">The span to write the decrypted plaintext to.</param>
        /// <param name="bytesWritten">When this method returns, contains the number of bytes written to <paramref name="destination"/>.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <param name="authenticationTag">Authentication tag (not supported for 3DES).</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="ciphertext"/> is empty, <paramref name="iv"/> length is invalid, or <paramref name="destination"/> is too small.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> or <paramref name="authenticationTag"/> is provided (not supported).</exception>
        public void Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination,
            out int bytesWritten, ReadOnlySpan<byte> associatedData = default,
            ReadOnlySpan<byte> authenticationTag = default)
        {
            if (!associatedData.IsEmpty)
                throw new NotSupportedException("3DES does not support Associated Data");
            if (!authenticationTag.IsEmpty)
                throw new NotSupportedException("3DES does not support Authentication Tag");

            ValidateSpanInput(ciphertext, iv);

            int requiredSize = GetOutputSize(ciphertext.Length, false);
            if (destination.Length < requiredSize)
                throw new ArgumentException($"Destination too small. Required: {requiredSize}, Actual: {destination.Length}");

            using var decryptor = _algorithm.CreateDecryptor(_key, iv.ToArray());
            bytesWritten = TransformSpan(decryptor, ciphertext, destination);
        }

        #endregion

        #region String Convenience Methods

        /// <summary>
        /// Encrypts the specified UTF-8 string and returns the ciphertext as a Base64-encoded string.
        /// </summary>
        /// <param name="plaintext">The plaintext string to encrypt.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>The Base64-encoded ciphertext string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public string EncryptToBase64(string plaintext, byte[] iv, byte[]? associatedData = null)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = Encrypt(plainBytes, iv, associatedData);
            return Convert.ToBase64String(cipherBytes);
        }

        /// <summary>
        /// Encrypts the specified UTF-8 string and returns the ciphertext as a hexadecimal string.
        /// </summary>
        /// <param name="plaintext">The plaintext string to encrypt.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>The hexadecimal ciphertext string (lowercase).</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public string EncryptToHex(string plaintext, byte[] iv, byte[]? associatedData = null)
        {
            var plainBytes = Encoding.UTF8.GetBytes(plaintext);
            var cipherBytes = Encrypt(plainBytes, iv, associatedData);
            return Convert.ToHexString(cipherBytes).ToLowerInvariant();
        }

        /// <summary>
        /// Decrypts the specified Base64-encoded ciphertext string and returns the original UTF-8 string.
        /// </summary>
        /// <param name="base64Ciphertext">The Base64-encoded ciphertext string.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <param name="tagBase64">Authentication tag (not supported for 3DES).</param>
        /// <returns>The decrypted plaintext string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Ciphertext"/> or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> or <paramref name="tagBase64"/> is provided (not supported).</exception>
        public string DecryptFromBase64(string base64Ciphertext, byte[] iv, byte[]? associatedData = null, byte[]? tagBase64 = null)
        {
            var cipherBytes = Convert.FromBase64String(base64Ciphertext);
            var plainBytes = Decrypt(cipherBytes, iv, associatedData, tagBase64);
            return Encoding.UTF8.GetString(plainBytes);
        }

        /// <summary>
        /// Decrypts the specified hexadecimal ciphertext string and returns the original UTF-8 string.
        /// </summary>
        /// <param name="hexCiphertext">The hexadecimal ciphertext string.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <param name="tagHex">Authentication tag as a hexadecimal string (not supported for 3DES).</param>
        /// <returns>The decrypted plaintext string.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="hexCiphertext"/> or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> or <paramref name="tagHex"/> is provided (not supported).</exception>
        public string DecryptFromHex(string hexCiphertext, byte[] iv, byte[]? associatedData = null, string? tagHex = null)
        {
            var cipherBytes = Convert.FromHexString(hexCiphertext);
            var plainBytes = Decrypt(cipherBytes, iv, associatedData,
                tagHex != null ? Convert.FromHexString(tagHex) : null);
            return Encoding.UTF8.GetString(plainBytes);
        }

        #endregion

        #region Streaming Methods

        /// <summary>
        /// Creates an encryptor transform for streaming encryption.
        /// </summary>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>An <see cref="ICryptoTransform"/> that can be used for encryption.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public ICryptoTransform CreateEncryptor(byte[] iv, byte[]? associatedData = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");

            ValidateIV(iv);
            return _algorithm.CreateEncryptor(_key, iv);
        }

        /// <summary>
        /// Creates a decryptor transform for streaming decryption.
        /// </summary>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <param name="authenticationTag">Authentication tag (not supported for 3DES).</param>
        /// <returns>An <see cref="ICryptoTransform"/> that can be used for decryption.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> or <paramref name="authenticationTag"/> is provided (not supported).</exception>
        public ICryptoTransform CreateDecryptor(byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");
            if (authenticationTag != null && authenticationTag.Length > 0)
                throw new NotSupportedException("3DES does not support Authentication Tag");

            ValidateIV(iv);
            return _algorithm.CreateDecryptor(_key, iv);
        }

        /// <summary>
        /// Asynchronously encrypts data from the source stream and writes it to the destination stream.
        /// </summary>
        /// <param name="plaintextStream">The stream containing the plaintext data to encrypt.</param>
        /// <param name="ciphertextStream">The stream to write the encrypted ciphertext to.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <param name="progress">An optional progress reporter to track the number of bytes processed.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous encryption operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if any required stream or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public async Task EncryptAsync(Stream plaintextStream, Stream ciphertextStream, byte[] iv,
            byte[]? associatedData = null, IProgress<long>? progress = null, CancellationToken ct = default)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");

            ValidateIV(iv);

            using var encryptor = _algorithm.CreateEncryptor(_key, iv);
            await ProcessStreamAsync(encryptor, plaintextStream, ciphertextStream, progress, ct);
        }

        /// <summary>
        /// Asynchronously decrypts data from the source stream and writes it to the destination stream.
        /// </summary>
        /// <param name="ciphertextStream">The stream containing the ciphertext data to decrypt.</param>
        /// <param name="plaintextStream">The stream to write the decrypted plaintext to.</param>
        /// <param name="iv">The initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <param name="authenticationTag">Authentication tag (not supported for 3DES).</param>
        /// <param name="progress">An optional progress reporter to track the number of bytes processed.</param>
        /// <param name="ct">A cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous decryption operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown if any required stream or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> or <paramref name="authenticationTag"/> is provided (not supported).</exception>
        public async Task DecryptAsync(Stream ciphertextStream, Stream plaintextStream, byte[] iv,
            byte[]? associatedData = null, byte[]? authenticationTag = null,
            IProgress<long>? progress = null, CancellationToken ct = default)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");
            if (authenticationTag != null && authenticationTag.Length > 0)
                throw new NotSupportedException("3DES does not support Authentication Tag");

            ValidateIV(iv);

            using var decryptor = _algorithm.CreateDecryptor(_key, iv);
            await ProcessStreamAsync(decryptor, ciphertextStream, plaintextStream, progress, ct);
        }

        #endregion

        #region Random Generation

        /// <summary>
        /// Generates a cryptographically random initialization vector (IV) of the appropriate size for this algorithm.
        /// </summary>
        /// <returns>A byte array containing the generated IV.</returns>
        public byte[] GenerateIV()
        {
            var iv = new byte[BlockSize];
            _rng.GetBytes(iv);
            return iv;
        }

        /// <summary>
        /// Generates a cryptographically random nonce. For 3DES, this is equivalent to <see cref="GenerateIV"/>.
        /// </summary>
        /// <returns>A byte array containing the generated nonce (IV).</returns>
        public byte[] GenerateNonce() => GenerateIV();

        /// <summary>
        /// Generates a cryptographically random sequence of bytes.
        /// </summary>
        /// <param name="byteCount">The number of random bytes to generate. Must be positive.</param>
        /// <returns>A byte array containing the generated random bytes.</returns>
        /// <exception cref="ArgumentException">Thrown if <paramref name="byteCount"/> is not positive.</exception>
        public byte[] GenerateRandomBytes(int byteCount)
        {
            if (byteCount <= 0)
                throw new ArgumentException("Byte count must be positive", nameof(byteCount));

            var bytes = new byte[byteCount];
            _rng.GetBytes(bytes);
            return bytes;
        }

        #endregion

        #region Business-Friendly Methods

        /// <summary>
        /// Encrypts the plaintext data, prepends a randomly generated IV to the ciphertext, and returns the combined result.
        /// </summary>
        /// <param name="plaintext">The plaintext data to encrypt.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>A byte array containing the IV (first <see cref="BlockSize"/> bytes) followed by the ciphertext.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> is null.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public byte[] EncryptWithPrefixIV(byte[] plaintext, byte[]? associatedData = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");

            var iv = GenerateIV();
            var ciphertext = Encrypt(plaintext, iv);

            var result = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);

            return result;
        }

        /// <summary>
        /// Decrypts data that has been encrypted with <see cref="EncryptWithPrefixIV"/>. The first <see cref="BlockSize"/> bytes are treated as the IV.
        /// </summary>
        /// <param name="combinedData">The combined data (IV + ciphertext).</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>The decrypted plaintext.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="combinedData"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="combinedData"/> is shorter than <see cref="BlockSize"/> bytes.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public byte[] DecryptWithPrefixIV(byte[] combinedData, byte[]? associatedData = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("3DES does not support Associated Data");

            if (combinedData.Length < BlockSize)
                throw new ArgumentException("Combined data too short", nameof(combinedData));

            var iv = new byte[BlockSize];
            var ciphertext = new byte[combinedData.Length - BlockSize];

            Buffer.BlockCopy(combinedData, 0, iv, 0, BlockSize);
            Buffer.BlockCopy(combinedData, BlockSize, ciphertext, 0, ciphertext.Length);

            return Decrypt(ciphertext, iv);
        }

        /// <summary>
        /// Encrypts the plaintext data with a prepended IV (using <see cref="EncryptWithPrefixIV"/>) and returns the combined result as a Base64 string.
        /// </summary>
        /// <param name="plaintext">The plaintext data to encrypt.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>A Base64-encoded string containing the IV and ciphertext.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="plaintext"/> is null.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public string EncryptWithPrefixIVToBase64(byte[] plaintext, byte[]? associatedData = null)
        {
            var combined = EncryptWithPrefixIV(plaintext, associatedData);
            return Convert.ToBase64String(combined);
        }

        /// <summary>
        /// Decrypts Base64-encoded data that has been encrypted with a prepended IV (using <see cref="EncryptWithPrefixIVToBase64"/>).
        /// </summary>
        /// <param name="base64Data">The Base64-encoded string containing the IV and ciphertext.</param>
        /// <param name="associatedData">Associated data (not supported for 3DES).</param>
        /// <returns>The decrypted plaintext.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="base64Data"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="base64Data"/> is not valid Base64 or decodes to insufficient data.</exception>
        /// <exception cref="NotSupportedException">Thrown if <paramref name="associatedData"/> is provided (not supported).</exception>
        public byte[] DecryptWithPrefixIVFromBase64(string base64Data, byte[]? associatedData = null)
        {
            var combined = Convert.FromBase64String(base64Data);
            return DecryptWithPrefixIV(combined, associatedData);
        }

        #endregion

        #region Legacy Methods

        /// <summary>
        /// [OBSOLETE] Encrypts data using ECB mode without an IV. ECB mode is insecure and should only be used for legacy compatibility.
        /// The instance must be configured with <see cref="CipherMode.ECB"/>.
        /// </summary>
        /// <param name="plaintext">The plaintext data to encrypt.</param>
        /// <returns>The encrypted ciphertext.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current mode is not ECB.</exception>
        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        public byte[] EncryptWithoutIV_ECB(byte[] plaintext)
        {
            if (_algorithm.Mode != CipherMode.ECB)
                throw new InvalidOperationException("Method only valid in ECB mode");

            using var encryptor = _algorithm.CreateEncryptor(_key, null);
            return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        /// <summary>
        /// [OBSOLETE] Decrypts data using ECB mode without an IV. ECB mode is insecure and should only be used for legacy compatibility.
        /// The instance must be configured with <see cref="CipherMode.ECB"/>.
        /// </summary>
        /// <param name="ciphertext">The ciphertext data to decrypt.</param>
        /// <returns>The decrypted plaintext.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the current mode is not ECB.</exception>
        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        public byte[] DecryptWithoutIV_ECB(byte[] ciphertext)
        {
            if (_algorithm.Mode != CipherMode.ECB)
                throw new InvalidOperationException("Method only valid in ECB mode");

            using var decryptor = _algorithm.CreateDecryptor(_key, null);
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Changes the cipher mode (e.g., CBC, ECB, CFB).
        /// </summary>
        /// <param name="mode">The new cipher mode.</param>
        public void ChangeMode(CipherMode mode)
        {
            _algorithm.Mode = mode;
        }

        /// <summary>
        /// Changes the padding mode (e.g., PKCS7, None).
        /// </summary>
        /// <param name="padding">The new padding mode.</param>
        public void ChangePadding(PaddingMode padding)
        {
            _algorithm.Padding = padding;
        }

        /// <summary>
        /// Changes the encryption/decryption key.
        /// </summary>
        /// <param name="newKey">The new key. Must be 16 or 24 bytes.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="newKey"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="newKey"/> length is not 16 or 24 bytes.</exception>
        public void ChangeKey(byte[] newKey)
        {
            ArgumentNullException.ThrowIfNull(newKey);
            if (newKey.Length != 16 && newKey.Length != 24)
                throw new ArgumentException("3DES key must be 16 or 24 bytes", nameof(newKey));

            ClearKey(); // Securely erase the old key
            _key = new byte[newKey.Length];
            Buffer.BlockCopy(newKey, 0, _key, 0, newKey.Length);
            _algorithm.Key = _key;
        }

        #endregion

        #region Performance Optimized Methods (Unsafe)

        /// <summary>
        /// High-performance encryption using unsafe pointers for direct memory access.
        /// </summary>
        /// <param name="plaintext">Pointer to the plaintext data.</param>
        /// <param name="plaintextLength">Length of the plaintext data in bytes.</param>
        /// <param name="iv">Pointer to the initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="destination">Pointer to the destination memory where the ciphertext will be written.</param>
        /// <exception cref="ArgumentNullException">Thrown if any pointer argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the IV length is invalid.</exception>
        public unsafe void EncryptUnsafe(byte* plaintext, int plaintextLength, byte* iv, byte* destination)
        {
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));
            if (iv == null) throw new ArgumentNullException(nameof(iv));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var encryptor = _algorithm.CreateEncryptor(_key, new Span<byte>(iv, BlockSize).ToArray());

            int inputOffset = 0;
            int outputOffset = 0;
            byte[] tempInput = ArrayPool<byte>.Shared.Rent(4096);
            byte[] tempOutput = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                while (inputOffset < plaintextLength)
                {
                    int chunkSize = Math.Min(4096, plaintextLength - inputOffset);

                    // Copy data from unmanaged to managed array
                    Unsafe.CopyBlock(ref tempInput[0], ref plaintext[inputOffset], (uint)chunkSize);

                    int transformed = encryptor.TransformBlock(tempInput, 0, chunkSize, tempOutput, 0);

                    // Copy transformed data back to unmanaged memory
                    fixed (byte* tempOutputPtr = tempOutput)
                    {
                        Unsafe.CopyBlock(ref destination[outputOffset], ref tempOutputPtr[0], (uint)transformed);
                    }

                    inputOffset += chunkSize;
                    outputOffset += transformed;
                }

                // Process final block
                byte[] finalBlock = encryptor.TransformFinalBlock(tempInput, 0, 0);
                if (finalBlock.Length > 0)
                {
                    fixed (byte* finalPtr = finalBlock)
                    {
                        Unsafe.CopyBlock(ref destination[outputOffset], ref finalPtr[0], (uint)finalBlock.Length);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tempInput);
                ArrayPool<byte>.Shared.Return(tempOutput);
            }
        }

        /// <summary>
        /// High-performance decryption using unsafe pointers for direct memory access.
        /// </summary>
        /// <param name="ciphertext">Pointer to the ciphertext data.</param>
        /// <param name="ciphertextLength">Length of the ciphertext data in bytes.</param>
        /// <param name="iv">Pointer to the initialization vector (IV). Must be exactly <see cref="BlockSize"/> bytes.</param>
        /// <param name="destination">Pointer to the destination memory where the plaintext will be written.</param>
        /// <exception cref="ArgumentNullException">Thrown if any pointer argument is null.</exception>
        /// <exception cref="ArgumentException">Thrown if the IV length is invalid.</exception>
        public unsafe void DecryptUnsafe(byte* ciphertext, int ciphertextLength, byte* iv, byte* destination)
        {
            if (ciphertext == null) throw new ArgumentNullException(nameof(ciphertext));
            if (iv == null) throw new ArgumentNullException(nameof(iv));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var decryptor = _algorithm.CreateDecryptor(_key, new Span<byte>(iv, BlockSize).ToArray());

            int inputOffset = 0;
            int outputOffset = 0;
            byte[] tempInput = ArrayPool<byte>.Shared.Rent(4096);
            byte[] tempOutput = ArrayPool<byte>.Shared.Rent(4096);

            try
            {
                while (inputOffset < ciphertextLength)
                {
                    int chunkSize = Math.Min(4096, ciphertextLength - inputOffset);

                    // Copy data from unmanaged to managed array
                    Unsafe.CopyBlock(ref tempInput[0], ref ciphertext[inputOffset], (uint)chunkSize);

                    int transformed = decryptor.TransformBlock(tempInput, 0, chunkSize, tempOutput, 0);

                    // Copy transformed data back to unmanaged memory
                    fixed (byte* tempOutputPtr = tempOutput)
                    {
                        Unsafe.CopyBlock(ref destination[outputOffset], ref tempOutputPtr[0], (uint)transformed);
                    }

                    inputOffset += chunkSize;
                    outputOffset += transformed;
                }

                // Process final block
                byte[] finalBlock = decryptor.TransformFinalBlock(tempInput, 0, 0);
                if (finalBlock.Length > 0)
                {
                    fixed (byte* finalPtr = finalBlock)
                    {
                        Unsafe.CopyBlock(ref destination[outputOffset], ref finalPtr[0], (uint)finalBlock.Length);
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(tempInput);
                ArrayPool<byte>.Shared.Return(tempOutput);
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculates the output size required for a given input size and operation (encryption or decryption).
        /// </summary>
        /// <param name="inputSize">The size of the input data in bytes.</param>
        /// <param name="encrypting">True for encryption, false for decryption.</param>
        /// <returns>The required output size in bytes.</returns>
        private int GetOutputSize(int inputSize, bool encrypting)
        {
            int blockSizeBytes = BlockSize;

            if (encrypting)
            {
                // Encryption may require padding
                if (_algorithm.Padding == PaddingMode.None)
                    return inputSize;

                // Calculate number of blocks after padding
                int fullBlocks = inputSize / blockSizeBytes;
                int remaining = inputSize % blockSizeBytes;
                int padding = (remaining == 0) ? blockSizeBytes : (blockSizeBytes - remaining);

                return inputSize + padding;
            }
            else
            {
                // Decryption output size is <= input size
                return inputSize;
            }
        }

        /// <summary>
        /// Transforms data using the specified <see cref="ICryptoTransform"/> and writes the result to the output span.
        /// Uses array pooling to reduce allocations.
        /// </summary>
        /// <param name="transform">The cryptographic transform to apply.</param>
        /// <param name="input">The input data as a read-only span.</param>
        /// <param name="output">The span to write the transformed data to.</param>
        /// <returns>The number of bytes written to <paramref name="output"/>.</returns>
        private static int TransformSpan(ICryptoTransform transform, ReadOnlySpan<byte> input, Span<byte> output)
        {
            // Use array pool to reduce allocations
            byte[] inputArray = ArrayPool<byte>.Shared.Rent(input.Length);
            byte[] outputArray = ArrayPool<byte>.Shared.Rent(output.Length);

            try
            {
                input.CopyTo(inputArray);

                int bytesWritten = transform.TransformBlock(inputArray, 0, input.Length, outputArray, 0);

                // Process final block
                byte[] finalBlock = transform.TransformFinalBlock(inputArray, 0, 0);
                if (finalBlock.Length > 0)
                {
                    finalBlock.CopyTo(outputArray.AsSpan(bytesWritten));
                    bytesWritten += finalBlock.Length;
                }

                outputArray.AsSpan(0, bytesWritten).CopyTo(output);
                return bytesWritten;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(inputArray);
                ArrayPool<byte>.Shared.Return(outputArray);
            }
        }

        /// <summary>
        /// Asynchronously processes a stream using the specified <see cref="ICryptoTransform"/>.
        /// </summary>
        /// <param name="transform">The cryptographic transform to apply.</param>
        /// <param name="inputStream">The stream to read input data from.</param>
        /// <param name="outputStream">The stream to write transformed data to.</param>
        /// <param name="progress">An optional progress reporter.</param>
        /// <param name="ct">A cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private static async Task ProcessStreamAsync(ICryptoTransform transform, Stream inputStream, Stream outputStream,
            IProgress<long>? progress, CancellationToken ct)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(81920); // 80KB buffer
            byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(81920);

            try
            {
                long totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = await inputStream.ReadAsync(buffer, ct)) > 0)
                {
                    int bytesTransformed = transform.TransformBlock(buffer, 0, bytesRead, outputBuffer, 0);
                    await outputStream.WriteAsync(outputBuffer.AsMemory(0, bytesTransformed), ct);

                    totalBytesRead += bytesRead;
                    progress?.Report(totalBytesRead);
                }

                // Process final block
                byte[] finalBlock = transform.TransformFinalBlock(buffer, 0, 0);
                if (finalBlock.Length > 0)
                {
                    await outputStream.WriteAsync(finalBlock, ct);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                ArrayPool<byte>.Shared.Return(outputBuffer);
            }
        }

        /// <summary>
        /// Validates the input data and IV for array-based operations.
        /// </summary>
        /// <param name="data">The data array to validate.</param>
        /// <param name="iv">The IV array to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="data"/> or <paramref name="iv"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is invalid.</exception>
        private void ValidateInput(byte[] data, byte[] iv)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(iv);
            ValidateIV(iv);
        }

        /// <summary>
        /// Validates the input data and IV for span-based operations.
        /// </summary>
        /// <param name="data">The data span to validate.</param>
        /// <param name="iv">The IV span to validate.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="data"/> is empty or <paramref name="iv"/> length is invalid.</exception>
        private void ValidateSpanInput(ReadOnlySpan<byte> data, ReadOnlySpan<byte> iv)
        {
            if (data.IsEmpty) throw new ArgumentException("Data cannot be empty", nameof(data));
            ValidateIV(iv);
        }

        /// <summary>
        /// Validates that the IV span has the correct length.
        /// </summary>
        /// <param name="iv">The IV span to validate.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="iv"/> length is not equal to <see cref="BlockSize"/>.</exception>
        private void ValidateIV(ReadOnlySpan<byte> iv)
        {
            if (iv.Length != BlockSize)
                throw new ArgumentException($"IV must be exactly {BlockSize} bytes for {AlgorithmName}");
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Releases all resources used by the current instance of <see cref="TripleDesCrypto"/>.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _algorithm.Dispose();
                ClearKey();
                _disposed = true;
            }
        }

        /// <summary>
        /// Securely clears the encryption/decryption key from memory.
        /// </summary>
        public void ClearKey()
        {
            if (_key != null)
            {
                Array.Clear(_key, 0, _key.Length);
                _key = null!;
            }
        }

        #endregion

        #region Static Factory Methods

        /// <summary>
        /// Creates a new <see cref="TripleDesCrypto"/> instance configured for CBC mode.
        /// </summary>
        /// <param name="key">The encryption/decryption key. Must be 16 or 24 bytes.</param>
        /// <param name="padding">The padding mode (default is PKCS7).</param>
        /// <returns>A new <see cref="TripleDesCrypto"/> instance configured for CBC mode.</returns>
        public static TripleDesCrypto CreateCbc(byte[] key, PaddingMode padding = PaddingMode.PKCS7)
        {
            return new TripleDesCrypto(key, CipherMode.CBC, padding);
        }

        /// <summary>
        /// Creates a new <see cref="TripleDesCrypto"/> instance configured for CFB mode.
        /// </summary>
        /// <param name="key">The encryption/decryption key. Must be 16 or 24 bytes.</param>
        /// <param name="padding">The padding mode (default is PKCS7).</param>
        /// <returns>A new <see cref="TripleDesCrypto"/> instance configured for CFB mode.</returns>
        public static TripleDesCrypto CreateCfb(byte[] key, PaddingMode padding = PaddingMode.PKCS7)
        {
            return new TripleDesCrypto(key, CipherMode.CFB, padding);
        }

        /// <summary>
        /// Creates a new <see cref="TripleDesCrypto"/> instance configured for OFB mode.
        /// </summary>
        /// <param name="key">The encryption/decryption key. Must be 16 or 24 bytes.</param>
        /// <param name="padding">The padding mode (default is PKCS7).</param>
        /// <returns>A new <see cref="TripleDesCrypto"/> instance configured for OFB mode.</returns>
        public static TripleDesCrypto CreateOfb(byte[] key, PaddingMode padding = PaddingMode.PKCS7)
        {
            return new TripleDesCrypto(key, CipherMode.OFB, padding);
        }

        /// <summary>
        /// [OBSOLETE] Creates a new <see cref="TripleDesCrypto"/> instance configured for ECB mode.
        /// ECB mode is insecure and should only be used for legacy compatibility.
        /// </summary>
        /// <param name="key">The encryption/decryption key. Must be 16 or 24 bytes.</param>
        /// <param name="padding">The padding mode (default is PKCS7).</param>
        /// <returns>A new <see cref="TripleDesCrypto"/> instance configured for ECB mode.</returns>
        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        public static TripleDesCrypto CreateEcb(byte[] key, PaddingMode padding = PaddingMode.PKCS7)
        {
            return new TripleDesCrypto(key, CipherMode.ECB, padding);
        }

        #endregion
#nullable restore
    }
}
#endif
