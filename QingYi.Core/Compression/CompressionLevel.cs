namespace QingYi.Core.Compression
{
    /// <summary>
    /// 压缩级别
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>最快压缩</summary>
        Fastest = 0,

        /// <summary>无压缩</summary>
        NoCompression = 1,

        /// <summary>最优压缩</summary>
        Optimal = 2,

        /// <summary>最小大小</summary>
        SmallestSize = 3
    }
}
