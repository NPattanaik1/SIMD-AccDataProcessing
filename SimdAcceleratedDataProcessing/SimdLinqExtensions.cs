using System.Numerics;
using System.Runtime.InteropServices;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// SIMD-accelerated LINQ operations with automatic fallback
    /// </summary>
    public static class SimdLinqExtensions
    {
        /// <summary>
        /// SIMD-accelerated Sum operation
        /// </summary>
        public static T SumSimd<T>(this ReadOnlySpan<T> source) where T : struct, INumber<T>
        {
            if (!SimdExtensionsBase.ShouldUseSimd(source))
            {
                return SumScalar(source);
            }
            
            return SumVectorized(source);
        }

        /// <summary>
        /// SIMD-accelerated Average operation
        /// </summary>
        public static T AverageSimd<T>(this ReadOnlySpan<T> source) where T : struct, INumber<T>, IDivisionOperators<T, T, T>
        {
            if (source.Length == 0)
                throw new ArgumentException("Source sequence is empty");

            var sum = source.SumSimd();
            return sum / T.CreateChecked(source.Length);
        }

        /// <summary>
        /// SIMD-accelerated Min operation
        /// </summary>
        public static T MinSimd<T>(this ReadOnlySpan<T> source) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            if (!SimdExtensionsBase.ShouldUseSimd(source))
            {
                return MinScalar(source);
            }
            
            return MinVectorized(source);
        }

        /// <summary>
        /// SIMD-accelerated Max operation
        /// </summary>
        public static T MaxSimd<T>(this ReadOnlySpan<T> source) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            if (!SimdExtensionsBase.ShouldUseSimd(source))
            {
                return MaxScalar(source);
            }
            
            return MaxVectorized(source);
        }

        /// <summary>
        /// SIMD-accelerated Where operation with predicate
        /// </summary>
        // public static List<T> WhereSimd<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate) where T : struct
        // {
        //     if (!SimdExtensionsBase.ShouldUseSimd(source))
        //     {
        //         return WhereScalar(source, predicate);
        //     }
            
        //     return WhereVectorized(source, predicate);
        // }

        #region Vectorized Implementations

        private static T SumVectorized<T>(ReadOnlySpan<T> source) where T : struct, INumber<T>
        {
            var vectorCount = Vector<T>.Count;
            var vectors = MemoryMarshal.Cast<T, Vector<T>>(source);
            
            Vector<T> sumVector = Vector<T>.Zero;
            
            foreach (var vector in vectors)
            {
                sumVector += vector;
            }
            
            // Sum the vector elements
            T sum = T.Zero;
            for (int i = 0; i < vectorCount; i++)
            {
                sum += sumVector[i];
            }
            
            // Add remaining elements
            SimdExtensionsBase.ProcessRemaining(source, vectorCount, (data, i) => sum += data[i]);
            
            return sum;
        }

        private static T MinVectorized<T>(ReadOnlySpan<T> source) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            var vectorCount = Vector<T>.Count;
            var vectors = MemoryMarshal.Cast<T, Vector<T>>(source);
            
            Vector<T> minVector = new Vector<T>(T.MaxValue);
            
            foreach (var vector in vectors)
            {
                minVector = Vector.Min(minVector, vector);
            }
            
            // Find min in the vector
            T min = T.MaxValue;
            for (int i = 0; i < vectorCount; i++)
            {
                min = T.Min(min, minVector[i]);
            }
            
            // Check remaining elements
            SimdExtensionsBase.ProcessRemaining(source, vectorCount, (data, i) => min = T.Min(min, data[i]));
            
            return min;
        }

        private static T MaxVectorized<T>(ReadOnlySpan<T> source) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            var vectorCount = Vector<T>.Count;
            var vectors = MemoryMarshal.Cast<T, Vector<T>>(source);
            
            Vector<T> maxVector = new Vector<T>(T.MinValue);
            
            foreach (var vector in vectors)
            {
                maxVector = Vector.Max(maxVector, vector);
            }
            
            // Find max in the vector
            T max = T.MinValue;
            for (int i = 0; i < vectorCount; i++)
            {
                max = T.Max(max, maxVector[i]);
            }
            
            // Check remaining elements
            SimdExtensionsBase.ProcessRemaining(source, vectorCount, (data, i) => max = T.Max(max, data[i]));
            
            return max;
        }

        /// <summary>
/// SIMD-accelerated Where operation with predicate
/// </summary>
    public static List<T> WhereSimd<T>(this ReadOnlySpan<T> source, Func<T, bool> predicate) where T : struct
    {
        if (!SimdExtensionsBase.ShouldUseSimd(source))
        {
            return WhereScalar(source, predicate);
        }
        
        return WhereVectorized(source, predicate);
    }

    // Fixed vectorized implementation
    private static List<T> WhereVectorized<T>(ReadOnlySpan<T> source, Func<T, bool> predicate) where T : struct
    {
        var result = new List<T>();
        var vectorCount = Vector<T>.Count;
        
        // Process vectorized elements
        for (int i = 0; i <= source.Length - vectorCount; i += vectorCount)
        {
            for (int j = 0; j < vectorCount; j++)
            {
                // Access the element directly without capturing the span
                if (predicate(source[i + j]))
                {
                    result.Add(source[i + j]);
                }
            }
        }
        
        // Process remaining elements
        // Process remaining elements
        SimdExtensionsBase.ProcessRemaining(source, vectorCount, (data, i) => 
        {
            if (predicate(data[i]))
            {
                result.Add(data[i]);
            }
        });
        
        return result;
    }

        #endregion

        #region Scalar Fallback Implementations

        private static T SumScalar<T>(ReadOnlySpan<T> source) where T : struct, INumber<T>
        {
            T sum = T.Zero;
            foreach (var item in source)
            {
                sum += item;
            }
            return sum;
        }

        private static T MinScalar<T>(ReadOnlySpan<T> source) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            if (source.Length == 0)
                throw new ArgumentException("Source sequence is empty");
            
            T min = source[0];
            for (int i = 1; i < source.Length; i++)
            {
                min = T.Min(min, source[i]);
            }
            return min;
        }

        private static T MaxScalar<T>(ReadOnlySpan<T> source) where T : struct, INumber<T>, IMinMaxValue<T>
        {
            if (source.Length == 0)
                throw new ArgumentException("Source sequence is empty");
            
            T max = source[0];
            for (int i = 1; i < source.Length; i++)
            {
                max = T.Max(max, source[i]);
            }
            return max;
        }

        private static List<T> WhereScalar<T>(ReadOnlySpan<T> source, Func<T, bool> predicate) where T : struct
        {
            var result = new List<T>();
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    result.Add(item);
                }
            }
            return result;
        }

        #endregion
    }
}