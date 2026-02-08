#if NET8_0_OR_GREATER
using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// Matrix operations extension methods for SIMD operations
    /// </summary>
    internal static class MatrixOperationsExtensions
    {
        // SIMD-optimized scalar multiplication for non-decimal types
        public static unsafe void MultiplySimd(Span<decimal> matrix, decimal scalar, Span<decimal> result, Type elementType)
        {
            // For non-decimal types, we can use SIMD instructions
            if (elementType == typeof(double))
            {
                double scalarDbl = (double)scalar;
                fixed (decimal* matrixPtr = matrix, resultPtr = result)
                {
                    double* matrixDouble = (double*)matrixPtr;
                    double* resultDouble = (double*)resultPtr;

                    int i = 0;
                    int length = matrix.Length;

                    if (Avx2.IsSupported && length >= 4)
                    {
                        var scalarVec = Vector256.Create(scalarDbl);

                        for (; i <= length - 4; i += 4)
                        {
                            var matrixVec = Avx.LoadVector256(matrixDouble + i);
                            var resultVec = Avx.Multiply(matrixVec, scalarVec);
                            Avx.Store(resultDouble + i, resultVec);
                        }
                    }

                    // Process remaining elements
                    for (; i < length; i++)
                    {
                        resultDouble[i] = matrixDouble[i] * scalarDbl;
                    }
                }
            }
            else if (elementType == typeof(float))
            {
                float scalarFlt = (float)scalar;
                fixed (decimal* matrixPtr = matrix, resultPtr = result)
                {
                    float* matrixFloat = (float*)matrixPtr;
                    float* resultFloat = (float*)resultPtr;

                    int i = 0;
                    int length = matrix.Length;

                    if (Avx2.IsSupported && length >= 8)
                    {
                        var scalarVec = Vector256.Create(scalarFlt);

                        for (; i <= length - 8; i += 8)
                        {
                            var matrixVec = Avx.LoadVector256(matrixFloat + i);
                            var resultVec = Avx.Multiply(matrixVec, scalarVec);
                            Avx.Store(resultFloat + i, resultVec);
                        }
                    }
                    else if (Sse2.IsSupported && length >= 4)
                    {
                        var scalarVec = Vector128.Create(scalarFlt);

                        for (; i <= length - 4; i += 4)
                        {
                            var matrixVec = Sse.LoadVector128(matrixFloat + i);
                            var resultVec = Sse.Multiply(matrixVec, scalarVec);
                            Sse.Store(resultFloat + i, resultVec);
                        }
                    }

                    // Process remaining elements
                    for (; i < length; i++)
                    {
                        resultFloat[i] = matrixFloat[i] * scalarFlt;
                    }
                }
            }
            else
            {
                // For other types, use standard multiplication
                for (int i = 0; i < matrix.Length; i++)
                {
                    result[i] = matrix[i] * scalar;
                }
            }
        }
    }
}
#endif
