using System;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 压缩统计信息
    /// </summary>
    public readonly struct CompressionStatistics
    {
        public long OriginalSize { get; init; }
        public long CompressedSize { get; init; }
        public double CompressionRatio => OriginalSize > 0 ?
            (double)CompressedSize / OriginalSize * 100 : 0;
        public TimeSpan CompressionTime { get; init; }
        public CompressionAlgorithm Algorithm { get; init; }
        public CompressionLevel Level { get; init; }
    }
}
