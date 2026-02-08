using System;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 文件压缩结果
    /// </summary>
    public class FileCompressionResult
    {
        public string SourceFile { get; init; } = string.Empty;
        public string DestinationFile { get; init; } = string.Empty;
        public bool Success { get; init; }
        public CompressionStatistics Statistics { get; init; }
        public Exception? Error { get; init; }
    }
}
