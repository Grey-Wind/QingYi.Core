#if NET8_0_OR_GREATER
using System;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// Matrix operation extension methods
    /// </summary>
    public static class MatrixExtensions
    {
        /// <summary>
        /// Scalar multiplication
        /// </summary>
        /// <typeparam name="T">Numeric type</typeparam>
        /// <param name="matrix">Matrix</param>
        /// <param name="scalar">Scalar value</param>
        /// <returns>Result matrix</returns>
        public static Matrix<T> Multiply<T>(this Matrix<T> matrix, T scalar) where T : unmanaged, INumber<T>
        {
            var result = new Matrix<T>(matrix.Rows, matrix.Cols);
            var resultData = result.Data.Span;
            var matrixData = matrix.Data.Span;
            decimal scalarDec = Convert.ToDecimal(scalar);

            // Use SIMD for non-decimal types if available
            if (typeof(T) != typeof(decimal) && Avx2.IsSupported && matrixData.Length >= 8)
            {
                // Use SIMD-optimized multiplication for non-decimal types
                MatrixOperationsExtensions.MultiplySimd(matrixData, scalarDec, resultData, typeof(T));
            }
            else
            {
                // Use standard multiplication for decimal or when SIMD is not available
                for (int i = 0; i < matrixData.Length; i++)
                {
                    resultData[i] = matrixData[i] * scalarDec;
                }
            }

            return result;
        }

        /// <summary>
        /// Matrix inversion (square matrices only)
        /// </summary>
        /// <typeparam name="T">Numeric type</typeparam>
        /// <param name="matrix">Matrix</param>
        /// <returns>Inverse matrix</returns>
        public static Matrix<double> Inverse<T>(this Matrix<T> matrix) where T : unmanaged, INumber<T>
        {
            if (matrix.Rows != matrix.Cols)
                throw new InvalidOperationException("Only square matrices can be inverted");

            // Implement Gauss-Jordan elimination for inversion
            // Note: This is a simplified implementation, production should use more stable algorithms
            int n = matrix.Rows;
            var augmented = new decimal[n, 2 * n];
            var result = new Matrix<double>(n, n);

            // Create augmented matrix [A|I]
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i, j] = matrix.GetDecimal(i, j);
                    augmented[i, j + n] = (i == j) ? 1m : 0m;
                }
            }

            // Gauss-Jordan elimination
            for (int i = 0; i < n; i++)
            {
                // Find pivot
                if (Math.Abs(augmented[i, i]) < 1e-28m)
                {
                    for (int k = i + 1; k < n; k++)
                    {
                        if (Math.Abs(augmented[k, i]) > 1e-28m)
                        {
                            for (int j = 0; j < 2 * n; j++)
                            {
                                (augmented[i, j], augmented[k, j]) = (augmented[k, j], augmented[i, j]);
                            }
                            break;
                        }
                    }
                }

                decimal divisor = augmented[i, i];
                if (Math.Abs(divisor) < 1e-28m)
                    throw new InvalidOperationException("Matrix is not invertible");

                for (int j = 0; j < 2 * n; j++)
                {
                    augmented[i, j] /= divisor;
                }

                for (int k = 0; k < n; k++)
                {
                    if (k != i)
                    {
                        decimal factor = augmented[k, i];
                        for (int j = 0; j < 2 * n; j++)
                        {
                            augmented[k, j] -= factor * augmented[i, j];
                        }
                    }
                }
            }

            // Extract the inverse matrix part
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    result.SetDecimal(i, j, augmented[i, j + n]);
                }
            }

            return result;
        }
    }

}
#endif
