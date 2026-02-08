using System.IO;
using System.IO.Compression;

namespace QingYi.Core.Compression
{
    public class GZipCompression : CompressionBase
    {
        public override string AlgorithmName => "GZip";

        public override byte[] Compress(byte[] input)
        {
            using var outputStream = new MemoryStream();
            using (var compressionStream = new GZipStream(outputStream, Level, leaveOpen: true))
            {
                compressionStream.Write(input, 0, input.Length);
            }
            return outputStream.ToArray();
        }

        public override byte[] Decompress(byte[] compressedData)
        {
            using var inputStream = new MemoryStream(compressedData);
            using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
            using var outputStream = new MemoryStream();

            decompressionStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }

        public override void CompressStream(Stream inputStream, Stream outputStream)
        {
            using var compressionStream = new GZipStream(outputStream, Level, leaveOpen: true);
            inputStream.CopyTo(compressionStream);
        }

        public override void DecompressStream(Stream compressedStream, Stream outputStream)
        {
            using var decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress);
            decompressionStream.CopyTo(outputStream);
        }

        protected override Stream CreateCompressionStream(Stream outputStream)
        {
            return new GZipStream(outputStream, Level, leaveOpen: true);
        }

        protected override Stream CreateDecompressionStream(Stream inputStream)
        {
            return new GZipStream(inputStream, CompressionMode.Decompress);
        }
    }
    }
