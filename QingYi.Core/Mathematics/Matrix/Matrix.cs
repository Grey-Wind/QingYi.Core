#if NET8_0_OR_GREATER
using System;
using System.IO;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace QingYi.Core.Mathematics.Matrix
{
    /// <summary>
    /// High-performance matrix class supporting multiple numeric types and operations
    /// Uses decimal internally for precision, with optimized performance for all types
    /// </summary>
    /// <typeparam name="T">Numeric type (double, float, int, etc.)</typeparam>
    public sealed class Matrix<T> : IDisposable where T : unmanaged, INumber<T>
    {
        private readonly Memory<decimal> _data;
        private readonly int _rows;
        private readonly int _cols;
        private readonly bool _isPooled;

        /// <summary>
        /// Number of rows
        /// </summary>
        public int Rows => _rows;

        /// <summary>
        /// Number of columns
        /// </summary>
        public int Cols => _cols;

        /// <summary>
        /// Total number of elements in the matrix
        /// </summary>
        public int Length => _rows * _cols;

        internal Memory<decimal> Data => _data;

        /// <summary>
        /// Create a matrix from a 2D array
        /// </summary>
        /// <param name="data">2D array data</param>
        public Matrix(T[,] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            _rows = data.GetLength(0);
            _cols = data.GetLength(1);
            _isPooled = false;

            _data = new decimal[_rows * _cols];

            // Copy 2D array data to 1D storage with conversion to decimal
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    _data.Span[i * _cols + j] = Convert.ToDecimal(data[i, j]);
                }
            }
        }

        /// <summary>
        /// Create a matrix from a 2D decimal array
        /// </summary>
        /// <param name="data">2D decimal array data</param>
        public Matrix(decimal[,] data)
        {
            if (data is null)
                throw new ArgumentNullException(nameof(data));

            _rows = data.GetLength(0);
            _cols = data.GetLength(1);
            _isPooled = false;

            _data = new decimal[_rows * _cols];

            // Copy 2D array data to 1D storage
            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    _data.Span[i * _cols + j] = data[i, j];
                }
            }
        }

        /// <summary>
        /// Create a matrix of specified size
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        public Matrix(int rows, int cols)
        {
            if (rows <= 0 || cols <= 0)
                throw new ArgumentException("Rows and columns must be greater than 0");

            _rows = rows;
            _cols = cols;
            _isPooled = false;
            _data = new decimal[rows * cols];
        }

        /// <summary>
        /// Create a matrix using existing memory (high performance)
        /// </summary>
        /// <param name="data">Memory data</param>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        /// <param name="isPooled">Whether to use memory pool</param>
        internal Matrix(Memory<decimal> data, int rows, int cols, bool isPooled = false)
        {
            if (data.Length != rows * cols)
                throw new ArgumentException("Data length does not match matrix dimensions");

            _data = data;
            _rows = rows;
            _cols = cols;
            _isPooled = isPooled;
        }

        /// <summary>
        /// Indexer for accessing matrix elements
        /// </summary>
        /// <param name="row">Row index</param>
        /// <param name="col">Column index</param>
        /// <returns>Matrix element value</returns>
        public T this[int row, int col]
        {
            get
            {
                if (row < 0 || row >= _rows || col < 0 || col >= _cols)
                    throw new IndexOutOfRangeException("Index is out of matrix bounds");

                return (T)Convert.ChangeType(_data.Span[row * _cols + col], typeof(T));
            }
            set
            {
                if (row < 0 || row >= _rows || col < 0 || col >= _cols)
                    throw new IndexOutOfRangeException("Index is out of matrix bounds");

                _data.Span[row * _cols + col] = Convert.ToDecimal(value);
            }
        }

        /// <summary>
        /// Get element as decimal (avoiding conversion for internal operations)
        /// </summary>
        /// <param name="row">Row index</param>
        /// <param name="col">Column index</param>
        /// <returns>Matrix element value as decimal</returns>
        internal decimal GetDecimal(int row, int col)
        {
            if (row < 0 || row >= _rows || col < 0 || col >= _cols)
                throw new IndexOutOfRangeException("Index is out of matrix bounds");

            return _data.Span[row * _cols + col];
        }

        /// <summary>
        /// Set element as decimal (avoiding conversion for internal operations)
        /// </summary>
        /// <param name="row">Row index</param>
        /// <param name="col">Column index</param>
        /// <param name="value">Value to set</param>
        internal void SetDecimal(int row, int col, decimal value)
        {
            if (row < 0 || row >= _rows || col < 0 || col >= _cols)
                throw new IndexOutOfRangeException("Index is out of matrix bounds");

            _data.Span[row * _cols + col] = value;
        }

        /// <summary>
        /// Create a zero matrix
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        /// <returns>Zero matrix</returns>
        public static Matrix<T> Zero(int rows, int cols)
        {
            var matrix = new Matrix<T>(rows, cols);
            matrix._data.Span.Clear();
            return matrix;
        }

        /// <summary>
        /// Create an identity matrix
        /// </summary>
        /// <param name="size">Matrix size</param>
        /// <returns>Identity matrix</returns>
        public static Matrix<T> Identity(int size)
        {
            var matrix = new Matrix<T>(size, size);
            var span = matrix._data.Span;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    span[i * size + j] = i == j ? 1m : 0m;
                }
            }

            return matrix;
        }

        /// <summary>
        /// Matrix addition
        /// </summary>
        /// <param name="other">Other matrix</param>
        /// <returns>Result matrix</returns>
        public Matrix<T> Add(Matrix<T> other)
        {
            if (_rows != other._rows || _cols != other._cols)
                throw new ArgumentException("Matrix dimensions must be the same");

            var result = new Matrix<T>(_rows, _cols);
            var resultData = result._data.Span;
            var thisData = _data.Span;
            var otherData = other._data.Span;

            // Use SIMD for non-decimal types if available
            if (typeof(T) != typeof(decimal) && Avx2.IsSupported && thisData.Length >= 8)
            {
                // Use SIMD-optimized addition for non-decimal types
                MatrixOperations.AddSimd(thisData, otherData, resultData, typeof(T));
            }
            else
            {
                // Use standard addition for decimal or when SIMD is not available
                for (int i = 0; i < thisData.Length; i++)
                {
                    resultData[i] = thisData[i] + otherData[i];
                }
            }

            return result;
        }

        /// <summary>
        /// Matrix multiplication
        /// </summary>
        /// <param name="other">Other matrix</param>
        /// <returns>Result matrix</returns>
        public Matrix<T> Multiply(Matrix<T> other)
        {
            if (_cols != other._rows)
                throw new ArgumentException("The number of columns of the first matrix must equal the number of rows of the second matrix");

            var result = new Matrix<T>(_rows, other._cols);

            // Use optimized multiplication based on type
            if (typeof(T) == typeof(double) && Avx2.IsSupported)
            {
                MatrixOperations.MultiplyDoubleSimd(this, other, result);
            }
            else if (typeof(T) == typeof(float) && Avx2.IsSupported)
            {
                MatrixOperations.MultiplyFloatSimd(this, other, result);
            }
            else
            {
                // Use standard multiplication for decimal or when SIMD is not available
                MatrixOperations.MultiplyStandard(this, other, result);
            }

            return result;
        }

        /// <summary>
        /// Matrix transpose
        /// </summary>
        /// <returns>Transposed matrix</returns>
        public Matrix<T> Transpose()
        {
            var result = new Matrix<T>(_cols, _rows);

            // Use parallel transposition for large matrices
            if (_rows * _cols > 10000)
            {
                Parallel.For(0, _rows, i =>
                {
                    for (int j = 0; j < _cols; j++)
                    {
                        result.SetDecimal(j, i, GetDecimal(i, j));
                    }
                });
            }
            else
            {
                for (int i = 0; i < _rows; i++)
                {
                    for (int j = 0; j < _cols; j++)
                    {
                        result.SetDecimal(j, i, GetDecimal(i, j));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Convert to a 2D array
        /// </summary>
        /// <returns>2D array</returns>
        public T[,] ToArray()
        {
            var array = new T[_rows, _cols];
            var span = _data.Span;

            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    array[i, j] = (T)Convert.ChangeType(span[i * _cols + j], typeof(T));
                }
            }

            return array;
        }

        /// <summary>
        /// Convert to a 2D decimal array
        /// </summary>
        /// <returns>2D decimal array</returns>
        public decimal[,] ToDecimalArray()
        {
            var array = new decimal[_rows, _cols];
            var span = _data.Span;

            for (int i = 0; i < _rows; i++)
            {
                for (int j = 0; j < _cols; j++)
                {
                    array[i, j] = span[i * _cols + j];
                }
            }

            return array;
        }

        /// <summary>
        /// Convert to string representation
        /// </summary>
        /// <returns>Matrix string</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Matrix<{typeof(T).Name}>[{_rows}x{_cols}]:");

            for (int i = 0; i < Math.Min(_rows, 10); i++) // Limit display rows
            {
                for (int j = 0; j < Math.Min(_cols, 10); j++) // Limit display columns
                {
                    sb.Append($"{this[i, j]:F4}\t");
                }

                if (_cols > 10) sb.Append("...");
                sb.AppendLine();
            }

            if (_rows > 10) sb.AppendLine("...");

            return sb.ToString();
        }

        /// <summary>
        /// Export to CSV format
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="delimiter">Delimiter</param>
        public void ExportToCsv(string path, string delimiter = ",")
        {
            using var writer = new StreamWriter(path);
            var span = _data.Span;

            for (int i = 0; i < _rows; i++)
            {
                var line = new StringBuilder();
                for (int j = 0; j < _cols; j++)
                {
                    if (j > 0) line.Append(delimiter);
                    line.Append(span[i * _cols + j]);
                }
                writer.WriteLine(line);
            }
        }

        /// <summary>
        /// Import matrix from CSV
        /// </summary>
        /// <param name="path">File path</param>
        /// <param name="delimiter">Delimiter</param>
        /// <returns>Matrix object</returns>
        public static Matrix<T> ImportFromCsv(string path, string delimiter = ",")
        {
            var lines = File.ReadAllLines(path);
            if (lines.Length == 0)
                throw new InvalidDataException("CSV file is empty");

            int rows = lines.Length;
            int cols = lines[0].Split(delimiter).Length;

            var matrix = new Matrix<T>(rows, cols);

            for (int i = 0; i < rows; i++)
            {
                var values = lines[i].Split(delimiter);
                if (values.Length != cols)
                    throw new InvalidDataException("CSV file has inconsistent rows and columns");

                for (int j = 0; j < cols; j++)
                {
                    if (decimal.TryParse(values[j], out decimal result))
                    {
                        matrix.SetDecimal(i, j, result);
                    }
                    else
                    {
                        throw new InvalidDataException($"Cannot parse value: {values[j]}");
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// Serialize matrix to JSON
        /// </summary>
        /// <returns>JSON string representation</returns>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            return JsonSerializer.Serialize(new MatrixJsonData
            {
                Rows = _rows,
                Cols = _cols,
                Data = ToFlatArray()
            }, options);
        }

        /// <summary>
        /// Deserialize matrix from JSON
        /// </summary>
        /// <param name="json">JSON string</param>
        /// <returns>Matrix object</returns>
        public static Matrix<T> FromJson(string json)
        {
            var data = JsonSerializer.Deserialize<MatrixJsonData>(json);

            if (data.Rows <= 0 || data.Cols <= 0 || data.Data == null || data.Data.Length != data.Rows * data.Cols)
                throw new JsonException("Invalid matrix data");

            var memory = new Memory<decimal>(data.Data);
            return new Matrix<T>(memory, data.Rows, data.Cols, false);
        }

        /// <summary>
        /// Convert matrix to flat array
        /// </summary>
        /// <returns>Flat array representation</returns>
        internal decimal[] ToFlatArray()
        {
            return _data.ToArray();
        }

        /// <summary>
        /// Dispose resources (if using memory pool)
        /// </summary>
        public void Dispose()
        {
            if (_isPooled)
            {
                // If using memory pool, return memory here
                // Simplified in this example, actual implementation should use memory pool
            }
        }

        /// <summary>
        /// Create a matrix using memory pool (high-performance scenarios)
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="cols">Number of columns</param>
        /// <returns>Matrix object</returns>
        public static Matrix<T> CreatePooled(int rows, int cols)
        {
            // Actual implementation should use ArrayPool<decimal>.Shared.Rent
            // Simplified here
            var memory = new Memory<decimal>(new decimal[rows * cols]);
            return new Matrix<T>(memory, rows, cols, true);
        }
    }
}
#endif
