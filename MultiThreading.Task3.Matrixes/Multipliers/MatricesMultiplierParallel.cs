using MultiThreading.Task3.MatrixMultiplier.Matrices;
using System.Threading.Tasks;

namespace MultiThreading.Task3.MatrixMultiplier.Multipliers
{
    public class MatricesMultiplierParallel : IMatricesMultiplier
    {
        public IMatrix Multiply(IMatrix m1, IMatrix m2)
        {
            // todo
            // Implement the logic of multiplying two matrices using parallel processing.
            // You can use Parallel.For or any other parallel processing technique.
            // Make sure that all the tests within MultiThreading.Task3.MatrixMultiplier.Tests.csproj run successfully.
            // You can use the existing MatricesMultiplier class as a reference for the logic.
            // For now, just return a new Matrix with 1 row and 1 column.
            // This is a placeholder implementation.
            // You should replace this with the actual implementation.
            // For example:
            var resultMatrix = new Matrix(m1.RowCount, m2.ColCount);
            var rows = (int)m1.RowCount;
            var cols = (int)m2.ColCount;
            Parallel.For(0, rows, i =>
            {
                for (int j = 0; j < cols; j++)
                {
                    var sum = 0L;
                    for (int k = 0; k < rows; k++)
                    {
                        sum += m1.GetElement(i, k) * m2.GetElement(k, j);
                    }
                    resultMatrix.SetElement(i, j, sum);
                }
            });

            return resultMatrix;
        }
    }
}
