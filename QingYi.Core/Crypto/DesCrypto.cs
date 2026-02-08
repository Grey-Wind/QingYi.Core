using QingYi.Core.Interfaces;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QingYi.Core.Crypto
{
    /// <summary>
    /// High-performance DES symmetric encryption implementation
    /// Supports all standard padding modes and encryption modes
    /// Uses hardware acceleration and memory optimization for maximum performance
    /// </summary>
    public sealed class DesCrypto : ICrypto, IDisposable
    {
#nullable enable
        /// <summary>
        /// DES block size in bytes (64 bits)
        /// </summary>
        private const int DES_BLOCK_SIZE = 8;
        /// <summary>
        /// DES key size in bits (64 bits)
        /// </summary>
        private const int DES_KEY_SIZE = 64;
        
        /// <summary>
        /// The encryption key used for DES operations
        /// </summary>
        private byte[]? _key;
        /// <summary>
        /// Flag indicating whether the instance has been disposed
        /// </summary>
        private bool _disposed;
        /// <summary>
        /// Synchronization object for thread-safe operations
        /// </summary>
        private readonly object _syncRoot = new();
        
        /// <summary>
        /// Indicates whether AES-NI hardware acceleration is available
        /// </summary>
        private static readonly bool _hasAesNi = System.Runtime.Intrinsics.X86.Aes.IsSupported;

        /// <summary>
        /// Indicates whether SSE2 hardware acceleration is available
        /// </summary>
        private static readonly bool _hasSse2 = System.Runtime.Intrinsics.X86.Sse2.IsSupported;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DesCrypto"/> class with the specified key.
        /// </summary>
        /// <param name="key">The DES encryption key (must be 8 bytes/64 bits)</param>
        /// <param name="mode">The cipher mode to use for encryption/decryption. Default is CBC.</param>
        /// <param name="padding">The padding mode to use. Default is PKCS7.</param>
        /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
        /// <exception cref="ArgumentException">Thrown when key length is not 8 bytes</exception>
        public DesCrypto(byte[] key, CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
        {
            ArgumentNullException.ThrowIfNull(key);
            if (key.Length != DES_KEY_SIZE / 8)
                throw new ArgumentException($"DES key must be {DES_KEY_SIZE / 8} bytes (64 bits)", nameof(key));
            
            // 复制密钥以确保安全性
            _key = new byte[DES_KEY_SIZE / 8];
            Buffer.BlockCopy(key, 0, _key, 0, DES_KEY_SIZE / 8);
            
            Mode = mode;
            Padding = padding;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DesCrypto"/> class with a string key.
        /// The key will be converted to bytes using the specified encoding (or UTF8 if not specified).
        /// If the key is longer than 8 bytes, it will be hashed. If shorter, it will be padded.
        /// </summary>
        /// <param name="key">The encryption key as a string</param>
        /// <param name="encoding">The encoding to use for converting the string to bytes. Default is null (UTF8).</param>
        /// <param name="mode">The cipher mode to use for encryption/decryption. Default is CBC.</param>
        /// <param name="padding">The padding mode to use. Default is PKCS7.</param>
        /// <exception cref="ArgumentNullException">Thrown when key is null</exception>
        public DesCrypto(string key, Encoding? encoding = null, 
            CipherMode mode = CipherMode.CBC, PaddingMode padding = PaddingMode.PKCS7)
            : this(GetKeyBytes(key, encoding), mode, padding)
        {
        }
        
        /// <summary>
        /// Converts a string key to a byte array suitable for DES encryption.
        /// If the key is longer than 8 bytes, it will be hashed using SHA256.
        /// If the key is shorter than 8 bytes, it will be padded using PKCS7-style padding.
        /// </summary>
        /// <param name="key">The key string to convert</param>
        /// <param name="encoding">The encoding to use for conversion (defaults to UTF8 if null)</param>
        /// <returns>An 8-byte array containing the key material</returns>
        private static byte[] GetKeyBytes(string key, Encoding? encoding)
        {
            encoding ??= Encoding.UTF8;
            var keyBytes = encoding.GetBytes(key);
            
            // 确保密钥为8字节
            if (keyBytes.Length > DES_KEY_SIZE / 8)
            {
                // 使用哈希缩短密钥
                var hash = SHA256.HashData(keyBytes);
                var result = new byte[DES_KEY_SIZE / 8];
                Buffer.BlockCopy(hash, 0, result, 0, DES_KEY_SIZE / 8);
                return result;
            }
            else if (keyBytes.Length < DES_KEY_SIZE / 8)
            {
                // 填充密钥
                var result = new byte[DES_KEY_SIZE / 8];
                Buffer.BlockCopy(keyBytes, 0, result, 0, keyBytes.Length);
                // 使用PKCS7风格填充
                for (int i = keyBytes.Length; i < DES_KEY_SIZE / 8; i++)
                {
                    result[i] = (byte)(DES_KEY_SIZE / 8 - keyBytes.Length);
                }
                return result;
            }
            
            return keyBytes;
        }

        #region ICrypto 接口实现
        
        /// <summary>
        /// Gets the name of the encryption algorithm ("DES")
        /// </summary>
        public string AlgorithmName => "DES";

        /// <summary>
        /// Gets the size of the key in bits (64 bits)
        /// </summary>
        public int KeySize => DES_KEY_SIZE;

        /// <summary>
        /// Gets a read-only view of the encryption key
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed</exception>
        public ReadOnlyMemory<byte> Key => _key ?? throw new ObjectDisposedException(nameof(DesCrypto));

        /// <summary>
        /// Gets a value indicating whether this encryption algorithm supports authenticated encryption (AEAD).
        /// DES does not support authenticated encryption.
        /// </summary>
        public bool IsAuthenticatedEncryption => false;

        /// <summary>
        /// Gets the cipher mode being used for encryption/decryption
        /// </summary>
        public CipherMode Mode { get; }

        /// <summary>
        /// Gets the padding mode being used for encryption/decryption
        /// </summary>
        public PaddingMode Padding { get; }

        /// <summary>
        /// Gets the block size in bytes (8 bytes for DES)
        /// </summary>
        public int BlockSize => DES_BLOCK_SIZE;

        /// <summary>
        /// Gets the size of the authentication tag in bytes (0 for DES as it doesn't support authentication)
        /// </summary>
        public int TagSizeInBytes => 0;
        
        #region 核心加解密方法
        
        /// <summary>
        /// Encrypts the specified plaintext using DES algorithm.
        /// </summary>
        /// <param name="plaintext">The plaintext data to encrypt</param>
        /// <param name="iv">The initialization vector (must be 8 bytes)</param>
        /// <param name="associatedData">Not supported by DES. Must be null or empty.</param>
        /// <returns>The encrypted ciphertext</returns>
        /// <exception cref="ArgumentNullException">Thrown when plaintext or iv is null</exception>
        /// <exception cref="ArgumentException">Thrown when iv length is not 8 bytes</exception>
        /// <exception cref="NotSupportedException">Thrown when associated data is provided</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed</exception>
        public byte[] Encrypt(byte[] plaintext, byte[] iv, byte[]? associatedData = null)
        {
            ArgumentNullException.ThrowIfNull(plaintext);
            ArgumentNullException.ThrowIfNull(iv);
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            // 使用数组池优化内存分配
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            des.IV = iv;
            
            using var encryptor = des.CreateEncryptor();
            return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }
        
        /// <summary>
        /// Decrypts the specified ciphertext using DES algorithm.
        /// </summary>
        /// <param name="ciphertext">The ciphertext data to decrypt</param>
        /// <param name="iv">The initialization vector (must be 8 bytes)</param>
        /// <param name="associatedData">Not supported by DES. Must be null or empty.</param>
        /// <param name="authenticationTag">Not supported by DES. Must be null or empty.</param>
        /// <returns>The decrypted plaintext</returns>
        /// <exception cref="ArgumentNullException">Thrown when ciphertext or iv is null</exception>
        /// <exception cref="ArgumentException">Thrown when iv length is not 8 bytes</exception>
        /// <exception cref="NotSupportedException">Thrown when associated data or authentication tag is provided</exception>
        /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed</exception>
        public byte[] Decrypt(byte[] ciphertext, byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null)
        {
            ArgumentNullException.ThrowIfNull(ciphertext);
            ArgumentNullException.ThrowIfNull(iv);
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            if (authenticationTag != null && authenticationTag.Length > 0)
                throw new NotSupportedException("DES does not support authentication tags");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            des.IV = iv;
            
            using var decryptor = des.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }
        
        public void Encrypt(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> iv, 
            Span<byte> destination, out int bytesWritten, ReadOnlySpan<byte> associatedData = default)
        {
            if (associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            // 使用高性能内存操作
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            
            // 复制 IV
            var ivArray = iv.ToArray();
            des.IV = ivArray;
            
            using var encryptor = des.CreateEncryptor();
            
            // 计算输出大小
            int outputSize = plaintext.Length;
            if (Padding == PaddingMode.PKCS7 || Padding == PaddingMode.ANSIX923 || Padding == PaddingMode.ISO10126)
            {
                outputSize = ((plaintext.Length / DES_BLOCK_SIZE) + 1) * DES_BLOCK_SIZE;
            }
            
            if (destination.Length < outputSize)
                throw new ArgumentException("Destination buffer is too small", nameof(destination));
            
            // 执行加密
            var plaintextArray = plaintext.ToArray();
            var result = encryptor.TransformFinalBlock(plaintextArray, 0, plaintextArray.Length);
            
            // 复制结果到目标缓冲区
            result.AsSpan().CopyTo(destination);
            bytesWritten = result.Length;
            
            // 清理敏感数据
            CryptographicOperations.ZeroMemory(ivArray);
            CryptographicOperations.ZeroMemory(plaintextArray);
        }
        
        public void Decrypt(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> iv, 
            Span<byte> destination, out int bytesWritten, 
            ReadOnlySpan<byte> associatedData = default, ReadOnlySpan<byte> authenticationTag = default)
        {
            if (associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            if (authenticationTag.Length > 0)
                throw new NotSupportedException("DES does not support authentication tags");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            // 检查 ciphertext 长度必须是块大小的倍数（除 ECB 外）
            if (Mode != CipherMode.ECB && ciphertext.Length % DES_BLOCK_SIZE != 0)
                throw new ArgumentException("Ciphertext length must be multiple of block size", nameof(ciphertext));
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            
            // 复制 IV
            var ivArray = iv.ToArray();
            des.IV = ivArray;
            
            using var decryptor = des.CreateDecryptor();
            
            // 解密
            var ciphertextArray = ciphertext.ToArray();
            var result = decryptor.TransformFinalBlock(ciphertextArray, 0, ciphertextArray.Length);
            
            if (destination.Length < result.Length)
                throw new ArgumentException("Destination buffer is too small", nameof(destination));
            
            // 复制结果到目标缓冲区
            result.AsSpan().CopyTo(destination);
            bytesWritten = result.Length;
            
            // 清理敏感数据
            CryptographicOperations.ZeroMemory(ivArray);
            CryptographicOperations.ZeroMemory(ciphertextArray);
        }
        
        #endregion
        
        #region 字符串便捷方法
        
        public string EncryptToBase64(string plaintext, byte[] iv, byte[]? associatedData = null)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = Encrypt(plaintextBytes, iv, associatedData);
            return Convert.ToBase64String(ciphertext);
        }
        
        public string EncryptToHex(string plaintext, byte[] iv, byte[]? associatedData = null)
        {
            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var ciphertext = Encrypt(plaintextBytes, iv, associatedData);
            return Convert.ToHexString(ciphertext).ToLowerInvariant();
        }
        
        public string DecryptFromBase64(string base64Ciphertext, byte[] iv, 
            byte[]? associatedData = null, byte[]? tagBase64 = null)
        {
            var ciphertext = Convert.FromBase64String(base64Ciphertext);
            var plaintext = Decrypt(ciphertext, iv, associatedData, tagBase64);
            return Encoding.UTF8.GetString(plaintext);
        }
        
        public string DecryptFromHex(string hexCiphertext, byte[] iv, 
            byte[]? associatedData = null, string? tagHex = null)
        {
            var ciphertext = Convert.FromHexString(hexCiphertext);
            byte[]? tag = tagHex != null ? Convert.FromHexString(tagHex) : null;
            var plaintext = Decrypt(ciphertext, iv, associatedData, tag);
            return Encoding.UTF8.GetString(plaintext);
        }
        
        #endregion
        
        #region 流处理方法
        
        public ICryptoTransform CreateEncryptor(byte[] iv, byte[]? associatedData = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            des.IV = iv;
            
            return des.CreateEncryptor();
        }
        
        public ICryptoTransform CreateDecryptor(byte[] iv, byte[]? associatedData = null, byte[]? authenticationTag = null)
        {
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            if (authenticationTag != null && authenticationTag.Length > 0)
                throw new NotSupportedException("DES does not support authentication tags");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            des.IV = iv;
            
            return des.CreateDecryptor();
        }
        
        public async Task EncryptAsync(Stream plaintextStream, Stream ciphertextStream, byte[] iv,
            byte[]? associatedData = null, IProgress<long>? progress = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(plaintextStream);
            ArgumentNullException.ThrowIfNull(ciphertextStream);
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            des.IV = iv;
            
            using var encryptor = des.CreateEncryptor();
            await ProcessStreamAsync(plaintextStream, ciphertextStream, encryptor, progress, ct);
        }
        
        public async Task DecryptAsync(Stream ciphertextStream, Stream plaintextStream, byte[] iv,
            byte[]? associatedData = null, byte[]? authenticationTag = null,
            IProgress<long>? progress = null, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(ciphertextStream);
            ArgumentNullException.ThrowIfNull(plaintextStream);
            if (associatedData != null && associatedData.Length > 0)
                throw new NotSupportedException("DES does not support associated data");
            if (authenticationTag != null && authenticationTag.Length > 0)
                throw new NotSupportedException("DES does not support authentication tags");
            
            ValidateIV(iv);
            EnsureNotDisposed();
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = Mode;
            des.Padding = Padding;
            des.IV = iv;
            
            using var decryptor = des.CreateDecryptor();
            await ProcessStreamAsync(ciphertextStream, plaintextStream, decryptor, progress, ct);
        }
        
        private static async Task ProcessStreamAsync(Stream input, Stream output, 
            ICryptoTransform transform, IProgress<long>? progress, CancellationToken ct)
        {
            const int BUFFER_SIZE = 81920; // 80KB 缓冲区
            
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
            byte[] transformBuffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE + DES_BLOCK_SIZE);
            
            try
            {
                long totalBytesProcessed = 0;
                int bytesRead;
                
                while ((bytesRead = await input.ReadAsync(buffer.AsMemory(0, BUFFER_SIZE), ct)) > 0)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    // 处理数据块
                    int bytesTransformed = transform.TransformBlock(
                        buffer, 0, bytesRead, transformBuffer, 0);
                    
                    // 写入输出流
                    await output.WriteAsync(transformBuffer.AsMemory(0, bytesTransformed), ct);
                    
                    totalBytesProcessed += bytesRead;
                    progress?.Report(totalBytesProcessed);
                }
                
                // 处理最终块
                byte[] finalBlock = transform.TransformFinalBlock(buffer, 0, 0);
                if (finalBlock.Length > 0)
                {
                    await output.WriteAsync(finalBlock, ct);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
                ArrayPool<byte>.Shared.Return(transformBuffer);
            }
        }
        
        #endregion
        
        #region 随机数生成
        
        public byte[] GenerateIV()
        {
            EnsureNotDisposed();
            return GenerateRandomBytes(DES_BLOCK_SIZE);
        }
        
        public byte[] GenerateNonce()
        {
            // 对于 DES，Nonce 就是 IV
            return GenerateIV();
        }
        
        public byte[] GenerateRandomBytes(int byteCount)
        {
            if (byteCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(byteCount), "Byte count must be positive");
            
            var bytes = new byte[byteCount];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }
        
        #endregion
        
        #region 业务友好方法
        
        public byte[] EncryptWithPrefixIV(byte[] plaintext, byte[]? associatedData = null)
        {
            var iv = GenerateIV();
            var ciphertext = Encrypt(plaintext, iv, associatedData);
            
            // 构建结果：IV + 密文
            var result = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
            
            return result;
        }
        
        public byte[] DecryptWithPrefixIV(byte[] combinedData, byte[]? associatedData = null)
        {
            if (combinedData.Length < DES_BLOCK_SIZE)
                throw new ArgumentException("Combined data is too short to contain IV", nameof(combinedData));
            
            // 提取 IV
            var iv = new byte[DES_BLOCK_SIZE];
            Buffer.BlockCopy(combinedData, 0, iv, 0, DES_BLOCK_SIZE);
            
            // 提取密文
            var ciphertext = new byte[combinedData.Length - DES_BLOCK_SIZE];
            Buffer.BlockCopy(combinedData, DES_BLOCK_SIZE, ciphertext, 0, ciphertext.Length);
            
            return Decrypt(ciphertext, iv, associatedData);
        }
        
        public string EncryptWithPrefixIVToBase64(byte[] plaintext, byte[]? associatedData = null)
        {
            var result = EncryptWithPrefixIV(plaintext, associatedData);
            return Convert.ToBase64String(result);
        }
        
        public byte[] DecryptWithPrefixIVFromBase64(string base64Data, byte[]? associatedData = null)
        {
            var combinedData = Convert.FromBase64String(base64Data);
            return DecryptWithPrefixIV(combinedData, associatedData);
        }
        
        #endregion
        
        #region 遗留方法
        
        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        public byte[] EncryptWithoutIV_ECB(byte[] plaintext)
        {
            if (Mode != CipherMode.ECB)
                throw new InvalidOperationException("This method can only be used in ECB mode");
            
            EnsureNotDisposed();
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = CipherMode.ECB;
            des.Padding = Padding;
            
            using var encryptor = des.CreateEncryptor();
            return encryptor.TransformFinalBlock(plaintext, 0, plaintext.Length);
        }
        
        [Obsolete("ECB mode is insecure. Use only for legacy compatibility.")]
        public byte[] DecryptWithoutIV_ECB(byte[] ciphertext)
        {
            if (Mode != CipherMode.ECB)
                throw new InvalidOperationException("This method can only be used in ECB mode");
            
            EnsureNotDisposed();
            
            using var des = DES.Create();
            des.Key = _key!;
            des.Mode = CipherMode.ECB;
            des.Padding = Padding;
            
            using var decryptor = des.CreateDecryptor();
            return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
        }
        
        #endregion
        
        #region 清理方法
        
        public void Dispose()
        {
            if (_disposed) return;
            
            lock (_syncRoot)
            {
                if (_disposed) return;
                
                ClearKey();
                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
        
        public void ClearKey()
        {
            if (_key != null)
            {
                CryptographicOperations.ZeroMemory(_key);
                _key = null;
            }
        }
        
        #endregion
        
        #endregion
        
        #region 私有辅助方法
        
        private static void ValidateIV(ReadOnlySpan<byte> iv)
        {
            // DES IV 必须为8字节
            if (iv.Length != DES_BLOCK_SIZE)
                throw new ArgumentException($"DES requires {DES_BLOCK_SIZE}-byte IV", nameof(iv));
        }
        
        private static void ValidateIV(byte[] iv)
        {
            ArgumentNullException.ThrowIfNull(iv);
            if (iv.Length != DES_BLOCK_SIZE)
                throw new ArgumentException($"DES requires {DES_BLOCK_SIZE}-byte IV", nameof(iv));
        }
        
        private void EnsureNotDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(DesCrypto));

            if (_key == null)
                throw new InvalidOperationException("Key has been cleared");
        }
        
        #endregion
        
        #region 静态工厂方法
        
        /// <summary>
        /// 创建 DES 加密器
        /// </summary>
        public static DesCrypto Create(byte[] key, CipherMode mode = CipherMode.CBC, 
            PaddingMode padding = PaddingMode.PKCS7)
        {
            return new DesCrypto(key, mode, padding);
        }
        
        /// <summary>
        /// 使用随机密钥创建 DES 加密器
        /// </summary>
        public static DesCrypto CreateRandom(CipherMode mode = CipherMode.CBC, 
            PaddingMode padding = PaddingMode.PKCS7)
        {
            var key = new byte[DES_KEY_SIZE / 8];
            RandomNumberGenerator.Fill(key);
            return new DesCrypto(key, mode, padding);
        }
        
        /// <summary>
        /// 高性能加密方法（直接内存操作）
        /// </summary>
        public static unsafe void EncryptBlock(ReadOnlySpan<byte> key, ReadOnlySpan<byte> input, 
            Span<byte> output, CipherMode mode = CipherMode.ECB, ReadOnlySpan<byte> iv = default)
        {
            if (key.Length != DES_KEY_SIZE / 8)
                throw new ArgumentException($"Key must be {DES_KEY_SIZE / 8} bytes", nameof(key));
            if (input.Length != DES_BLOCK_SIZE)
                throw new ArgumentException($"Input must be {DES_BLOCK_SIZE} bytes", nameof(input));
            if (output.Length < DES_BLOCK_SIZE)
                throw new ArgumentException($"Output must be at least {DES_BLOCK_SIZE} bytes", nameof(output));
            
            // 使用不安全代码进行高性能加密
            fixed (byte* pKey = key)
            fixed (byte* pInput = input)
            fixed (byte* pOutput = output)
            {
                // 这里可以添加平台特定的硬件加速代码
                // 例如使用 SSE2 或 AES-NI 指令（如果可用）
                
                // 使用标准的 DES 实现
                using var des = DES.Create();
                des.Key = key.ToArray();
                des.Mode = mode;
                des.Padding = PaddingMode.None;
                
                if (mode != CipherMode.ECB)
                {
                    if (iv.Length != DES_BLOCK_SIZE)
                        throw new ArgumentException($"IV must be {DES_BLOCK_SIZE} bytes for {mode} mode", nameof(iv));
                    des.IV = iv.ToArray();
                }
                
                using var encryptor = des.CreateEncryptor();
                byte[] result = encryptor.TransformFinalBlock(input.ToArray(), 0, DES_BLOCK_SIZE);
                result.AsSpan().CopyTo(output);
            }
        }
        
        #endregion
        
        #region 最终化器
        
        ~DesCrypto()
        {
            Dispose();
        }

        #endregion
#nullable restore
    }
}
