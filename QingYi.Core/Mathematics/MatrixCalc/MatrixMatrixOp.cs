using System;

namespace QingYi.Core.Mathematics.MatrixCalc
{
    class MatrixMatrixOp
    {
        public static Matrix Add(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Cols != b.Cols)
            {
                throw new ArgumentException("Matrices must have the same dimensions for addition.");
            }
            int rows = a.Rows;
            int cols = a.Cols;
            double[,] newData = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newData[i, j] = a.Data[i, j] + b.Data[i, j];
                }
            }
            return new Matrix(rows, cols, newData);
        }

        public static Matrix Subtract(Matrix a, Matrix b)
        {
            if (a.Rows != b.Rows || a.Cols != b.Cols)
            {
                throw new ArgumentException("Matrices must have the same dimensions for subtraction.");
            }
            int rows = a.Rows;
            int cols = a.Cols;
            double[,] newData = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newData[i, j] = a.Data[i, j] - b.Data[i, j];
                }
            }
            return new Matrix(rows, cols, newData);
        }

        public static Matrix Multiply(Matrix a, Matrix b)
        {
            if (a.Cols != b.Rows)
            {
                throw new ArgumentException("Matrix multiplication is only possible when the number of columns of the first matrix equals the number of rows of the second matrix.");
            }
            int rows = a.Rows;
            int cols = b.Cols;
            int mid = a.Cols;
            double[,] newData = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < mid; k++)
                    {
                        sum += a.Data[i, k] * b.Data[k, j];
                    }
                    newData[i, j] = sum;
                }
            }
            return new Matrix(rows, cols, newData);
        }
    }
}
