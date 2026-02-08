#if NET8_0_OR_GREATER
using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// Matrix operations class with SIMD optimizations
    /// </summary>
    internal static class MatrixOperations
    {
        // SIMD-optimized addition for non-decimal types
        public static unsafe void AddSimd(Span<decimal> left, Span<decimal> right, Span<decimal> result, Type elementType)
        {
            // For non-decimal types, we can use SIMD instructions
            if (elementType == typeof(double))
            {
                fixed (decimal* leftPtr = left, rightPtr = right, resultPtr = result)
                {
                    double* leftDouble = (double*)leftPtr;
                    double* rightDouble = (double*)rightPtr;
                    double* resultDouble = (double*)resultPtr;

                    int i = 0;
                    int length = left.Length;

                    if (Avx2.IsSupported && length >= 4)
                    {
                        for (; i <= length - 4; i += 4)
                        {
                            var leftVec = Avx.LoadVector256(leftDouble + i);
                            var rightVec = Avx.LoadVector256(rightDouble + i);
                            var resultVec = Avx.Add(leftVec, rightVec);
                            Avx.Store(resultDouble + i, resultVec);
                        }
                    }

                    // Process remaining elements
                    for (; i < length; i++)
                    {
                        resultDouble[i] = leftDouble[i] + rightDouble[i];
                    }
                }
            }
            else if (elementType == typeof(float))
            {
                fixed (decimal* leftPtr = left, rightPtr = right, resultPtr = result)
                {
                    float* leftFloat = (float*)leftPtr;
                    float* rightFloat = (float*)rightPtr;
                    float* resultFloat = (float*)resultPtr;

                    int i = 0;
                    int length = left.Length;

                    if (Avx2.IsSupported && length >= 8)
                    {
                        for (; i <= length - 8; i += 8)
                        {
                            var leftVec = Avx.LoadVector256(leftFloat + i);
                            var rightVec = Avx.LoadVector256(rightFloat + i);
                            var resultVec = Avx.Add(leftVec, rightVec);
                            Avx.Store(resultFloat + i, resultVec);
                        }
                    }
                    else if (Sse2.IsSupported && length >= 4)
                    {
                        for (; i <= length - 4; i += 4)
                        {
                            var leftVec = Sse.LoadVector128(leftFloat + i);
                            var rightVec = Sse.LoadVector128(rightFloat + i);
                            var resultVec = Sse.Add(leftVec, rightVec);
                            Sse.Store(resultFloat + i, resultVec);
                        }
                    }

                    // Process remaining elements
                    for (; i < length; i++)
                    {
                        resultFloat[i] = leftFloat[i] + rightFloat[i];
                    }
                }
            }
            else
            {
                // For other types, use standard addition
                for (int i = 0; i < left.Length; i++)
                {
                    result[i] = left[i] + right[i];
                }
            }
        }

        // Standard matrix multiplication
        public static void MultiplyStandard<T>(Matrix<T> left, Matrix<T> right, Matrix<T> result) where T : unmanaged, INumber<T>
        {
            Parallel.For(0, left.Rows, i =>
            {
                for (int j = 0; j < right.Cols; j++)
                {
                    decimal sum = 0m;
                    for (int k = 0; k < left.Cols; k++)
                    {
                        sum += left.GetDecimal(i, k) * right.GetDecimal(k, j);
                    }
                    result.SetDecimal(i, j, sum);
                }
            });
        }

        // SIMD-optimized matrix multiplication for double
        public static unsafe void MultiplyDoubleSimd<T>(Matrix<T> left, Matrix<T> right, Matrix<T> result) where T : unmanaged, INumber<T>
        {
            Parallel.For(0, left.Rows, i =>
            {
                var leftData = left.Data.Span;
                var rightData = right.Data.Span;
                var resultData = result.Data.Span;

                fixed (decimal* leftPtr = leftData, rightPtr = rightData, resultPtr = resultData)
                {
                    double* leftDouble = (double*)leftPtr;
                    double* rightDouble = (double*)rightPtr;
                    double* resultDouble = (double*)resultPtr;

                    for (int j = 0; j < right.Cols; j++)
                    {
                        double sum = 0.0;
                        int k = 0;

                        if (Avx2.IsSupported && left.Cols >= 4)
                        {
                            var sumVec = Vector256<double>.Zero;

                            for (; k <= left.Cols - 4; k += 4)
                            {
                                var leftVec = Avx.LoadVector256(leftDouble + i * left.Cols + k);
                                var rightVec = Avx.LoadVector256(rightDouble + k * right.Cols + j);
                                sumVec = Avx.Add(sumVec, Avx.Multiply(leftVec, rightVec));
                            }

                            // Horizontal add
                            sum += sumVec.GetElement(0) + sumVec.GetElement(1) + sumVec.GetElement(2) + sumVec.GetElement(3);
                        }

                        // Process remaining elements
                        for (; k < left.Cols; k++)
                        {
                            sum += leftDouble[i * left.Cols + k] * rightDouble[k * right.Cols + j];
                        }

                        resultDouble[i * result.Cols + j] = sum;
                    }
                }
            });
        }

        // SIMD-optimized matrix multiplication for float
        public static unsafe void MultiplyFloatSimd<T>(Matrix<T> left, Matrix<T> right, Matrix<T> result) where T : unmanaged, INumber<T>
        {
            Parallel.For(0, left.Rows, i =>
            {
                var leftData = left.Data.Span;
                var rightData = right.Data.Span;
                var resultData = result.Data.Span;

                fixed (decimal* leftPtr = leftData, rightPtr = rightData, resultPtr = resultData)
                {
                    float* leftFloat = (float*)leftPtr;
                    float* rightFloat = (float*)rightPtr;
                    float* resultFloat = (float*)resultPtr;

                    for (int j = 0; j < right.Cols; j++)
                    {
                        float sum = 0.0f;
                        int k = 0;

                        if (Avx2.IsSupported && left.Cols >= 8)
                        {
                            var sumVec = Vector256<float>.Zero;

                            for (; k <= left.Cols - 8; k += 8)
                            {
                                var leftVec = Avx.LoadVector256(leftFloat + i * left.Cols + k);
                                var rightVec = Avx.LoadVector256(rightFloat + k * right.Cols + j);
                                sumVec = Avx.Add(sumVec, Avx.Multiply(leftVec, rightVec));
                            }

                            // Horizontal add
                            sum += sumVec.GetElement(0) + sumVec.GetElement(1) + sumVec.GetElement(2) + sumVec.GetElement(3) +
                                   sumVec.GetElement(4) + sumVec.GetElement(5) + sumVec.GetElement(6) + sumVec.GetElement(7);
                        }
                        else if (Sse2.IsSupported && left.Cols >= 4)
                        {
                            var sumVec = Vector128<float>.Zero;

                            for (; k <= left.Cols - 4; k += 4)
                            {
                                var leftVec = Sse.LoadVector128(leftFloat + i * left.Cols + k);
                                var rightVec = Sse.LoadVector128(rightFloat + k * right.Cols + j);
                                sumVec = Sse.Add(sumVec, Sse.Multiply(leftVec, rightVec));
                            }

                            // Horizontal add
                            sum += sumVec.GetElement(0) + sumVec.GetElement(1) + sumVec.GetElement(2) + sumVec.GetElement(3);
                        }

                        // Process remaining elements
                        for (; k < left.Cols; k++)
                        {
                            sum += leftFloat[i * left.Cols + k] * rightFloat[k * right.Cols + j];
                        }

                        resultFloat[i * result.Cols + j] = sum;
                    }
                }
            });
        }
    }
}
#endif
