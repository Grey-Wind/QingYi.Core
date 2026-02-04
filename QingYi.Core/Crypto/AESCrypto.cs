using QingYi.Core.Interfaces;
using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Crypto
{
    /// <summary>
    /// 高性能AES加解密实现，支持多种模式和填充方式
    /// </summary>
    public sealed unsafe class AesCrypto : ICrypto
    {
        #region 常量定义

        private const int AES_BLOCK_SIZE = 16; // AES块大小固定为16字节
        private const int AES_IV_SIZE = 16;    // AES IV大小固定为16字节
        private const int DEFAULT_TAG_SIZE = 16; // GCM模式默认标签大小

        private static readonly byte[] EmptyByteArray = Array.Empty<byte>();

        #endregion

        #region 自定义枚举和辅助类

        /// <summary>
        /// 扩展的加密模式，包含GCM等AEAD模式
        /// </summary>
        public enum ExtendedCipherMode
        {
            CBC,
            ECB,
            CFB,
            OFB,
            CTS,
            GCM
        }

        #endregion

        #region 字段

        private readonly Aes _aes;
        private readonly AesGcm? _aesGcm; // 用于GCM模式
        private byte[] _key;
        private bool _isGcmMode;
        private bool _disposed;
        private ExtendedCipherMode _extendedMode;

        // 用于内存池操作的缓冲区
        private GCHandle _keyHandle;
        private byte* _keyPtr;

        #endregion

        #region 属性

        public string AlgorithmName => _isGcmMode ? "AES-GCM" : $"AES-{KeySize}-{_extendedMode}";

        public int KeySize => _key.Length * 8;

        public ReadOnlyMemory<byte> Key => _key;

        public bool IsAuthenticatedEncryption => _isGcmMode;

        public CipherMode Mode
        {
            get
            {
                // 将扩展模式映射回标准CipherMode
                return _extendedMode switch
                {
                    ExtendedCipherMode.CBC => CipherMode.CBC,
                    ExtendedCipherMode.ECB => CipherMode.ECB,
                    ExtendedCipherMode.CFB => CipherMode.CFB,
                    ExtendedCipherMode.OFB => CipherMode.OFB,
                    ExtendedCipherMode.CTS => CipherMode.CTS,
                    ExtendedCipherMode.GCM => CipherMode.CBC, // GCM不是标准CipherMode，返回CBC作为占位
                    _ => CipherMode.CBC
                };
            }
        }

        public PaddingMode Padding => _aes.Padding;

        public int BlockSize => AES_BLOCK_SIZE;

        public int TagSizeInBytes => _isGcmMode ? DEFAULT_TAG_SIZE : 0;

        #endregion

        #region 构造函数和工厂方法

        /// <summary>
        /// 创建AES加密器实例
        /// </summary>
        /// <param name="key">密钥</param>
        /// <param name="mode">加密模式</param>
        /// <param name="padding">填充模式</param>
        public AesCrypto(byte[] key, ExtendedCipherMode mode = ExtendedCipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            ValidateKey(key);
            ValidateModeAndPadding(mode, padding);

            _key = new byte[key.Length];
            Buffer.BlockCopy(key, 0, _key, 0, key.Length);

            _keyHandle = GCHandle.Alloc(_key, GCHandleType.Pinned);
            _keyPtr = (byte*)_keyHandle.AddrOfPinnedObject();

            _extendedMode = mode;
            _isGcmMode = mode == ExtendedCipherMode.GCM;

            if (_isGcmMode)
            {
                _aesGcm = new AesGcm(key, DEFAULT_TAG_SIZE);
                _aes = Aes.Create(); // 仅用于属性访问
                _aes.Mode = CipherMode.ECB; // 不使用，仅占位
                _aes.Padding = PaddingMode.None; // GCM模式不使用填充
            }
            else
            {
                _aes = Aes.Create();
                _aes.Key = key;
                _aes.Mode = (CipherMode)mode; // 标准模式可以转换
                _aes.Padding = padding;
                _aes.BlockSize = 128; // AES固定为128位块
            }
        }

        /// <summary>
        /// 从Base64字符串创建AES加密器
        /// </summary>
        public static AesCrypto CreateFromBase64Key(string base64Key, ExtendedCipherMode mode = ExtendedCipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            byte[] key = Convert.FromBase64String(base64Key);
            return new AesCrypto(key, mode, padding);
        }

        /// <summary>
        /// 从Hex字符串创建AES加密器
        /// </summary>
        public static AesCrypto CreateFromHexKey(string hexKey, ExtendedCipherMode mode = ExtendedCipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            byte[] key = HexStringToBytes(hexKey);
            return new AesCrypto(key, mode, padding);
        }

        /// <summary>
        /// 生成随机密钥并创建AES加密器
        /// </summary>
        public static AesCrypto CreateRandom(int keySize = 256, ExtendedCipherMode mode = ExtendedCipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            byte[] key = new byte[keySize / 8];
            RandomNumberGenerator.Fill(key);
            return new AesCrypto(key, mode, padding);
        }

        #endregion

        #region 核心加解密方法

        public byte[] Encrypt(byte[] plaintext, byte[] iv, byte[]? associatedData = null)
        {
            ThrowIfDisposed();
            ValidateIV(iv);

            if (_isGcmMode)
            {
                return EncryptGcm(plaintext, iv, associatedData);
            }
            else
            {
                return EncryptNonGcm(plaintext, iv);
            }
        }

        public byte[] Decrypt(byte[] ciphertext, byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null)
        {
            ThrowIfDisposed();
            ValidateIV(iv);

            if (_isGcmMode)
            {
                if (authenticationTag == null || authenticationTag.Length == 0)
                    throw new ArgumentException("Authentication tag is required for GCM mode");

                return DecryptGcm(ciphertext, iv, authenticationTag, associatedData);
            }
            else
            {
                if (authenticationTag != null && authenticationTag.Length > 0)
                    throw new InvalidOperationException("Authentication tag should not be provided for non-GCM modes");

                return DecryptNonGcm(ciphertext, iv);
            }
        }

        public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, ReadOnlySpan<byte> associatedData = default)
        {
            ThrowIfDisposed();
            ValidateIV(iv);

            if (_isGcmMode)
            {
                EncryptGcm(plaintext, iv, destination, out bytesWritten, associatedData);
            }
            else
            {
                EncryptNonGcm(plaintext, iv, destination, out bytesWritten);
            }
        }

        public void Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, ReadOnlySpan<byte> associatedData = default, ReadOnlySpan<byte> authenticationTag = default)
        {
            ThrowIfDisposed();
            ValidateIV(iv);

            if (_isGcmMode)
            {
                if (authenticationTag.IsEmpty)
                    throw new ArgumentException("Authentication tag is required for GCM mode");

                DecryptGcm(ciphertext, iv, destination, out bytesWritten, authenticationTag, associatedData);
            }
            else
            {
                if (!authenticationTag.IsEmpty)
                    throw new InvalidOperationException("Authentication tag should not be provided for non-GCM modes");

                DecryptNonGcm(ciphertext, iv, destination, out bytesWritten);
            }
        }

        #endregion

        #region GCM模式加解密实现

        private byte[] EncryptGcm(byte[] plaintext, byte[] iv, byte[]? associatedData)
        {
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[DEFAULT_TAG_SIZE];

            if (associatedData == null || associatedData.Length == 0)
            {
                _aesGcm!.Encrypt(iv, plaintext, ciphertext, tag);
            }
            else
            {
                _aesGcm!.Encrypt(iv, plaintext, ciphertext, tag, associatedData);
            }

            return ciphertext;
        }

        private void EncryptGcm(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, ReadOnlySpan<byte> associatedData)
        {
            bytesWritten = plaintext.Length;
            Span<byte> tag = stackalloc byte[DEFAULT_TAG_SIZE];

            if (associatedData.IsEmpty)
            {
                _aesGcm!.Encrypt(iv, plaintext, destination, tag);
            }
            else
            {
                _aesGcm!.Encrypt(iv, plaintext, destination, tag, associatedData);
            }
        }

        private byte[] DecryptGcm(byte[] ciphertext, byte[] iv, byte[] tag, byte[]? associatedData)
        {
            byte[] plaintext = new byte[ciphertext.Length];

            if (associatedData == null || associatedData.Length == 0)
            {
                _aesGcm!.Decrypt(iv, ciphertext, tag, plaintext);
            }
            else
            {
                _aesGcm!.Decrypt(iv, ciphertext, tag, plaintext, associatedData);
            }

            return plaintext;
        }

        private void DecryptGcm(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten, ReadOnlySpan<byte> tag, ReadOnlySpan<byte> associatedData)
        {
            bytesWritten = ciphertext.Length;

            if (associatedData.IsEmpty)
            {
                _aesGcm!.Decrypt(iv, ciphertext, tag, destination);
            }
            else
            {
                _aesGcm!.Decrypt(iv, ciphertext, tag, destination, associatedData);
            }
        }

        #endregion

        #region 非GCM模式加解密实现

        private byte[] EncryptNonGcm(byte[] plaintext, byte[] iv)
        {
            using var encryptor = _aes.CreateEncryptor(_key, iv);
            return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }

        private void EncryptNonGcm(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten)
        {
            using var encryptor = _aes.CreateEncryptor(_key, iv.ToArray());

            int blockSize = encryptor.InputBlockSize;
            int inputIndex = 0;
            bytesWritten = 0;

            // 处理完整块
            while (inputIndex <= plaintext.Length - blockSize)
            {
                int transformed = encryptor.TransformBlock(
                    plaintext.ToArray(), inputIndex, blockSize,
                    destination.ToArray(), bytesWritten);

                inputIndex += blockSize;
                bytesWritten += transformed;
            }

            // 处理最后一个块
            byte[] finalBlock = encryptor.TransformFinalBlock(
                plaintext.ToArray(), inputIndex, plaintext.Length - inputIndex);

            finalBlock.AsSpan().CopyTo(destination.Slice(bytesWritten));
            bytesWritten += finalBlock.Length;
        }

        private byte[] DecryptNonGcm(byte[] ciphertext, byte[] iv)
        {
            using var decryptor = _aes.CreateDecryptor(_key, iv);
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }

        private void DecryptNonGcm(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, Span<byte> destination, out int bytesWritten)
        {
            using var decryptor = _aes.CreateDecryptor(_key, iv.ToArray());

            int blockSize = decryptor.InputBlockSize;
            int inputIndex = 0;
            bytesWritten = 0;

            // 处理完整块
            while (inputIndex <= ciphertext.Length - blockSize)
            {
                int transformed = decryptor.TransformBlock(
                    ciphertext.ToArray(), inputIndex, blockSize,
                    destination.ToArray(), bytesWritten);

                inputIndex += blockSize;
                bytesWritten += transformed;
            }

            // 处理最后一个块
            byte[] finalBlock = decryptor.TransformFinalBlock(
                ciphertext.ToArray(), inputIndex, ciphertext.Length - inputIndex);

            finalBlock.AsSpan().CopyTo(destination.Slice(bytesWritten));
            bytesWritten += finalBlock.Length;
        }

        #endregion

        #region 字符串便捷方法

        public string EncryptToBase64(string plaintext, byte[] iv, byte[]? associatedData = null)
        {
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertext = Encrypt(plaintextBytes, iv, associatedData);
            return Convert.ToBase64String(ciphertext);
        }

        public string EncryptToHex(string plaintext, byte[] iv, byte[]? associatedData = null)
        {
            byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            byte[] ciphertext = Encrypt(plaintextBytes, iv, associatedData);
            return BytesToHexString(ciphertext);
        }

        public string DecryptFromBase64(string base64Ciphertext, byte[] iv, byte[]? associatedData = null, byte[]? tagBase64 = null)
        {
            byte[] ciphertext = Convert.FromBase64String(base64Ciphertext);
            byte[]? tag = tagBase64 != null ? Convert.FromBase64String(Convert.ToBase64String(tagBase64)) : null;
            byte[] plaintext = Decrypt(ciphertext, iv, associatedData, tag);
            return Encoding.UTF8.GetString(plaintext);
        }

        public string DecryptFromHex(string hexCiphertext, byte[] iv, byte[]? associatedData = null, string? tagHex = null)
        {
            byte[] ciphertext = HexStringToBytes(hexCiphertext);
            byte[]? tag = tagHex != null ? HexStringToBytes(tagHex) : null;
            byte[] plaintext = Decrypt(ciphertext, iv, associatedData, tag);
            return Encoding.UTF8.GetString(plaintext);
        }

        #endregion

        #region 流处理方法 - 安全的实现

        public ICryptoTransform CreateEncryptor(byte[] iv, byte[]? associatedData = null)
        {
            ThrowIfDisposed();

            if (_isGcmMode)
                throw new NotSupportedException("ICryptoTransform is not supported for GCM mode. Use streaming methods instead.");

            return _aes.CreateEncryptor(_key, iv);
        }

        public ICryptoTransform CreateDecryptor(byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null)
        {
            ThrowIfDisposed();

            if (_isGcmMode)
                throw new NotSupportedException("ICryptoTransform is not supported for GCM mode. Use streaming methods instead.");

            if (authenticationTag != null && authenticationTag.Length > 0)
                throw new ArgumentException("Authentication tag is not supported for non-GCM modes");

            return _aes.CreateDecryptor(_key, iv);
        }

        // 安全的异步方法实现，不包含不安全代码
        public async Task EncryptAsync(Stream plaintextStream, Stream ciphertextStream, byte[] iv,
            byte[]? associatedData = null, IProgress<long>? progress = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ValidateIV(iv);

            // 将不安全代码提取到独立方法中
            if (_isGcmMode)
            {
                await SafeEncryptGcmAsync(plaintextStream, ciphertextStream, iv, associatedData, progress, ct);
            }
            else
            {
                await SafeEncryptNonGcmAsync(plaintextStream, ciphertextStream, iv, progress, ct);
            }
        }

        public async Task DecryptAsync(Stream ciphertextStream, Stream plaintextStream, byte[] iv,
            byte[]? associatedData = null, byte[]? authenticationTag = null,
            IProgress<long>? progress = null, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ValidateIV(iv);

            if (_isGcmMode)
            {
                if (authenticationTag == null || authenticationTag.Length == 0)
                    throw new ArgumentException("Authentication tag is required for GCM mode");

                await SafeDecryptGcmAsync(ciphertextStream, plaintextStream, iv, authenticationTag, associatedData, progress, ct);
            }
            else
            {
                await SafeDecryptNonGcmAsync(ciphertextStream, plaintextStream, iv, progress, ct);
            }
        }

        // 安全的异步加密方法（非GCM）
        private async Task SafeEncryptNonGcmAsync(Stream input, Stream output, byte[] iv,
            IProgress<long>? progress, CancellationToken ct)
        {
            const int bufferSize = 81920; // 80KB缓冲区
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(bufferSize + AES_BLOCK_SIZE);

            try
            {
                using var encryptor = _aes.CreateEncryptor(_key, iv);
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await input.ReadAsync(buffer, 0, bufferSize, ct)) > 0)
                {
                    int bytesToProcess = bytesRead;
                    int inputOffset = 0;

                    // 处理完整块
                    while (bytesToProcess >= encryptor.InputBlockSize)
                    {
                        int transformed = encryptor.TransformBlock(
                            buffer, inputOffset, encryptor.InputBlockSize,
                            outputBuffer, 0);

                        await output.WriteAsync(outputBuffer, 0, transformed, ct);

                        inputOffset += encryptor.InputBlockSize;
                        bytesToProcess -= encryptor.InputBlockSize;
                        totalRead += encryptor.InputBlockSize;
                    }

                    // 处理剩余数据
                    if (bytesToProcess > 0)
                    {
                        byte[] finalBlock = encryptor.TransformFinalBlock(
                            buffer, inputOffset, bytesToProcess);

                        await output.WriteAsync(finalBlock, 0, finalBlock.Length, ct);
                        totalRead += bytesToProcess;
                    }

                    progress?.Report(totalRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                ArrayPool<byte>.Shared.Return(outputBuffer);
            }
        }

        // 安全的异步加密方法（GCM）
        private async Task SafeEncryptGcmAsync(Stream input, Stream output, byte[] iv, byte[]? associatedData,
            IProgress<long>? progress, CancellationToken ct)
        {
            // GCM模式不支持流式加密，需要一次性处理
            using var ms = new MemoryStream();
            await input.CopyToAsync(ms, ct);
            byte[] plaintext = ms.ToArray();

            byte[] ciphertext = EncryptGcm(plaintext, iv, associatedData);

            await output.WriteAsync(ciphertext, 0, ciphertext.Length, ct);
            progress?.Report(plaintext.Length);
        }

        // 安全的异步解密方法（非GCM）
        private async Task SafeDecryptNonGcmAsync(Stream input, Stream output, byte[] iv,
            IProgress<long>? progress, CancellationToken ct)
        {
            const int bufferSize = 81920;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            byte[] outputBuffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                using var decryptor = _aes.CreateDecryptor(_key, iv);
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await input.ReadAsync(buffer, 0, bufferSize, ct)) > 0)
                {
                    int bytesToProcess = bytesRead;
                    int inputOffset = 0;

                    // 处理完整块
                    while (bytesToProcess >= decryptor.InputBlockSize)
                    {
                        int transformed = decryptor.TransformBlock(
                            buffer, inputOffset, decryptor.InputBlockSize,
                            outputBuffer, 0);

                        await output.WriteAsync(outputBuffer, 0, transformed, ct);

                        inputOffset += decryptor.InputBlockSize;
                        bytesToProcess -= decryptor.InputBlockSize;
                        totalRead += decryptor.InputBlockSize;
                    }

                    // 处理剩余数据
                    if (bytesToProcess > 0)
                    {
                        byte[] finalBlock = decryptor.TransformFinalBlock(
                            buffer, inputOffset, bytesToProcess);

                        await output.WriteAsync(finalBlock, 0, finalBlock.Length, ct);
                        totalRead += bytesToProcess;
                    }

                    progress?.Report(totalRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                ArrayPool<byte>.Shared.Return(outputBuffer);
            }
        }

        // 安全的异步解密方法（GCM）
        private async Task SafeDecryptGcmAsync(Stream input, Stream output, byte[] iv, byte[] tag, byte[]? associatedData,
            IProgress<long>? progress, CancellationToken ct)
        {
            // GCM模式不支持流式解密，需要一次性处理
            using var ms = new MemoryStream();
            await input.CopyToAsync(ms, ct);
            byte[] ciphertext = ms.ToArray();

            byte[] plaintext = DecryptGcm(ciphertext, iv, tag, associatedData);

            await output.WriteAsync(plaintext, 0, plaintext.Length, ct);
            progress?.Report(ciphertext.Length);
        }

        #endregion

        #region 随机数生成

        public byte[] GenerateIV()
        {
            return GenerateRandomBytes(AES_IV_SIZE);
        }

        public byte[] GenerateNonce()
        {
            // 对于GCM模式，推荐使用12字节的nonce
            return GenerateRandomBytes(_isGcmMode ? 12 : AES_IV_SIZE);
        }

        public byte[] GenerateRandomBytes(int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        #endregion

        #region 业务友好方法

        public byte[] EncryptWithPrefixIV(byte[] plaintext, byte[]? associatedData = null)
        {
            byte[] iv = GenerateIV();
            byte[] ciphertext;
            byte[] tag = EmptyByteArray;

            if (_isGcmMode)
            {
                ciphertext = new byte[plaintext.Length];
                tag = new byte[DEFAULT_TAG_SIZE];

                if (associatedData == null || associatedData.Length == 0)
                {
                    _aesGcm!.Encrypt(iv, plaintext, ciphertext, tag);
                }
                else
                {
                    _aesGcm!.Encrypt(iv, plaintext, ciphertext, tag, associatedData);
                }

                // 返回格式: iv + ciphertext + tag
                byte[] result = new byte[iv.Length + ciphertext.Length + tag.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
                Buffer.BlockCopy(tag, 0, result, iv.Length + ciphertext.Length, tag.Length);
                return result;
            }
            else
            {
                ciphertext = Encrypt(plaintext, iv);

                // 返回格式: iv + ciphertext
                byte[] result = new byte[iv.Length + ciphertext.Length];
                Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
                Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
                return result;
            }
        }

        public byte[] DecryptWithPrefixIV(byte[] combinedData, byte[]? associatedData = null)
        {
            if (_isGcmMode)
            {
                // 格式: iv(16) + ciphertext + tag(16)
                if (combinedData.Length < AES_IV_SIZE + DEFAULT_TAG_SIZE)
                    throw new ArgumentException("Invalid combined data length for GCM mode");

                byte[] iv = new byte[AES_IV_SIZE];
                byte[] tag = new byte[DEFAULT_TAG_SIZE];
                byte[] ciphertext = new byte[combinedData.Length - AES_IV_SIZE - DEFAULT_TAG_SIZE];

                Buffer.BlockCopy(combinedData, 0, iv, 0, AES_IV_SIZE);
                Buffer.BlockCopy(combinedData, AES_IV_SIZE, ciphertext, 0, ciphertext.Length);
                Buffer.BlockCopy(combinedData, AES_IV_SIZE + ciphertext.Length, tag, 0, DEFAULT_TAG_SIZE);

                return DecryptGcm(ciphertext, iv, tag, associatedData);
            }
            else
            {
                // 格式: iv(16) + ciphertext
                if (combinedData.Length < AES_IV_SIZE)
                    throw new ArgumentException("Invalid combined data length");

                byte[] iv = new byte[AES_IV_SIZE];
                byte[] ciphertext = new byte[combinedData.Length - AES_IV_SIZE];

                Buffer.BlockCopy(combinedData, 0, iv, 0, AES_IV_SIZE);
                Buffer.BlockCopy(combinedData, AES_IV_SIZE, ciphertext, 0, ciphertext.Length);

                return Decrypt(ciphertext, iv);
            }
        }

        public string EncryptWithPrefixIVToBase64(byte[] plaintext, byte[]? associatedData = null)
        {
            byte[] combined = EncryptWithPrefixIV(plaintext, associatedData);
            return Convert.ToBase64String(combined);
        }

        public byte[] DecryptWithPrefixIVFromBase64(string base64Data, byte[]? associatedData = null)
        {
            byte[] combined = Convert.FromBase64String(base64Data);
            return DecryptWithPrefixIV(combined, associatedData);
        }

        #endregion

        #region 遗留方法

        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        public byte[] EncryptWithoutIV_ECB(byte[] plaintext)
        {
            if (_extendedMode != ExtendedCipherMode.ECB)
                throw new InvalidOperationException("This method is only valid in ECB mode");

            return Encrypt(plaintext, new byte[AES_IV_SIZE]); // 使用全零IV
        }

        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        public byte[] DecryptWithoutIV_ECB(byte[] ciphertext)
        {
            if (_extendedMode != ExtendedCipherMode.ECB)
                throw new InvalidOperationException("This method is only valid in ECB mode");

            return Decrypt(ciphertext, new byte[AES_IV_SIZE]); // 使用全零IV
        }

        #endregion

        #region 辅助方法

        private static byte[] HexStringToBytes(string hex)
        {
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have even length");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            return bytes;
        }

        private static string BytesToHexString(byte[] bytes)
        {
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static void ValidateKey(byte[] key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                throw new ArgumentException("AES key must be 128, 192, or 256 bits (16, 24, or 32 bytes)");
        }

        private static void ValidateModeAndPadding(ExtendedCipherMode mode, PaddingMode padding)
        {
            if (!Enum.IsDefined(typeof(ExtendedCipherMode), mode))
                throw new ArgumentException($"Invalid cipher mode: {mode}");

            if (!Enum.IsDefined(typeof(PaddingMode), padding))
                throw new ArgumentException($"Invalid padding mode: {padding}");

            // GCM模式只支持NoPadding
            if (mode == ExtendedCipherMode.GCM && padding != PaddingMode.None)
                throw new ArgumentException("GCM mode only supports PaddingMode.None");
        }

        private void ValidateIV(ReadOnlySpan<byte> iv)
        {
            if (iv.Length != AES_IV_SIZE)
                throw new ArgumentException($"IV must be {AES_IV_SIZE} bytes for AES");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        #endregion

        #region 清理和资源释放

        public void Dispose()
        {
            if (_disposed) return;

            ClearKey();

            _aes?.Dispose();
            _aesGcm?.Dispose();

            if (_keyHandle.IsAllocated)
                _keyHandle.Free();

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public void ClearKey()
        {
            if (_key != null)
            {
                // 使用Unsafe将密钥内存清零
                fixed (byte* pKey = _key)
                {
                    Unsafe.InitBlock(pKey, 0, (uint)_key.Length);
                }
                _key = EmptyByteArray;
            }
        }

        ~AesCrypto()
        {
            Dispose();
        }

        #endregion
    }
}
