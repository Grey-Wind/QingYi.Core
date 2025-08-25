#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// JSON data structure for matrix serialization
    /// </summary>
    /// <typeparam name="T">Numeric type</typeparam>
    internal class MatrixJsonData<T> where T : unmanaged, INumber<T>
    {
        public int Rows { get; set; }
        public int Cols { get; set; }
        public T[] Data { get; set; }
    }
}
#endif
