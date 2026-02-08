using System;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 压缩选项
    /// </summary>
    public class CompressionOptions
    {
        public CompressionLevel Level { get; set; } = CompressionLevel.Optimal;
        public int BufferSize { get; set; } = 81920; // 默认80KB
        public bool LeaveOpen { get; set; } = false;
        public bool UseAsync { get; set; } = true;

        // 高级选项
        public bool IncludeChecksum { get; set; } = true;
        public bool EnableParallelProcessing { get; set; } = false;
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    }
}
