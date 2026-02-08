using System;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 压缩结果
    /// </summary>
    public class CompressionResult<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public CompressionStatistics Statistics { get; init; }
        public Exception? Error { get; init; }
        public TimeSpan ElapsedTime { get; init; }

        public static CompressionResult<T> CreateSuccess(T data, CompressionStatistics statistics, TimeSpan elapsedTime) =>
            new() { Success = true, Data = data, Statistics = statistics, ElapsedTime = elapsedTime };

        public static CompressionResult<T> CreateFailure(Exception error, TimeSpan elapsedTime) =>
            new() { Success = false, Error = error, ElapsedTime = elapsedTime };
    }
}
