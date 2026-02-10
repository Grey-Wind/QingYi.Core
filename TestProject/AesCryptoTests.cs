using NUnit.Framework;
using QingYi.Core.Crypto;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace TestProject
{
    [TestFixture]
    public class AesCryptoTests
    {
        #region 测试数据准备

        private static readonly byte[] TestKey128;
        private static readonly byte[] TestKey192;
        private static readonly byte[] TestKey256;
        private static readonly byte[] TestIV;
        private static readonly byte[] TestPlaintext;
        private static readonly byte[] TestAssociatedData;

        static AesCryptoTests()
        {
            TestKey128 = new byte[16];
            TestKey192 = new byte[24];
            TestKey256 = new byte[32];
            TestIV = new byte[16];
            TestPlaintext = Encoding.UTF8.GetBytes("Hello, AES加密测试！");
            TestAssociatedData = Encoding.UTF8.GetBytes("AdditionalData123");

            RandomNumberGenerator.Fill(TestKey128);
            RandomNumberGenerator.Fill(TestKey192);
            RandomNumberGenerator.Fill(TestKey256);
            RandomNumberGenerator.Fill(TestIV);
        }

        #endregion

        #region 构造函数测试

        [Test]
        public void Constructor_WithValidKey_ShouldInitialize()
        {
            // Arrange & Act
            using var aes = new AesCrypto(TestKey256);

            // Assert
            Assert.That(aes.KeySize, Is.EqualTo(256));
            Assert.That(aes.Mode.ToString(), Is.EqualTo("CBC"));
            Assert.That(aes.Padding, Is.EqualTo(PaddingMode.PKCS7));
            Assert.That(aes.IsAuthenticatedEncryption, Is.False);
        }

        [Test]
        [TestCase(16)]
        [TestCase(24)]
        [TestCase(32)]
        public void Constructor_WithDifferentKeySizes_ShouldInitialize(int keySize)
        {
            // Arrange
            var key = new byte[keySize];
            RandomNumberGenerator.Fill(key);

            // Act
            using var aes = new AesCrypto(key);

            // Assert
            Assert.That(aes.KeySize, Is.EqualTo(keySize * 8));
        }

        [Test]
        public void Constructor_WithInvalidKey_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidKey = new byte[20]; // 不是有效的AES密钥长度

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new AesCrypto(invalidKey));
        }

        [Test]
        public void Constructor_WithNullKey_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AesCrypto(null!));
        }

        [Test]
        public void Constructor_WithDifferentModes_ShouldInitialize(
    [Values] AesCrypto.ExtendedCipherMode mode)
        {
            // 为每个模式选择正确的填充
            var padding = mode == AesCrypto.ExtendedCipherMode.GCM
                ? PaddingMode.None
                : PaddingMode.PKCS7;

            // Act
            using var aes = new AesCrypto(TestKey256, mode, padding);

            // Assert
            Assert.That(aes.ExtendedMode, Is.EqualTo(mode));
            Assert.That(aes.Padding, Is.EqualTo(padding));
        }

        [Test]
        public void Constructor_WithGcmAndInvalidPadding_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.GCM, PaddingMode.PKCS7));
        }

        #endregion

        #region 工厂方法测试

        [Test]
        public void CreateFromBase64Key_ShouldInitializeCorrectly()
        {
            // Arrange
            var base64Key = Convert.ToBase64String(TestKey256);

            // Act
            using var aes = AesCrypto.CreateFromBase64Key(base64Key);

            // Assert
            Assert.That(aes.KeySize, Is.EqualTo(256));
            Assert.That(aes.Key.ToArray(), Is.EqualTo(TestKey256));
        }

        [Test]
        public void CreateFromHexKey_ShouldInitializeCorrectly()
        {
            // Arrange
            var hexKey = Convert.ToHexString(TestKey256).ToLower();

            // Act
            using var aes = AesCrypto.CreateFromHexKey(hexKey);

            // Assert
            Assert.That(aes.KeySize, Is.EqualTo(256));
            Assert.That(aes.Key.ToArray(), Is.EqualTo(TestKey256));
        }

        [Test]
        public void CreateRandom_ShouldGenerateValidInstance()
        {
            // Act
            using var aes = AesCrypto.CreateRandom(256);

            // Assert
            Assert.That(aes.KeySize, Is.EqualTo(256));
            Assert.That(aes.Key.Length, Is.EqualTo(32));
        }

        #endregion

        #region 基本加解密测试

        [Test]
        [TestCase(AesCrypto.ExtendedCipherMode.CBC)]
        [TestCase(AesCrypto.ExtendedCipherMode.CFB)]
        public void EncryptDecrypt_NonGcmModes_ShouldReturnOriginalData(AesCrypto.ExtendedCipherMode mode)
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256, mode);
            var iv = aes.GenerateIV();

            // Act
            var ciphertext = aes.Encrypt(TestPlaintext, iv);
            var plaintext = aes.Decrypt(ciphertext, iv);

            // Assert
            Assert.That(plaintext, Is.EqualTo(TestPlaintext));
        }

        [Test]
        public void Encrypt_WithSpan_ShouldWorkCorrectly()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var plaintextSpan = TestPlaintext.AsSpan();
            var ciphertext = new byte[TestPlaintext.Length + 16]; // 预留填充空间

            // Act
            aes.Encrypt(plaintextSpan, iv, ciphertext, out int bytesWritten);

            // Assert
            Assert.That(bytesWritten, Is.GreaterThan(0));
        }

        [Test]
        public void Decrypt_WithSpan_ShouldWorkCorrectly()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var ciphertext = aes.Encrypt(TestPlaintext, iv);

            // 计算解密后可能的最大大小（包括填充）
            int maxPlaintextSize = ciphertext.Length;
            var plaintext = new byte[maxPlaintextSize];

            // Act
            bool success = aes.TryDecrypt(ciphertext.AsSpan(), iv, plaintext, out int bytesWritten);

            // Assert
            Assert.That(success, Is.True);
            var actualPlaintext = new byte[bytesWritten];
            Array.Copy(plaintext, actualPlaintext, bytesWritten);
            Assert.That(actualPlaintext, Is.EqualTo(TestPlaintext));
        }

        #endregion

        #region GCM模式测试

        [Test]
        public void GcmMode_ShouldSupportAuthenticatedEncryption()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.GCM, PaddingMode.None);
            var iv = aes.GenerateIV();

            // Assert
            Assert.That(aes.IsAuthenticatedEncryption, Is.True);
            Assert.That(aes.TagSizeInBytes, Is.EqualTo(16));
            Assert.That(aes.AlgorithmName, Is.EqualTo("AES-GCM"));
        }

        [Test]
        public void GcmMode_DecryptWithoutTag_ShouldThrowException()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.GCM, PaddingMode.None);
            var iv = aes.GenerateIV();
            var ciphertext = new byte[16]; // 模拟密文

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => aes.Decrypt(ciphertext, iv));
            Assert.That(ex!.Message, Does.Contain("Authentication tag"));
        }

        #endregion

        #region 字符串便捷方法测试

        [Test]
        public void EncryptToBase64DecryptFromBase64_ShouldReturnOriginalString()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var originalText = "Hello, 世界!";

            // Act
            var ciphertextBase64 = aes.EncryptToBase64(originalText, iv);
            var decryptedText = aes.DecryptFromBase64(ciphertextBase64, iv);

            // Assert
            Assert.That(decryptedText, Is.EqualTo(originalText));
        }

        [Test]
        public void EncryptToHexDecryptFromHex_ShouldReturnOriginalString()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var originalText = "Test encryption with hex";

            // Act
            var ciphertextHex = aes.EncryptToHex(originalText, iv);
            var decryptedText = aes.DecryptFromHex(ciphertextHex, iv);

            // Assert
            Assert.That(decryptedText, Is.EqualTo(originalText));
        }

        #endregion

        #region 流处理方法测试

        [Test]
        public async Task EncryptDecryptAsync_ShouldReturnOriginalData()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var originalData = Encoding.UTF8.GetBytes("Large data stream test " + new string('X', 10000));

            using var plaintextStream = new MemoryStream(originalData);
            using var encryptedStream = new MemoryStream();
            using var decryptedStream = new MemoryStream();

            // Act - 加密
            await aes.EncryptAsync(plaintextStream, encryptedStream, iv, ct:CancellationToken.None);
            encryptedStream.Position = 0;

            // Act - 解密
            await aes.DecryptAsync(encryptedStream, decryptedStream, iv, ct:CancellationToken.None);

            // Assert
            Assert.That(decryptedStream.ToArray(), Is.EqualTo(originalData));
        }

        [Test]
        public void CreateEncryptorCreateDecryptor_ShouldWorkCorrectly()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();

            // Act
            using var encryptor = aes.CreateEncryptor(iv);
            using var decryptor = aes.CreateDecryptor(iv);

            var ciphertext = encryptor.TransformFinalBlock(TestPlaintext, 0, TestPlaintext.Length);
            var plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);

            // Assert
            Assert.That(plaintext, Is.EqualTo(TestPlaintext));
        }

        #endregion

        #region 业务友好方法测试

        [Test]
        public void EncryptDecryptWithPrefixIV_ShouldReturnOriginalData()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);

            // Act
            var combined = aes.EncryptWithPrefixIV(TestPlaintext);
            var plaintext = aes.DecryptWithPrefixIV(combined);

            // Assert
            Assert.That(plaintext, Is.EqualTo(TestPlaintext));
        }

        [Test]
        public void EncryptDecryptWithPrefixIVToBase64_ShouldReturnOriginalData()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);

            // Act
            var base64Data = aes.EncryptWithPrefixIVToBase64(TestPlaintext);
            var plaintext = aes.DecryptWithPrefixIVFromBase64(base64Data);

            // Assert
            Assert.That(plaintext, Is.EqualTo(TestPlaintext));
        }

        #endregion

        #region 随机数生成测试

        [Test]
        public void GenerateIV_ShouldGenerate16Bytes()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);

            // Act
            var iv = aes.GenerateIV();

            // Assert
            Assert.That(iv.Length, Is.EqualTo(16));
        }

        [Test]
        public void GenerateNonce_ShouldGenerateCorrectSize()
        {
            // Arrange
            using var aesCbc = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.CBC);
            using var aesGcm = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.GCM, PaddingMode.None);

            // Act
            var nonceCbc = aesCbc.GenerateNonce();
            var nonceGcm = aesGcm.GenerateNonce();

            // Assert
            Assert.That(nonceCbc.Length, Is.EqualTo(16)); // 非GCM模式生成16字节
            Assert.That(nonceGcm.Length, Is.EqualTo(12)); // GCM模式生成12字节
        }

        [Test]
        [TestCase(16)]
        [TestCase(32)]
        [TestCase(64)]
        [TestCase(128)]
        public void GenerateRandomBytes_ShouldGenerateCorrectLength(int length)
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);

            // Act
            var bytes1 = aes.GenerateRandomBytes(length);
            var bytes2 = aes.GenerateRandomBytes(length);

            // Assert
            Assert.That(bytes1.Length, Is.EqualTo(length));
            Assert.That(bytes2.Length, Is.EqualTo(length));
            Assert.That(bytes1, Is.Not.EqualTo(bytes2)); // 应该是不同的随机数
        }

        #endregion

        #region 异常测试

        [Test]
        public void Encrypt_WithInvalidIV_ShouldThrowArgumentException()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var invalidIV = new byte[8]; // 太短

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => aes.Encrypt(TestPlaintext, invalidIV));
            Assert.That(ex!.Message, Does.Contain("IV must be"));
        }

        [Test]
        public void Decrypt_WithInvalidCiphertext_ShouldThrowCryptographicException()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var invalidCiphertext = new byte[16];

            // Act & Assert
            Assert.Throws<CryptographicException>(() => aes.Decrypt(invalidCiphertext, iv));
        }

        [Test]
        public void Dispose_ShouldPreventFurtherOperations()
        {
            // Arrange
            var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();

            // Act
            aes.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => aes.Encrypt(TestPlaintext, iv));
        }

        [Test]
        public void ClearKey_ShouldZeroOutKey()
        {
            // Arrange
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            using var aes = new AesCrypto(key);

            // Act
            aes.ClearKey();

            // Assert
            var keyAfterClear = aes.Key.ToArray();
            Assert.That(keyAfterClear, Is.All.EqualTo(0));
        }

        #endregion

        #region 属性测试

        [Test]
        public void Properties_ShouldReturnCorrectValues()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.CBC, PaddingMode.PKCS7);

            // Assert
            Assert.That(aes.AlgorithmName, Is.EqualTo("AES-256-CBC"));
            Assert.That(aes.KeySize, Is.EqualTo(256));
            Assert.That(aes.BlockSize, Is.EqualTo(16));
            Assert.That(aes.TagSizeInBytes, Is.EqualTo(0));
            Assert.That(aes.IsAuthenticatedEncryption, Is.False);
        }

        [Test]
        public void Properties_GcmMode_ShouldReturnCorrectValues()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.GCM, PaddingMode.None);

            // Assert
            Assert.That(aes.AlgorithmName, Is.EqualTo("AES-GCM"));
            Assert.That(aes.TagSizeInBytes, Is.EqualTo(16));
            Assert.That(aes.IsAuthenticatedEncryption, Is.True);
        }

        #endregion

        #region 遗留方法测试

        [Test]
        public void EncryptDecryptWithoutIV_ECB_ShouldWork()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.ECB);

            // Act
#pragma warning disable CS0618 // 类型或成员已过时
            var ciphertext = aes.EncryptWithoutIV_ECB(TestPlaintext);
            var plaintext = aes.DecryptWithoutIV_ECB(ciphertext);
#pragma warning restore CS0618 // 类型或成员已过时

            // Assert
            Assert.That(plaintext, Is.EqualTo(TestPlaintext));
        }

        [Test]
        public void EncryptWithoutIV_ECB_InNonEcbMode_ShouldThrow()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256, AesCrypto.ExtendedCipherMode.CBC);

            // Act & Assert
#pragma warning disable CS0618 // 类型或成员已过时
            Assert.Throws<InvalidOperationException>(() => aes.EncryptWithoutIV_ECB(TestPlaintext));
#pragma warning restore CS0618 // 类型或成员已过时
        }

        #endregion

        #region 性能测试

        [Test]
        [Timeout(2000)] // 设置2秒超时
        public void Encrypt_LargeData_ShouldCompleteQuickly()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var largeData = new byte[1024 * 1024]; // 1MB
            RandomNumberGenerator.Fill(largeData);

            // Act - 这应该在2秒内完成
            var ciphertext = aes.Encrypt(largeData, iv);

            // Assert - 如果测试通过，说明性能可以接受
            Assert.That(ciphertext, Is.Not.Null);
        }

        #endregion

        #region 多线程测试

        [Test]
        public void EncryptDecrypt_Multithreaded_ShouldBeThreadSafe()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var testData = new byte[100];
            RandomNumberGenerator.Fill(testData);

            const int threadCount = 10;
            const int iterationsPerThread = 100;
            var exceptions = new ConcurrentBag<Exception>();

            // Act
            Parallel.For(0, threadCount, i =>
            {
                try
                {
                    for (int j = 0; j < iterationsPerThread; j++)
                    {
                        var ciphertext = aes.Encrypt(testData, iv);
                        var plaintext = aes.Decrypt(ciphertext, iv);
                        Assert.That(plaintext, Is.EqualTo(testData));
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            // Assert
            Assert.That(exceptions, Is.Empty,
                $"Found {exceptions.Count} exceptions in multithreaded test");
        }

        #endregion

        #region 边界条件测试

        [Test]
        public void Encrypt_EmptyData_ShouldWork()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var emptyData = Array.Empty<byte>();

            // Act
            var ciphertext = aes.Encrypt(emptyData, iv);
            var plaintext = aes.Decrypt(ciphertext, iv);

            // Assert - 修复：对于空数据，加密后可能仍有填充块
            Assert.That(plaintext, Is.EqualTo(emptyData));
        }

        [Test]
        public void Encrypt_SmallData_ShouldWork()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var smallData = new byte[] { 0x01, 0x02, 0x03 };

            // Act
            var ciphertext = aes.Encrypt(smallData, iv);
            var plaintext = aes.Decrypt(ciphertext, iv);

            // Assert
            Assert.That(plaintext, Is.EqualTo(smallData));
        }

        [Test]
        public void Encrypt_ExactlyOneBlock_ShouldWork()
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var oneBlockData = new byte[16]; // AES块大小
            RandomNumberGenerator.Fill(oneBlockData);

            // Act
            var ciphertext = aes.Encrypt(oneBlockData, iv);
            var plaintext = aes.Decrypt(ciphertext, iv);

            // Assert
            Assert.That(plaintext, Is.EqualTo(oneBlockData));
        }

        [Test]
        [TestCase(1)]    // 1字节
        [TestCase(15)]   // 比块少1字节
        [TestCase(17)]   // 比块多1字节
        [TestCase(31)]   // 奇数长度
        [TestCase(1023)] // 较大的非对齐长度
        public void Encrypt_VariousDataLengths_ShouldWork(int length)
        {
            // Arrange
            using var aes = new AesCrypto(TestKey256);
            var iv = aes.GenerateIV();
            var data = new byte[length];
            RandomNumberGenerator.Fill(data);

            // Act
            var ciphertext = aes.Encrypt(data, iv);
            var plaintext = aes.Decrypt(ciphertext, iv);

            // Assert
            Assert.That(plaintext, Is.EqualTo(data));
        }

        #endregion

        #region 辅助方法测试

        [Test]
        public void CreateFromHexKey_WithInvalidHex_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidHex = "GGGG"; // 无效的十六进制字符

            // Act & Assert
            Assert.Throws<FormatException>(() => AesCrypto.CreateFromHexKey(invalidHex));
        }

        [Test]
        public void CreateFromBase64Key_WithInvalidBase64_ShouldThrowFormatException()
        {
            // Arrange
            var invalidBase64 = "NotValidBase64!!"; // 无效的Base64

            // Act & Assert
            Assert.Throws<FormatException>(() => AesCrypto.CreateFromBase64Key(invalidBase64));
        }

        #endregion
    }

    /// <summary>
    /// 设置测试
    /// </summary>
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // 全局测试设置
            TestContext.WriteLine("AesCryptoTests 测试套件开始运行");
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // 全局测试清理
            TestContext.WriteLine("AesCryptoTests 测试套件运行完成");
        }
    }

    /// <summary>
    /// 性能测试类
    /// </summary>
    [TestFixture]
    [Category("Performance")]
    public class AesCryptoPerformanceTests
    {
        [Test]
        [Category("Performance")]
        [MaxTime(5000)] // 最大执行时间5秒
        public void Performance_EncryptDecrypt_1MB_ShouldBeFast()
        {
            // Arrange
            using var aes = new AesCrypto(new byte[32]);
            var iv = aes.GenerateIV();
            var data = new byte[1024 * 1024]; // 1MB
            RandomNumberGenerator.Fill(data);

            // Act & Assert - 测试多次以确保性能稳定
            for (int i = 0; i < 10; i++)
            {
                var ciphertext = aes.Encrypt(data, iv);
                var plaintext = aes.Decrypt(ciphertext, iv);
                Assert.That(plaintext, Is.EqualTo(data));
            }
        }

        [Test]
        [Category("Performance")]
        public void Performance_StreamEncryption_10MB_ShouldBeFast()
        {
            // Arrange
            using var aes = new AesCrypto(new byte[32]);
            var iv = aes.GenerateIV();
            var data = new byte[10 * 1024 * 1024]; // 10MB
            RandomNumberGenerator.Fill(data);

            using var inputStream = new MemoryStream(data);
            using var encryptedStream = new MemoryStream();
            using var decryptedStream = new MemoryStream();

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            aes.EncryptAsync(inputStream, encryptedStream, iv).Wait();
            encryptedStream.Position = 0;
            aes.DecryptAsync(encryptedStream, decryptedStream, iv).Wait();

            stopwatch.Stop();

            // Assert
            Assert.That(decryptedStream.ToArray(), Is.EqualTo(data));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(10000),
                $"10MB流加密解密耗时{stopwatch.ElapsedMilliseconds}ms，超过10秒");
        }
    }

    /// <summary>
    /// 集成测试类
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    public class AesCryptoIntegrationTests
    {
        [Test]
        [Category("Integration")]
        public void Integration_FileEncryptionDecryption_ShouldWork()
        {
            // Arrange
            var originalText = "这是一个测试文件的内容，用于测试AES加密解密功能。\n包含多行文本和特殊字符：!@#$%^&*()\n中文字符：测试加密";
            var tempFile = Path.GetTempFileName();
            var encryptedFile = Path.GetTempFileName();
            var decryptedFile = Path.GetTempFileName();

            try
            {
                // 写入原始文件
                File.WriteAllText(tempFile, originalText, Encoding.UTF8);

                // 创建加密实例
                using var aes = AesCrypto.CreateRandom(256);
                var iv = aes.GenerateIV();

                // Act - 加密文件
                using (var inputStream = File.OpenRead(tempFile))
                using (var outputStream = File.Create(encryptedFile))
                {
                    aes.EncryptAsync(inputStream, outputStream, iv).Wait();
                }

                // Act - 解密文件
                using (var inputStream = File.OpenRead(encryptedFile))
                using (var outputStream = File.Create(decryptedFile))
                {
                    aes.DecryptAsync(inputStream, outputStream, iv).Wait();
                }

                // Assert
                var decryptedText = File.ReadAllText(decryptedFile, Encoding.UTF8);
                Assert.That(decryptedText, Is.EqualTo(originalText));
            }
            finally
            {
                // 清理临时文件
                CleanupTempFile(tempFile);
                CleanupTempFile(encryptedFile);
                CleanupTempFile(decryptedFile);
            }
        }

        private void CleanupTempFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch
            {
                // 忽略清理错误
            }
        }

        [Test]
        [Category("Integration")]
        public void Integration_MultipleEncryptionFormats_ShouldBeConsistent()
        {
            // Arrange
            using var aes = AesCrypto.CreateRandom(256);
            var iv = aes.GenerateIV();
            var originalText = "统一测试数据";

            // Act - 使用不同格式加密
            var ciphertextBytes = aes.Encrypt(Encoding.UTF8.GetBytes(originalText), iv);
            var ciphertextBase64 = aes.EncryptToBase64(originalText, iv);
            var ciphertextHex = aes.EncryptToHex(originalText, iv);

            // 将Base64和Hex转换回字节数组进行比较
            var ciphertextFromBase64 = Convert.FromBase64String(ciphertextBase64);
            var ciphertextFromHex = Enumerable.Range(0, ciphertextHex.Length / 2)
                .Select(x => Convert.ToByte(ciphertextHex.Substring(x * 2, 2), 16))
                .ToArray();

            // Assert - 所有格式应该产生相同的密文
            Assert.That(ciphertextFromBase64, Is.EqualTo(ciphertextBytes));
            Assert.That(ciphertextFromHex, Is.EqualTo(ciphertextBytes));
        }
    }

    /// <summary>
    /// 参数化测试示例
    /// </summary>
    [TestFixture]
    public class AesCryptoParameterizedTests
    {
        private static IEnumerable<TestCaseData> EncryptionModesTestCases
        {
            get
            {
                yield return new TestCaseData(AesCrypto.ExtendedCipherMode.CBC, PaddingMode.PKCS7)
                    .SetName("CBC_PKCS7");
                yield return new TestCaseData(AesCrypto.ExtendedCipherMode.CFB, PaddingMode.PKCS7)
                    .SetName("CFB_PKCS7");
                yield return new TestCaseData(AesCrypto.ExtendedCipherMode.ECB, PaddingMode.PKCS7)
                    .SetName("ECB_PKCS7");
            }
        }

        [Test]
        [TestCaseSource(nameof(EncryptionModesTestCases))]
        public void Parameterized_EncryptDecrypt_AllModes_ShouldWork(
            AesCrypto.ExtendedCipherMode mode,
            PaddingMode padding)
        {
            // Arrange
            var key = new byte[32];
            RandomNumberGenerator.Fill(key);
            using var aes = new AesCrypto(key, mode, padding);
            var iv = aes.GenerateIV();
            var data = new byte[100];
            RandomNumberGenerator.Fill(data);

            // Act
            var ciphertext = aes.Encrypt(data, iv);
            var plaintext = aes.Decrypt(ciphertext, iv);

            // Assert
            Assert.That(plaintext, Is.EqualTo(data));
        }

        private static IEnumerable<TestCaseData> KeySizeTestCases
        {
            get
            {
                yield return new TestCaseData(128).SetName("KeySize_128");
                yield return new TestCaseData(192).SetName("KeySize_192");
                yield return new TestCaseData(256).SetName("KeySize_256");
            }
        }

        [Test]
        [TestCaseSource(nameof(KeySizeTestCases))]
        public void Parameterized_DifferentKeySizes_ShouldWork(int keySizeBits)
        {
            // Arrange
            using var aes = AesCrypto.CreateRandom(keySizeBits);
            var iv = aes.GenerateIV();
            var data = Encoding.UTF8.GetBytes($"Test data for {keySizeBits}-bit key");

            // Act
            var ciphertext = aes.Encrypt(data, iv);
            var plaintext = aes.Decrypt(ciphertext, iv);

            // Assert
            Assert.That(plaintext, Is.EqualTo(data));
            Assert.That(aes.KeySize, Is.EqualTo(keySizeBits));
        }
    }
}
