using System.Text.Json;

namespace SimdAcceleratedDataProcessing
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SIMD-Accelerated Data Processing Library");
            Console.WriteLine("=====================================");

            // Initialize and demonstrate the library
            var processor = new SimdAcceleratedProcessor();
            processor.DemonstrateCapabilities();

            // Test LINQ operations
            TestLinqOperations();

            // Test JSON integration
            TestJsonIntegration();

            // Test memory efficiency
            TestMemoryEfficiency();

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void TestLinqOperations()
        {
            Console.WriteLine("\n=== Testing LINQ Operations ===");
            
            var random = new Random();
            var data = Enumerable.Range(0, 10000).Select(_ => random.NextDouble() * 1000).ToArray();
            var span = data.AsSpan();

            // Test various LINQ operations - cast to ReadOnlySpan
            Console.WriteLine($"Sum: {((ReadOnlySpan<double>)span).SumSimd()}");
            Console.WriteLine($"Average: {((ReadOnlySpan<double>)span).AverageSimd()}");
            Console.WriteLine($"Min: {((ReadOnlySpan<double>)span).MinSimd()}");
            Console.WriteLine($"Max: {((ReadOnlySpan<double>)span).MaxSimd()}");

            // Test Where operation
            var filtered = ((ReadOnlySpan<double>)span).WhereSimd(x => x > 500);
            Console.WriteLine($"Values > 500: {filtered.Count}");
        }

        static void TestJsonIntegration()
        {
            Console.WriteLine("\n=== Testing JSON Integration ===");
            
            var data = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();
            var span = data.AsSpan();

            // Test SIMD-enhanced JSON serialization (simple array)
            var json = span.ToJsonArraySimd();
            Console.WriteLine($"Generated JSON length: {json.Length}");

            // Test parsing back
            var element = JsonDocument.Parse(json).RootElement;
            var parsedArray = element.ParseNumericArraySimd<double>();
            Console.WriteLine($"Parsed back {parsedArray.Length} elements");

            // Test SIMD-enhanced JSON serialization with metadata
            var jsonWithMetadata = span.ToJsonWithMetadataSimd();
            Console.WriteLine($"JSON with metadata length: {jsonWithMetadata.Length}");
            
            // Parse the JSON with metadata
            var jsonDoc = JsonDocument.Parse(jsonWithMetadata);
            var valuesElement = jsonDoc.RootElement.GetProperty("Values");
            var parsedArrayWithMetadata = valuesElement.ParseNumericArraySimd<double>();
            Console.WriteLine($"Parsed back {parsedArrayWithMetadata.Length} elements from metadata JSON");
            
            // Display metadata
            var metadataElement = jsonDoc.RootElement.GetProperty("Metadata");
            Console.WriteLine($"Metadata - Count: {metadataElement.GetProperty("Count").GetInt32()}");
            Console.WriteLine($"Metadata - Sum: {metadataElement.GetProperty("Sum").GetDouble()}");
            Console.WriteLine($"Metadata - Processed with SIMD: {metadataElement.GetProperty("ProcessedWithSimd").GetBoolean()}");
        }

        static void TestMemoryEfficiency()
        {
            Console.WriteLine("\n=== Testing Memory Efficiency ===");
            
            var largeData = Enumerable.Range(1, 100000).Select(i => (double)i).ToArray();
            var span = largeData.AsSpan();

            // Test chunked processing - cast to ReadOnlySpan
            var sum = ((ReadOnlySpan<double>)span).ProcessInChunks(s => s.SumSimd(), 8192);
            Console.WriteLine($"Chunked sum: {sum}");

            // Test stack allocation
            var smallData = Enumerable.Range(1, 100).Select(i => (double)i).ToArray();
            var smallSpan = smallData.AsSpan();
            var smallSum = ((ReadOnlySpan<double>)smallSpan).ProcessWithStackAllocation(s => s.SumSimd());
            Console.WriteLine($"Stack-allocated sum: {smallSum}");
        }
    }
}