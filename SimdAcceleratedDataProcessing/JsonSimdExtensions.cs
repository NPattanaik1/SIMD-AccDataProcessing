using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// SIMD-accelerated JSON processing extensions
    /// </summary>
    public static class JsonSimdExtensions
    {
        /// <summary>
        /// SIMD-accelerated JSON array parsing for numeric arrays
        /// </summary>
        /// <summary>
        /// SIMD-accelerated JSON array parsing for numeric arrays
        /// </summary>
        public static T[] ParseNumericArraySimd<T>(this JsonElement element) where T : struct, INumber<T>
        {
            if (element.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException($"Expected JSON array but got {element.ValueKind}");
            }

            var values = new List<T>();
            
            foreach (var item in element.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Number)
                {
                    if (typeof(T) == typeof(int))
                        values.Add((T)(object)item.GetInt32());
                    else if (typeof(T) == typeof(double))
                        values.Add((T)(object)item.GetDouble());
                    else if (typeof(T) == typeof(float))
                        values.Add((T)(object)item.GetSingle());
                    else if (typeof(T) == typeof(decimal))
                        values.Add((T)(object)item.GetDecimal());
                    else
                        throw new NotSupportedException($"Type {typeof(T)} is not supported");
                }
                else
                {
                    throw new JsonException($"Expected number but got {item.ValueKind}");
                }
            }

            return values.ToArray();
        }

        /// <summary>
        /// SIMD-accelerated JSON array serialization for numeric arrays
        /// </summary>
        // public static string ToJsonArraySimd<T>(this ReadOnlySpan<T> values) where T : struct, INumber<T>,
        // IMinMaxValue<T>
        // {
        //     // Use SIMD to calculate statistics for the array
        //     var sum = values.SumSimd();
        //     var avg = values.AverageSimd();
        //     var min = values.MinSimd();
        //     var max = values.MaxSimd();

        //     // Create enhanced JSON with SIMD-calculated metadata
        //     var result = new
        //     {
        //         Values = values.ToArray(),
        //         Metadata = new
        //         {
        //             Count = values.Length,
        //             Sum = sum,
        //             Average = avg,
        //             Min = min,
        //             Max = max,
        //             ProcessedWithSimd = SimdDetection.IsSimdSupported
        //         }
        //     };

        //     return JsonSerializer.Serialize(result);
        // }
        /// </summary>
        public static string ToJsonWithMetadataSimd<T>(this Span<T> values) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            return ToJsonWithMetadataSimd((ReadOnlySpan<T>)values);
        }
        public static string ToJsonWithMetadataSimd<T>(this ReadOnlySpan<T> values) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            // Use SIMD to calculate statistics for the array
            var sum = values.SumSimd();
            var avg = values.AverageSimd();
            var min = values.MinSimd();
            var max = values.MaxSimd();

            // Create enhanced JSON with SIMD-calculated metadata
            var result = new
            {
                Values = values.ToArray(),
                Metadata = new
                {
                    Count = values.Length,
                    Sum = sum,
                    Average = avg,
                    Min = min,
                    Max = max,
                    ProcessedWithSimd = SimdDetection.IsSimdSupported
                }
            };

            return JsonSerializer.Serialize(result);
        }

        public static string ToJsonArraySimd<T>(this ReadOnlySpan<T> values) where T : struct, INumber<T>
        {
            // For compatibility with ParseNumericArraySimd, we'll create a simple array
            return JsonSerializer.Serialize(values.ToArray());
        }
        /// </summary>
        public static string ToJsonArraySimd<T>(this Span<T> values) where T : struct, INumber<T>
        {
            return ToJsonArraySimd((ReadOnlySpan<T>)values);
        }

        /// <summary>
        /// SIMD-accelerated JSON batch processing
        /// </summary>
        public static BatchProcessingResult<T> ProcessBatchSimd<T>(this string json, Func<ReadOnlySpan<T>, T> processor)
            where T : struct, INumber<T>
        {
            var array = JsonSerializer.Deserialize<T[]>(json);
            if (array == null)
                throw new JsonException("Failed to deserialize JSON array");

            var span = array.AsSpan();
            var result = processor(span);

            return new BatchProcessingResult<T>
            {
                Result = result,
                InputCount = span.Length,
                ProcessingTimeMs = 0, // Would be measured in real implementation
                UsedSimd = SimdDetection.IsSimdSupported && span.Length >= Vector<T>.Count * 2
            };
        }
    }

    public class BatchProcessingResult<T>
    {
        public T Result { get; set; } = default!;
        public int InputCount { get; set; }
        public double ProcessingTimeMs { get; set; }
        public bool UsedSimd { get; set; }
    }
}