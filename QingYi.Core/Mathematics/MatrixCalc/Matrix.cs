namespace QingYi.Core.Mathematics.MatrixCalc
{
    /// <summary>
    /// Represents a mathematical matrix and provides various matrix operations.<br />
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// Gets the number of rows in the matrix.<br />
        /// </summary>
        public int Rows { get; private set; }

        /// <summary>
        /// Gets the number of columns in the matrix.<br />
        /// </summary>
        public int Cols { get; private set; }

        /// <summary>
        /// Gets the data of the matrix in a 2D array.<br />
        /// </summary>
        public double[,] Data { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class with the specified dimensions and data.<br />
        /// </summary>
        /// <param name="rows">The number of rows in the matrix.<br /></param>
        /// <param name="cols">The number of columns in the matrix.<br /></param>
        /// <param name="data">The data to populate the matrix with.<br /></param>
        public Matrix(int rows, int cols, double[,] data)
        {
            Rows = rows;
            Cols = cols;
            Data = data;
        }

        /// <summary>
        /// Adds a scalar value to the matrix.<br />
        /// </summary>
        /// <param name="a">The matrix to which the value is added.<br /></param>
        /// <param name="value">The scalar value to add to each element of the matrix.<br /></param>
        /// <returns>A new matrix with the scalar value added to each element.<br /></returns>
        public static Matrix operator +(Matrix a, int value) => MatrixIntOp.Add(a, value);

        /// <summary>
        /// Subtracts a scalar value from the matrix.<br />
        /// </summary>
        /// <param name="a">The matrix from which the value is subtracted.<br /></param>
        /// <param name="value">The scalar value to subtract from each element of the matrix.<br /></param>
        /// <returns>A new matrix with the scalar value subtracted from each element.<br /></returns>
        public static Matrix operator -(Matrix a, int value) => MatrixIntOp.Subtract(a, value);

        /// <summary>
        /// Multiplies the matrix by a scalar value.<br />
        /// </summary>
        /// <param name="a">The matrix to multiply.<br /></param>
        /// <param name="value">The scalar value to multiply with each element of the matrix.<br /></param>
        /// <returns>A new matrix with each element multiplied by the scalar value.<br /></returns>
        public static Matrix operator *(Matrix a, int value) => MatrixIntOp.Multiply(a, value);

        /// <summary>
        /// Divides the matrix by a scalar value.<br />
        /// </summary>
        /// <param name="a">The matrix to divide.<br /></param>
        /// <param name="value">The scalar value to divide each element of the matrix by.<br /></param>
        /// <returns>A new matrix with each element divided by the scalar value.<br /></returns>
        public static Matrix operator /(Matrix a, int value) => MatrixIntOp.Divide(a, value);

        /// <summary>
        /// Adds two matrices together.<br />
        /// </summary>
        /// <param name="a">The first matrix to add.<br /></param>
        /// <param name="b">The second matrix to add.<br /></param>
        /// <returns>A new matrix that is the result of adding the two matrices together.<br /></returns>
        public static Matrix operator +(Matrix a, Matrix b) => MatrixMatrixOp.Add(a, b);

        /// <summary>
        /// Subtracts one matrix from another.<br />
        /// </summary>
        /// <param name="a">The matrix to subtract from.<br /></param>
        /// <param name="b">The matrix to subtract.<br /></param>
        /// <returns>A new matrix that is the result of subtracting matrix b from matrix a.<br /></returns>
        public static Matrix operator -(Matrix a, Matrix b) => MatrixMatrixOp.Subtract(a, b);

        /// <summary>
        /// Multiplies two matrices together.<br />
        /// </summary>
        /// <param name="a">The first matrix to multiply.<br /></param>
        /// <param name="b">The second matrix to multiply.<br /></param>
        /// <returns>A new matrix that is the result of multiplying the two matrices together.<br /></returns>
        public static Matrix operator *(Matrix a, Matrix b) => MatrixMatrixOp.Multiply(a, b);

        /// <summary>
        /// Multiplies a matrix by a scalar value (commutative).<br />
        /// </summary>
        /// <param name="value">The scalar value to multiply with each element of the matrix.<br /></param>
        /// <param name="m">The matrix to multiply.<br /></param>
        /// <returns>A new matrix with each element multiplied by the scalar value.<br /></returns>
        public static Matrix operator *(int value, Matrix m) => MatrixIntOp.Multiply(m, value);

        /// <summary>
        /// Divides a matrix by a scalar value (commutative).<br />
        /// </summary>
        /// <param name="value">The scalar value to divide each element of the matrix by.<br /></param>
        /// <param name="m">The matrix to divide.<br /></param>
        /// <returns>A new matrix with each element divided by the scalar value.<br /></returns>
        public static Matrix operator /(int value, Matrix m) => MatrixIntOp.Divide(m, value);
    }
}
