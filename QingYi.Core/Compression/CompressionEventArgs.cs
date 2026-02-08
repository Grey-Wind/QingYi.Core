using System;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 压缩事件参数
    /// </summary>
    public class CompressionEventArgs : EventArgs
    {
        public string? OperationId { get; init; }
        public CompressionAlgorithm Algorithm { get; init; }
        public CompressionStatistics? Statistics { get; init; }
        public Exception? Error { get; init; }
    }
}
