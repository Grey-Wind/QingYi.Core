using QingYi.Core.Mathematics.Matrix;

// Create a matrix
var matrix = new Matrix<double>(new double[,] {
    {1, 2, 3},
    {4, 5, 6},
    {7, 8, 9}
});

// Serialize to JSON
string json = matrix.ToJson();
Console.WriteLine(json);

// Deserialize from JSON
var matrixFromJson = Matrix<double>.FromJson(json);
Console.WriteLine(matrixFromJson);

// Export to CSV
matrix.ExportToCsv("matrix.csv");

// Import from CSV
var matrixFromCsv = Matrix<double>.ImportFromCsv("matrix.csv");
Console.WriteLine(matrixFromCsv);