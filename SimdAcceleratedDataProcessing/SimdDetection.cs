using System.Numerics;
using System.Runtime.Intrinsics.X86;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// Provides automatic SIMD detection and capability information
    /// </summary>
    public static class SimdDetection
    {
        private static SimdCapabilities? _capabilities;
        
        /// <summary>
        /// Gets the SIMD capabilities of the current system
        /// </summary>
        public static SimdCapabilities Capabilities
        {
            get
            {
                if (_capabilities == null)
                {
                    _capabilities = new SimdCapabilities();
                }
                return _capabilities;
            }
        }
        
        /// <summary>
        /// Checks if the system supports any SIMD acceleration
        /// </summary>
        public static bool IsSimdSupported => Vector.IsHardwareAccelerated;
        
        /// <summary>
        /// Gets the optimal vector size for the current system
        /// </summary>
        public static int OptimalVectorSize<T>() where T : struct => Vector<T>.Count;
    }

    /// <summary>
    /// Represents the SIMD capabilities of the current system
    /// </summary>
    public class SimdCapabilities
    {
        public bool IsSseSupported { get; }
        public bool IsSse2Supported { get; }
        public bool IsSse3Supported { get; }
        public bool IsSsse3Supported { get; }
        public bool IsSse41Supported { get; }
        public bool IsSse42Supported { get; }
        public bool IsAvxSupported { get; }
        public bool IsAvx2Supported { get; }
        public bool IsAvx512Supported { get; }
        public bool IsNeonSupported { get; }
        public int OptimalVectorSizeDouble { get; }
        public int OptimalVectorSizeInt { get; }
        public int OptimalVectorSizeFloat { get; }

        public SimdCapabilities()
        {
            IsSseSupported = Sse.IsSupported;
            IsSse2Supported = Sse2.IsSupported;
            IsSse3Supported = Sse3.IsSupported;
            IsSsse3Supported = Ssse3.IsSupported;
            IsSse41Supported = Sse41.IsSupported;
            IsSse42Supported = Sse42.IsSupported;
            IsAvxSupported = Avx.IsSupported;
            IsAvx2Supported = Avx2.IsSupported;
            IsAvx512Supported = Avx512F.IsSupported;
            IsNeonSupported = System.Runtime.Intrinsics.Arm.ArmBase.IsSupported;
            OptimalVectorSizeDouble = Vector<double>.Count;
            OptimalVectorSizeInt = Vector<int>.Count;
            OptimalVectorSizeFloat = Vector<float>.Count;
        }

        public override string ToString()
        {
            return $"SIMD Capabilities: SSE={IsSseSupported}, SSE2={IsSse2Supported}, " +
                   $"AVX={IsAvxSupported}, AVX2={IsAvx2Supported}, AVX512={IsAvx512Supported}, " +
                   $"NEON={IsNeonSupported}, VectorSize(double)={OptimalVectorSizeDouble}, " +
                   $"VectorSize(int)={OptimalVectorSizeInt}, VectorSize(float)={OptimalVectorSizeFloat}";
        }
    }
}