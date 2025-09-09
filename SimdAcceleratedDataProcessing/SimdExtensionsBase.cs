using System.Numerics;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// Base class for SIMD extensions with automatic fallback
    /// </summary>
    public static class SimdExtensionsBase
    {
        /// <summary>
        /// Determines if SIMD should be used based on data size and system capabilities
        /// </summary>
        public static bool ShouldUseSimd<T>(ReadOnlySpan<T> data) where T : struct
        {
            // Use SIMD only if hardware acceleration is available and data is large enough
            return Vector.IsHardwareAccelerated && data.Length >= Vector<T>.Count * 2;
        }

        /// <summary>
        /// Gets the vector count for type T
        /// </summary>
        public static int GetVectorCount<T>() where T : struct => Vector<T>.Count;

        /// <summary>
        /// Processes remaining elements that don't fit into full vectors
        /// </summary>
        public static void ProcessRemaining<T>(ReadOnlySpan<T> data, int vectorCount, Action<ReadOnlySpan<T>, int> action)
        {
            int remainingStart = (data.Length / vectorCount) * vectorCount;
            for (int i = remainingStart; i < data.Length; i++)
            {
                action(data, i);
            }
        }
    }
}