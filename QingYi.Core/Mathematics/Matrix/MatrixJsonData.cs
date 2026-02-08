#if NET8_0_OR_GREATER
namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// JSON data structure for matrix serialization
    /// </summary>
    internal class MatrixJsonData
    {
        public int Rows { get; set; }
        public int Cols { get; set; }
        public decimal[] Data { get; set; }
    }
}
#endif
