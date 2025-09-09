using System.Numerics;
using System.Runtime.InteropServices;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// SIMD-accelerated scientific computing operations
    /// </summary>
    public static class ScientificSimdExtensions
    {
        /// <summary>
        /// SIMD-accelerated dot product calculation
        /// </summary>
        public static T DotProductSimd<T>(this ReadOnlySpan<T> vector1, ReadOnlySpan<T> vector2) 
            where T : struct, INumber<T>
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vectors must have the same length");

            if (!SimdExtensionsBase.ShouldUseSimd(vector1))
            {
                return DotProductScalar(vector1, vector2);
            }

            return DotProductVectorized(vector1, vector2);
        }

        /// <summary>
        /// SIMD-accelerated Euclidean distance calculation
        /// </summary>
        public static double EuclideanDistanceSimd(this ReadOnlySpan<double> vector1, ReadOnlySpan<double> vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("Vectors must have the same length");

            if (!SimdExtensionsBase.ShouldUseSimd(vector1))
            {
                return EuclideanDistanceScalar(vector1, vector2);
            }

            return EuclideanDistanceVectorized(vector1, vector2);
        }

        /// <summary>
        /// SIMD-accelerated matrix-vector multiplication
        /// </summary>
        public static double[] MatrixVectorMultiplySimd(this double[,] matrix, ReadOnlySpan<double> vector)
        {
            if (matrix.GetLength(1) != vector.Length)
                throw new ArgumentException("Matrix columns must match vector length");

            var result = new double[matrix.GetLength(0)];
            var columns = matrix.GetLength(1);

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                var rowArray = new double[columns];
                for (int j = 0; j < columns; j++) rowArray[j] = matrix[i, j];

                var rowSpan = new ReadOnlySpan<double>(rowArray);
                result[i] = rowSpan.DotProductSimd(vector);
            }
            
            return result;
        }

        #region Vectorized Implementations
        
        private static T DotProductVectorized<T>(ReadOnlySpan<T> vector1, ReadOnlySpan<T> vector2) 
            where T : struct, INumber<T>
        {
            var vectorCount = Vector<T>.Count;
            var vectors1 = MemoryMarshal.Cast<T, Vector<T>>(vector1);
            var vectors2 = MemoryMarshal.Cast<T, Vector<T>>(vector2);
            
            Vector<T> dotProduct = Vector<T>.Zero;
            
            for (int i = 0; i < vectors1.Length; i++)
            {
                dotProduct += vectors1[i] * vectors2[i];
            }
            
            // Sum the vector elements
            T result = T.Zero;
            for (int i = 0; i < vectorCount; i++)
            {
                result += dotProduct[i];
            }
            
            // Add remaining elements
            int remainingStart = vectors1.Length * vectorCount;
            for (int i = remainingStart; i < vector1.Length; i++)
            {
                result += vector1[i] * vector2[i];
            }
            
            return result;
        }

        private static double EuclideanDistanceVectorized(ReadOnlySpan<double> vector1, ReadOnlySpan<double> vector2)
        {
            var vectorCount = Vector<double>.Count;
            var vectors1 = MemoryMarshal.Cast<double, Vector<double>>(vector1);
            var vectors2 = MemoryMarshal.Cast<double, Vector<double>>(vector2);
            
            Vector<double> sumSquared = Vector<double>.Zero;
            
            for (int i = 0; i < vectors1.Length; i++)
            {
                var diff = vectors1[i] - vectors2[i];
                sumSquared += diff * diff;
            }
            
            // Sum the vector elements
            double result = 0;
            for (int i = 0; i < vectorCount; i++)
            {
                result += sumSquared[i];
            }
            
            // Add remaining elements
            int remainingStart = vectors1.Length * vectorCount;
            for (int i = remainingStart; i < vector1.Length; i++)
            {
                var diff = vector1[i] - vector2[i];
                result += diff * diff;
            }
            
            return Math.Sqrt(result);
        }

        #endregion

        #region Scalar Fallback Implementations

        private static T DotProductScalar<T>(ReadOnlySpan<T> vector1, ReadOnlySpan<T> vector2) 
            where T : struct, INumber<T>
        {
            T result = T.Zero;
            for (int i = 0; i < vector1.Length; i++)
            {
                result += vector1[i] * vector2[i];
            }
            return result;
        }

        private static double EuclideanDistanceScalar(ReadOnlySpan<double> vector1, ReadOnlySpan<double> vector2)
        {
            double sumSquared = 0;
            for (int i = 0; i < vector1.Length; i++)
            {
                var diff = vector1[i] - vector2[i];
                sumSquared += diff * diff;
            }
            return Math.Sqrt(sumSquared);
        }

        #endregion
    }
}