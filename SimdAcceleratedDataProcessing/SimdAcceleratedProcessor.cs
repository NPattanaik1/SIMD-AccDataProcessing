using System.Numerics;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// Main entry point for the SIMD-Accelerated Data Processing Library
    /// </summary>
    public class SimdAcceleratedProcessor
    {
        private readonly SimdCapabilities _capabilities;

        public SimdAcceleratedProcessor()
        {
            _capabilities = SimdDetection.Capabilities;
        }

        /// <summary>
        /// Gets the SIMD capabilities of the current system
        /// </summary>
        public SimdCapabilities Capabilities => _capabilities;

        /// <summary>
        /// Processes data with automatic SIMD acceleration selection
        /// </summary>
        public ProcessingResult<T> Process<T>(ReadOnlySpan<T> data, ProcessingOperation operation) 
            where T : struct, INumber<T>, IMinMaxValue<T>
        {
            var result = new ProcessingResult<T>
            {
                InputCount = data.Length,
                UsedSimd = SimdExtensionsBase.ShouldUseSimd(data),
                Capabilities = _capabilities
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            switch (operation)
            {
                case ProcessingOperation.Sum:
                    result.Result = data.SumSimd();
                    break;
                case ProcessingOperation.Average:
                    result.Result = data.AverageSimd();
                    break;
                case ProcessingOperation.Min:
                    result.Result = data.MinSimd();
                    break;
                case ProcessingOperation.Max:
                    result.Result = data.MaxSimd();
                    break;
                default:
                    throw new ArgumentException("Unsupported processing operation");
            }

            stopwatch.Stop();
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }

        /// <summary>
        /// Demonstrates the library capabilities with sample data
        /// </summary>
        public void DemonstrateCapabilities()
        {
            Console.WriteLine("=== SIMD-Accelerated Data Processing Library Demo ===");
            Console.WriteLine($"System Capabilities: {_capabilities}");
            Console.WriteLine();

            // Generate sample data
            var random = new Random();
            var data = new double[10000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = random.NextDouble() * 100;
            }

            var span = data.AsSpan();
            var processor = new SimdAcceleratedProcessor();

            // Demonstrate various operations
            DemonstrateOperation<double>(processor, span, ProcessingOperation.Sum);
            DemonstrateOperation<double>(processor, span, ProcessingOperation.Average);
            DemonstrateOperation<double>(processor, span, ProcessingOperation.Min);
            DemonstrateOperation<double>(processor, span, ProcessingOperation.Max);

            // Demonstrate financial calculations
            DemonstrateFinancialCalculations(span);

            // Demonstrate scientific calculations
            DemonstrateScientificCalculations(span);
        }

        private void DemonstrateOperation<T>(SimdAcceleratedProcessor processor, ReadOnlySpan<T> data, ProcessingOperation operation) 
            where T : struct, INumber<T>, IMinMaxValue<T>
        {
            var result = processor.Process(data, operation);
            Console.WriteLine($"{operation}: {result.Result} (SIMD: {result.UsedSimd}, Time: {result.ProcessingTimeMs}ms)");
        }

        private void DemonstrateFinancialCalculations(ReadOnlySpan<double> data)
        {
            Console.WriteLine("\n=== Financial Calculations ===");
            
            // Create weights for portfolio calculation
            var weights = new double[data.Length];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = 1.0 / data.Length;
            }

            var portfolioReturn = data.CalculatePortfolioReturnSimd(weights.AsSpan());
            var volatility = data.CalculateVolatilitySimd();
            var var95 = data.CalculateVaRSimd(0.95);
            var var99 = data.CalculateVaRSimd(0.99);

            Console.WriteLine($"Portfolio Return: {portfolioReturn}");
            Console.WriteLine($"Volatility: {volatility}");
            Console.WriteLine($"VaR (95%): {var95}");
            Console.WriteLine($"VaR (99%): {var99}");
        }

        private void DemonstrateScientificCalculations(ReadOnlySpan<double> data)
        {
            Console.WriteLine("\n=== Scientific Calculations ===");
            
            // Create a second vector for dot product
            var data2 = new double[data.Length];
            var random = new Random();
            for (int i = 0; i < data2.Length; i++)
            {
                data2[i] = random.NextDouble() * 100;
            }

            var dotProduct = data.DotProductSimd(data2.AsSpan());
            var euclideanDistance = data.EuclideanDistanceSimd(data2.AsSpan());

            Console.WriteLine($"Dot Product: {dotProduct}");
            Console.WriteLine($"Euclidean Distance: {euclideanDistance}");
        }
    }

    public enum ProcessingOperation
    {
        Sum,
        Average,
        Min,
        Max
    }

    public class ProcessingResult<T>
    {
        public T Result { get; set; } = default!;
        public int InputCount { get; set; }
        public bool UsedSimd { get; set; }
        public double ProcessingTimeMs { get; set; }
        public SimdCapabilities Capabilities { get; set; } = default!;
    }
}