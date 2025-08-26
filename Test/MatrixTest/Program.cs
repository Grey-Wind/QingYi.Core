using QingYi.Core.Mathematics.Matrix;
using System.Text.Json;

// Create a matrix from double array
var matrix1 = new Matrix<double>(new double[,] {
    {1.0, 2.0, 3.0},
    {4.0, 5.0, 6.0},
    {7.0, 8.0, 9.0}
});

// Create a matrix from decimal array
var matrix2 = new Matrix<decimal>(new decimal[,] {
    {0.1m, 0.2m, 0.3m},
    {0.4m, 0.5m, 0.6m},
    {0.7m, 0.8m, 0.9m}
});

// Matrix operations with high precision
var result = matrix1.Add(matrix2);
var product = matrix1.Multiply(matrix2);
var transposed = result.Transpose();

// Export to CSV
result.ExportToCsv("matrix.csv");

// Import from CSV
var matrixFromCsv = Matrix<double>.ImportFromCsv("matrix.csv");

// Serialize to JSON
string json = result.ToJson();
File.WriteAllText("matrix.json", json);

// Deserialize from JSON
var jsonFromFile = File.ReadAllText("matrix.json");
var matrixFromJson = Matrix<double>.FromJson(jsonFromFile);

// Use extension methods
var scaled = matrix1.Multiply(2.5); // Scalar multiplication
var inverse = matrix1.Inverse(); // Matrix inversion

// Create special matrices
var identity = Matrix<double>.Identity(5);
var diagonal = MatrixFactory.Diagonal(new double[] { 1, 2, 3, 4, 5 });
var random = MatrixFactory.Random(3, 3, 0.0, 1.0);