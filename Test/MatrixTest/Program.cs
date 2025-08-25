// Create matrices
using QingYi.Core.Mathematics.Matrix;
using System.Text.Json;

var matrix1 = new Matrix<double>(new double[,] {
    {1, 2, 3},
    {4, 5, 6},
    {7, 8, 9}
});

// Create identity matrix
var identity = Matrix<double>.Identity(3);

// Matrix multiplication
var result = matrix1.Multiply(identity);

// Matrix transpose
var transposed = result.Transpose();

// Export to CSV
result.ExportToCsv("matrix.csv");

// Serialize to JSON
var jsonString = JsonSerializer.Serialize(result);
File.WriteAllText("matrix.json", jsonString);

// Deserialize from JSON
var jsonFromFile = File.ReadAllText("matrix.json");
var matrixFromJson = JsonSerializer.Deserialize<Matrix<double>>(jsonFromFile);

// Use extension methods
var scaled = matrix1.Multiply(2.5); // Scalar multiplication
var inverse = matrix1.Inverse(); // Matrix inversion