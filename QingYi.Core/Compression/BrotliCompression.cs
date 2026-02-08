using System.IO;
using System.IO.Compression;

namespace QingYi.Core.Compression
{
    public class BrotliCompression : CompressionBase
    {
        public override string AlgorithmName => "Brotli";

        public override byte[] Compress(byte[] input)
        {
            using var outputStream = new MemoryStream();
            using (var compressionStream = new BrotliStream(outputStream, Level, leaveOpen: true))
            {
                compressionStream.Write(input, 0, input.Length);
            }
            return outputStream.ToArray();
        }

        public override byte[] Decompress(byte[] compressedData)
        {
            using var inputStream = new MemoryStream(compressedData);
            using var decompressionStream = new BrotliStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            decompressionStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        public override void CompressStream(Stream inputStream, Stream outputStream)
        {
            using var compressionStream = new BrotliStream(outputStream, Level, leaveOpen: true);
            inputStream.CopyTo(compressionStream);
        }

        public override void DecompressStream(Stream compressedStream, Stream outputStream)
        {
            using var decompressionStream = new BrotliStream(compressedStream, CompressionMode.Decompress);
            decompressionStream.CopyTo(outputStream);
        }

        protected override Stream CreateCompressionStream(Stream outputStream)
        {
            return new BrotliStream(outputStream, Level, leaveOpen: true);
        }

        protected override Stream CreateDecompressionStream(Stream inputStream)
        {
            return new BrotliStream(inputStream, CompressionMode.Decompress);
        }
    }
}
