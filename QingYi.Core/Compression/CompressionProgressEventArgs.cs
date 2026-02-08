using System;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 压缩进度事件参数
    /// </summary>
    public class CompressionProgressEventArgs : EventArgs
    {
        public string? OperationId { get; init; }
        public long ProcessedBytes { get; init; }
        public long TotalBytes { get; init; }
        public double Percentage => TotalBytes > 0 ?
            (double)ProcessedBytes / TotalBytes * 100 : 0;
        public CompressionOperation Operation { get; init; }
    }
}
