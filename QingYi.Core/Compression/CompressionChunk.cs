using System;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 压缩块
    /// </summary>
    public readonly struct CompressionChunk
    {
        public int Index { get; init; }
        public int TotalChunks { get; init; }
        public long OriginalSize { get; init; }
        public long CompressedSize { get; init; }
        public ReadOnlyMemory<byte> Data { get; init; }
        public byte[] Checksum { get; init; }
    }

}
