using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using QingYi.Core.Mathematics.Matrix;
using System.Diagnostics;

[MemoryDiagnoser]
[RankColumn]
public class MatrixBenchmark
{
    // Matrix sizes to test
    [Params(2, 10, 50, 100, 200, 500, 1000)]
    public int MatrixSize { get; set; }

    private Matrix<double> matrix1;
    private Matrix<double> matrix2;
    private Matrix<double> squareMatrix;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42); // Fixed seed for consistent results

        // Create matrices for testing
        var data1 = new double[MatrixSize, MatrixSize];
        var data2 = new double[MatrixSize, MatrixSize];

        for (int i = 0; i < MatrixSize; i++)
        {
            for (int j = 0; j < MatrixSize; j++)
            {
                data1[i, j] = random.NextDouble();
                data2[i, j] = random.NextDouble();
            }
        }

        matrix1 = new Matrix<double>(data1);
        matrix2 = new Matrix<double>(data2);

        // Create a square matrix for operations that require square matrices
        if (MatrixSize <= 100) // Limit size for expensive operations
        {
            squareMatrix = new Matrix<double>(data1);
        }
    }

    [Benchmark]
    public void MatrixAddition()
    {
        var result = matrix1.Add(matrix2);
    }

    [Benchmark]
    public void MatrixMultiplication()
    {
        var result = matrix1.Multiply(matrix2);
    }

    [Benchmark]
    public void MatrixTranspose()
    {
        var result = matrix1.Transpose();
    }

    [Benchmark]
    public void ScalarMultiplication()
    {
        var result = matrix1.Multiply(2.5);
    }

    [Benchmark]
    public void MatrixInversion()
    {
        if (MatrixSize <= 100) // Only test inversion on smaller matrices
        {
            var result = squareMatrix.Inverse();
        }
    }

    [Benchmark]
    public void MatrixDiagonal()
    {
        if (MatrixSize <= 100) // Only test on smaller matrices
        {
            var diagonal = new double[MatrixSize];
            var random = new Random(42);
            for (int i = 0; i < MatrixSize; i++)
            {
                diagonal[i] = random.NextDouble();
            }
            var result = MatrixFactory.Diagonal(diagonal);
        }
    }

    [Benchmark]
    public void SerializeToJson()
    {
        if (MatrixSize <= 50) // Only test on smaller matrices
        {
            var json = matrix1.ToJson();
        }
    }

    [Benchmark]
    public void DeserializeFromJson()
    {
        if (MatrixSize <= 50) // Only test on smaller matrices
        {
            var json = matrix1.ToJson();
            var matrix = Matrix<double>.FromJson(json);
        }
    }

    [Benchmark]
    public void ExportToCsv()
    {
        if (MatrixSize <= 50) // Only test on smaller matrices
        {
            matrix1.ExportToCsv("benchmark.csv");
        }
    }

    [Benchmark]
    public void ImportFromCsv()
    {
        if (MatrixSize <= 50) // Only test on smaller matrices
        {
            matrix1.ExportToCsv("benchmark.csv");
            var matrix = Matrix<double>.ImportFromCsv("benchmark.csv");
        }
    }
}

// Additional benchmark for specific operations on different matrix shapes
[MemoryDiagnoser]
[RankColumn]
public class MatrixShapeBenchmark
{
    // Test different matrix shapes
    [ParamsSource(nameof(MatrixShapes))]
    public (int rows, int cols) MatrixShape { get; set; }

    public (int, int)[] MatrixShapes => new[]
    {
            (2, 2),     // Small square
            (10, 10),   // Medium square
            (50, 50),   // Large square
            (10, 5),    // Rectangle
            (5, 10),    // Rectangle (transposed)
            (100, 50),  // Large rectangle
            (50, 100),  // Large rectangle (transposed)
        };

    private Matrix<double> matrix1;
    private Matrix<double> matrix2;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42); // Fixed seed for consistent results

        // Create matrices for testing
        var data1 = new double[MatrixShape.rows, MatrixShape.cols];
        var data2 = new double[MatrixShape.cols, MatrixShape.rows]; // For multiplication compatibility

        for (int i = 0; i < MatrixShape.rows; i++)
        {
            for (int j = 0; j < MatrixShape.cols; j++)
            {
                data1[i, j] = random.NextDouble();
            }
        }

        for (int i = 0; i < MatrixShape.cols; i++)
        {
            for (int j = 0; j < MatrixShape.rows; j++)
            {
                data2[i, j] = random.NextDouble();
            }
        }

        matrix1 = new Matrix<double>(data1);
        matrix2 = new Matrix<double>(data2);
    }

    [Benchmark]
    public void MatrixMultiplicationDifferentShapes()
    {
        var result = matrix1.Multiply(matrix2);
    }

    [Benchmark]
    public void MatrixTransposeDifferentShapes()
    {
        var result = matrix1.Transpose();
    }
}

// Benchmark for SIMD vs non-SIMD performance comparison
[MemoryDiagnoser]
[RankColumn]
public class SimdBenchmark
{
    [Params(50, 100, 200)]
    public int MatrixSize { get; set; }

    private Matrix<double> matrix1;
    private Matrix<double> matrix2;
    private Matrix<float> matrix1Float;
    private Matrix<float> matrix2Float;
    private Matrix<decimal> matrix1Decimal;
    private Matrix<decimal> matrix2Decimal;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42); // Fixed seed for consistent results

        // Create double matrices
        var data1 = new double[MatrixSize, MatrixSize];
        var data2 = new double[MatrixSize, MatrixSize];

        // Create float matrices
        var data1Float = new float[MatrixSize, MatrixSize];
        var data2Float = new float[MatrixSize, MatrixSize];

        // Create decimal matrices
        var data1Decimal = new decimal[MatrixSize, MatrixSize];
        var data2Decimal = new decimal[MatrixSize, MatrixSize];

        for (int i = 0; i < MatrixSize; i++)
        {
            for (int j = 0; j < MatrixSize; j++)
            {
                var value = random.NextDouble();
                data1[i, j] = value;
                data1Float[i, j] = (float)value;
                data1Decimal[i, j] = (decimal)value;

                value = random.NextDouble();
                data2[i, j] = value;
                data2Float[i, j] = (float)value;
                data2Decimal[i, j] = (decimal)value;
            }
        }

        matrix1 = new Matrix<double>(data1);
        matrix2 = new Matrix<double>(data2);
        matrix1Float = new Matrix<float>(data1Float);
        matrix2Float = new Matrix<float>(data2Float);
        matrix1Decimal = new Matrix<decimal>(data1Decimal);
        matrix2Decimal = new Matrix<decimal>(data2Decimal);
    }

    [Benchmark]
    public void DoubleMatrixAddition()
    {
        var result = matrix1.Add(matrix2);
    }

    [Benchmark]
    public void FloatMatrixAddition()
    {
        var result = matrix1Float.Add(matrix2Float);
    }

    [Benchmark]
    public void DecimalMatrixAddition()
    {
        var result = matrix1Decimal.Add(matrix2Decimal);
    }

    [Benchmark]
    public void DoubleMatrixMultiplication()
    {
        var result = matrix1.Multiply(matrix2);
    }

    [Benchmark]
    public void FloatMatrixMultiplication()
    {
        var result = matrix1Float.Multiply(matrix2Float);
    }

    [Benchmark]
    public void DecimalMatrixMultiplication()
    {
        var result = matrix1Decimal.Multiply(matrix2Decimal);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks
        var summary1 = BenchmarkRunner.Run<MatrixBenchmark>();
        var summary2 = BenchmarkRunner.Run<MatrixShapeBenchmark>();
        var summary3 = BenchmarkRunner.Run<SimdBenchmark>();
    }
}