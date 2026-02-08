using QingYi.Core.Compression;

namespace QingYi.Core.Interfaces.Compression
{
    /// <summary>
    /// 压缩工厂接口
    /// </summary>
    public interface ICompressionFactory
    {
        /// <summary>
        /// 创建压缩提供程序
        /// </summary>
        /// <param name="algorithm">压缩算法</param>
        /// <param name="level">压缩级别</param>
        /// <returns>压缩提供程序实例</returns>
        ICompressionProvider CreateProvider(
            CompressionAlgorithm algorithm,
            CompressionLevel level);

        /// <summary>
        /// 创建高级压缩提供程序
        /// </summary>
        /// <param name="algorithm">压缩算法</param>
        /// <param name="level">压缩级别</param>
        /// <returns>高级压缩提供程序实例</returns>
        IAdvancedCompressionProvider CreateAdvancedProvider(
            CompressionAlgorithm algorithm,
            CompressionLevel level);
    }
}
