using System;

namespace QingYi.Core.Mathematics.MatrixCalc
{
    class MatrixIntOp
    {
        public static Matrix Add(Matrix m, int value)
        {
            int rows = m.Rows;
            int cols = m.Cols;
            double[,] newData = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newData[i, j] = m.Data[i, j] + value;
                }
            }
            return new Matrix(rows, cols, newData);
        }

        public static Matrix Subtract(Matrix m, int value)
        {
            int rows = m.Rows;
            int cols = m.Cols;
            double[,] newData = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newData[i, j] = m.Data[i, j] - value;
                }
            }
            return new Matrix(rows, cols, newData);
        }

        public static Matrix Multiply(Matrix m, int value)
        {
            int rows = m.Rows;
            int cols = m.Cols;
            double[,] newData = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newData[i, j] = m.Data[i, j] * value;
                }
            }
            return new Matrix(rows, cols, newData);
        }

        public static Matrix Divide(Matrix m, int value)
        {
            if (value == 0)
            {
                throw new DivideByZeroException("Cannot divide by zero.");
            }
            int rows = m.Rows;
            int cols = m.Cols;
            double[,] newData = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    newData[i, j] = m.Data[i, j] / value;
                }
            }
            return new Matrix(rows, cols, newData);
        }
    }
}
