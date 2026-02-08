using QingYi.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QingYi.Core.Compression
{
    public static class CompressionFactory
    {
        private static readonly Dictionary<string, Func<ICompression>> _compressors = new()
        {
            ["gzip"] = () => new GZipCompression(),
            ["brotli"] = () => new BrotliCompression()
        };

        public static ICompression Create(string algorithmName)
        {
            algorithmName = algorithmName.ToLowerInvariant();

            if (_compressors.TryGetValue(algorithmName, out var factory))
            {
                return factory();
            }

            throw new NotSupportedException($"不支持的压缩算法: {algorithmName}");
        }

        public static void Register(string algorithmName, Func<ICompression> factory)
        {
            _compressors[algorithmName.ToLowerInvariant()] = factory;
        }

        public static IEnumerable<string> GetSupportedAlgorithms()
        {
            return _compressors.Keys;
        }
    }
}
