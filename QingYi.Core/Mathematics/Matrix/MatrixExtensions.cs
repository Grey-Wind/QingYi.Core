#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

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
            var resultSpan = result.Data.Span;
            var matrixSpan = matrix.Data.Span;

            for (int i = 0; i < matrixSpan.Length; i++)
            {
                resultSpan[i] = matrixSpan[i] * scalar;
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
            var augmented = new double[n, 2 * n];
            var result = new Matrix<double>(n, n);

            // Create augmented matrix [A|I]
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmented[i, j] = Convert.ToDouble(matrix[i, j]);
                    augmented[i, j + n] = (i == j) ? 1.0 : 0.0;
                }
            }

            // Gauss-Jordan elimination
            for (int i = 0; i < n; i++)
            {
                // Find pivot
                if (Math.Abs(augmented[i, i]) < 1e-10)
                {
                    for (int k = i + 1; k < n; k++)
                    {
                        if (Math.Abs(augmented[k, i]) > 1e-10)
                        {
                            for (int j = 0; j < 2 * n; j++)
                            {
                                (augmented[i, j], augmented[k, j]) = (augmented[k, j], augmented[i, j]);
                            }
                            break;
                        }
                    }
                }

                double divisor = augmented[i, i];
                if (Math.Abs(divisor) < 1e-10)
                    throw new InvalidOperationException("Matrix is not invertible");

                for (int j = 0; j < 2 * n; j++)
                {
                    augmented[i, j] /= divisor;
                }

                for (int k = 0; k < n; k++)
                {
                    if (k != i)
                    {
                        double factor = augmented[k, i];
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
                    result[i, j] = augmented[i, j + n];
                }
            }

            return result;
        }
    }
}
#endif
