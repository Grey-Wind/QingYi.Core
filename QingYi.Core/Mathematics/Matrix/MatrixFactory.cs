#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

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
                matrix[i, i] = diagonal[i];
            }

            return matrix;
        }

        /// <summary>
        /// Create a random matrix
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        /// <param name="min">Minimum value</param>
        /// <param name="max">Maximum value</param>
        /// <param name="random">Random number generator</param>
        /// <returns>Random matrix</returns>
        public static Matrix<double> Random(int rows, int cols, double min = 0, double max = 1, Random random = null)
        {
            random ??= new Random();
            var matrix = new Matrix<double>(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = min + (max - min) * random.NextDouble();
                }
            }

            return matrix;
        }
    }
}
#endif
