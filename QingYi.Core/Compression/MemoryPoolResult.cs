using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QingYi.Core.Compression
{
    /// <summary>
    /// 内存池结果
    /// </summary>
    public readonly struct MemoryPoolResult : IDisposable
    {
        private readonly IMemoryOwner<byte>? _memoryOwner;

        public ReadOnlyMemory<byte> Data { get; }
        public int Length { get; }

        public MemoryPoolResult(IMemoryOwner<byte> memoryOwner, int length)
        {
            _memoryOwner = memoryOwner;
            Data = memoryOwner.Memory[..length];
            Length = length;
        }

        public void Dispose()
        {
            _memoryOwner?.Dispose();
        }

        public byte[] ToArray() => Data.ToArray();
    }
}
