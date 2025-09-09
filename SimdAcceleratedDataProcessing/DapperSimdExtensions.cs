using System.Data;
using System.Numerics;
using System.Runtime.InteropServices;
using Dapper;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// SIMD-accelerated Dapper extensions for database operations
    /// </summary>
    public static class DapperSimdExtensions
    {
        /// <summary>
        /// SIMD-accelerated query that processes numeric columns with SIMD
        /// </summary>
        public static SimdQueryResult<T> QuerySimd<T>(this IDbConnection connection, string sql, object? param = null) 
            where T : struct, INumber<T>, IMinMaxValue<T>
        {
            var results = connection.Query<T>(sql, param).ToArray();
            var span = results.AsSpan();

            return new SimdQueryResult<T>
            {
                Data = results,
                Sum = ((ReadOnlySpan<T>)span).SumSimd(),
                Average = ((ReadOnlySpan<T>)span).AverageSimd(),
                Min = ((ReadOnlySpan<T>)span).MinSimd(),
                Max = ((ReadOnlySpan<T>)span).MaxSimd(),
                Count = results.Length,
                UsedSimd = SimdDetection.IsSimdSupported && results.Length >= Vector<T>.Count * 2
            };
        }

        /// <summary>
        /// SIMD-accelerated batch insert operation
        /// </summary>
        public static int BulkInsertSimd<T>(this IDbConnection connection, string tableName, IEnumerable<T> data) 
            where T : struct
        {
            var dataArray = data.ToArray();
            var span = dataArray.AsSpan();

            // Use SIMD to validate data before insertion
            if (typeof(T) == typeof(double))
            {
                ValidateNumericData<double>(MemoryMarshal.Cast<T, double>(span));
            }
            else if (typeof(T) == typeof(float))
            {
                ValidateNumericData<float>(MemoryMarshal.Cast<T, float>(span));
            }

            // Perform bulk insert (simplified example)
            // In real implementation, you'd use proper bulk insert methods
            var insertSql = GenerateBulkInsertSql(tableName, dataArray);
            return connection.Execute(insertSql);
        }

        /// <summary>
        /// SIMD-accelerated financial data query with automatic calculations
        /// </summary>
        public static FinancialQueryResult QueryFinancialDataSimd(this IDbConnection connection, string sql, object? param = null)
        {
            var results = connection.Query<FinancialDataPoint>(sql, param).ToArray();
            var returnsSpan = results.Select(x => x.Return).ToArray().AsSpan();
            var weightsSpan = results.Select(x => x.Weight).ToArray().AsSpan();

            return new FinancialQueryResult
            {
                DataPoints = results,
                PortfolioReturn = ((ReadOnlySpan<double>)returnsSpan).CalculatePortfolioReturnSimd((ReadOnlySpan<double>)weightsSpan),
                Volatility = ((ReadOnlySpan<double>)returnsSpan).CalculateVolatilitySimd(),
                VaR95 = ((ReadOnlySpan<double>)returnsSpan).CalculateVaRSimd(0.95),
                VaR99 = ((ReadOnlySpan<double>)returnsSpan).CalculateVaRSimd(0.99),
                Count = results.Length,
                UsedSimd = SimdDetection.IsSimdSupported && results.Length >= Vector<double>.Count * 2
            };
        }

        private static void ValidateNumericData<T>(ReadOnlySpan<T> data) where T : struct
        {
            if (typeof(T) == typeof(double))
            {
                var doubleSpan = MemoryMarshal.Cast<T, double>(data);
                foreach (var value in doubleSpan)
                {
                    if (double.IsNaN(value) || double.IsInfinity(value))
                    {
                        throw new ArgumentException("Data contains NaN or Infinity values");
                    }
                }
            }
            else if (typeof(T) == typeof(float))
            {
                var floatSpan = MemoryMarshal.Cast<T, float>(data);
                foreach (var value in floatSpan)
                {
                    if (float.IsNaN(value) || float.IsInfinity(value))
                    {
                        throw new ArgumentException("Data contains NaN or Infinity values");
                    }
                }
            }
        }

        private static string GenerateBulkInsertSql<T>(string tableName, T[] data)
        {
            // Simplified SQL generation
            // In real implementation, you'd generate proper parameterized SQL
            var columns = string.Join(", ", typeof(T).GetProperties().Select(p => p.Name));
            var values = string.Join(", ", data.Select(d => $"({string.Join(", ", typeof(T).GetProperties().Select(p => $"'{p.GetValue(d)}'"))})"));
            return $"INSERT INTO {tableName} ({columns}) VALUES {values}";
        }
    }

    public class SimdQueryResult<T>
    {
        public T[] Data { get; set; } = [];
        public T Sum { get; set; } = default!;
        public T Average { get; set; } = default!;
        public T Min { get; set; } = default!;
        public T Max { get; set; } = default!;
        public int Count { get; set; }
        public bool UsedSimd { get; set; }
    }

    public class FinancialDataPoint
    {
        public double Return { get; set; }
        public double Weight { get; set; }
        public DateTime Date { get; set; }
    }

    public class FinancialQueryResult
    {
        public FinancialDataPoint[] DataPoints { get; set; } = [];
        public double PortfolioReturn { get; set; }
        public double Volatility { get; set; }
        public double VaR95 { get; set; }
        public double VaR99 { get; set; }
        public int Count { get; set; }
        public bool UsedSimd { get; set; }
    }
}