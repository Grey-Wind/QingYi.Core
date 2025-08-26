#if NET8_0_OR_GREATER
using System;
using System.Numerics;

namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// Matrix factory class for creating special matrices
    /// </summary>
    public static class MatrixFactory
    {
        /// <summary>
        /// Create a diagonal matrix
        /// </summary>
        /// <typeparam name="T">Numeric type</typeparam>
        /// <param name="diagonal">Diagonal elements</param>
        /// <returns>Diagonal matrix</returns>
        public static Matrix<T> Diagonal<T>(T[] diagonal) where T : unmanaged, INumber<T>
        {
            int size = diagonal.Length;
            var matrix = new Matrix<T>(size, size);

            for (int i = 0; i < size; i++)
            {
                matrix.SetDecimal(i, i, Convert.ToDecimal(diagonal[i]));
            }

            return matrix;
        }

        /// <summary>
        /// Create a random matrix
        /// </summary>
        /// <typeparam name="T">Numeric type</typeparam>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="random">Random number generator</param>
        /// <returns>Random matrix</returns>
        public static Matrix<T> Random<T>(int rows, int cols, T min, T max, Random random = null) where T : unmanaged, INumber<T>
        {
            random ??= new Random();
            var matrix = new Matrix<T>(rows, cols);

            decimal minDec = Convert.ToDecimal(min);
            decimal maxDec = Convert.ToDecimal(max);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    decimal range = maxDec - minDec;
                    decimal value = minDec + (decimal)random.NextDouble() * range;
                    matrix.SetDecimal(i, j, value);
                }
            }

            return matrix;
        }
    }
}
#endif
