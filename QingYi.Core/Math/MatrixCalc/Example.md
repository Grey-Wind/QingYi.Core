```csharp
namespace MatrixTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Create two 2x2 matrices
            double[,] dataA = { { 1, 2 }, { 3, 4 } };
            double[,] dataB = { { 5, 6 }, { 7, 8 } };
            Matrix a = new Matrix(2, 2, dataA);
            Matrix b = new Matrix(2, 2, dataB);

            // Result of matrix addition
            Matrix sum = a + b;
            Console.WriteLine("Result of matrix addition:");
            PrintMatrix(sum);

            // Result of matrix subtraction
            Matrix diff = a - b;
            Console.WriteLine("Result of matrix subtraction:");
            PrintMatrix(diff);

            // Result of matrix multiplication
            Matrix product = a * b;
            Console.WriteLine("Result of matrix multiplication:");
            PrintMatrix(product);

            // The result of multiplying the matrix by 2
            Matrix scaled = a * 2;
            Console.WriteLine("The result of multiplying the matrix by 2:");
            PrintMatrix(scaled);

            // The result of multiplying an integer by a matrix
            Matrix scaledB = 2 * b;
            Console.WriteLine("The result of multiplying an integer by a matrix:");
            PrintMatrix(scaledB);

            // The result of dividing the matrix by 2
            Matrix divided = a / 2;
            Console.WriteLine("The result of dividing the matrix by 2:");
            PrintMatrix(divided);

            Console.ReadLine();
        }

        private static void PrintMatrix(Matrix m)
        {
            for (int i = 0; i < m.Rows; i++)
            {
                for (int j = 0; j < m.Cols; j++)
                {
                    Console.Write(m.Data[i, j] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
```

Output

```text
Result of matrix addition:
6 8
10 12

Result of matrix subtraction:
-4 -4
-4 -4

Result of matrix multiplication:
19 22
43 50

The result of multiplying the matrix by 2:
2 4
6 8

The result of multiplying an integer by a matrix:
10 12
14 16

The result of dividing the matrix by 2:
0.5 1
1.5 2
```
