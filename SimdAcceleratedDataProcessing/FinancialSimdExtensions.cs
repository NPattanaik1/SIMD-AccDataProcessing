using System.Numerics;
using System.Runtime.InteropServices;

namespace SimdAcceleratedDataProcessing
{
    /// <summary>
    /// SIMD-accelerated financial calculations
    /// </summary>
    public static class FinancialSimdExtensions
    {
        /// <summary>
        /// SIMD-accelerated portfolio return calculation
        /// </summary>
        public static double CalculatePortfolioReturnSimd(this ReadOnlySpan<double> returns, ReadOnlySpan<double> weights)
        {
            if (returns.Length != weights.Length)
                throw new ArgumentException("Returns and weights must have the same length");

            if (!SimdExtensionsBase.ShouldUseSimd(returns))
            {
                return CalculatePortfolioReturnScalar(returns, weights);
            }

            return CalculatePortfolioReturnVectorized(returns, weights);
        }

        /// <summary>
        /// SIMD-accelerated volatility calculation
        /// </summary>
        public static double CalculateVolatilitySimd(this ReadOnlySpan<double> returns)
        {
            if (returns.Length < 2)
                throw new ArgumentException("At least 2 returns are required for volatility calculation");

            if (!SimdExtensionsBase.ShouldUseSimd(returns))
            {
                return CalculateVolatilityScalar(returns);
            }

            return CalculateVolatilityVectorized(returns);
        }

        /// <summary>
        /// SIMD-accelerated Value at Risk (VaR) calculation
        /// </summary>
        public static double CalculateVaRSimd(this ReadOnlySpan<double> returns, double confidence = 0.95)
        {
            if (!SimdExtensionsBase.ShouldUseSimd(returns))
            {
                return CalculateVaRScalar(returns, confidence);
            }

            return CalculateVaRVectorized(returns, confidence);
        }

        #region Vectorized Implementations

        private static double CalculatePortfolioReturnVectorized(ReadOnlySpan<double> returns, ReadOnlySpan<double> weights)
        {
            var vectorCount = Vector<double>.Count;
            var returnVectors = MemoryMarshal.Cast<double, Vector<double>>(returns);
            var weightVectors = MemoryMarshal.Cast<double, Vector<double>>(weights);
            
            Vector<double> portfolioReturn = Vector<double>.Zero;
            
            for (int i = 0; i < returnVectors.Length; i++)
            {
                portfolioReturn += returnVectors[i] * weightVectors[i];
            }
            
            // Sum the vector elements
            double result = 0;
            for (int i = 0; i < vectorCount; i++)
            {
                result += portfolioReturn[i];
            }
            
            // Add remaining elements
            int remainingStart = returnVectors.Length * vectorCount;
            for (int i = remainingStart; i < returns.Length; i++)
            {
                result += returns[i] * weights[i];
            }
            
            return result;
        }

        private static double CalculateVolatilityVectorized(ReadOnlySpan<double> returns)
        {
            // Calculate mean return
            var mean = returns.AverageSimd();
            
            // Calculate variance
            var vectorCount = Vector<double>.Count;
            var returnVectors = MemoryMarshal.Cast<double, Vector<double>>(returns);
            var meanVector = new Vector<double>(mean);
            
            Vector<double> varianceSum = Vector<double>.Zero;
            
            foreach (var returnVector in returnVectors)
            {
                var diff = returnVector - meanVector;
                varianceSum += diff * diff;
            }
            
            // Sum the variance vector
            double variance = 0;
            for (int i = 0; i < vectorCount; i++)
            {
                variance += varianceSum[i];
            }
            
            // Add remaining elements
            SimdExtensionsBase.ProcessRemaining(returns, vectorCount, (data, i) => 
            {
                var diff = data[i] - mean;
                variance += diff * diff;
            });
                        
            variance /= (returns.Length - 1);
            return Math.Sqrt(variance);
        }

        private static double CalculateVaRVectorized(ReadOnlySpan<double> returns, double confidence)
        {
            // For simplicity, we'll use the parametric VaR calculation
            // In a real implementation, you might want to use historical simulation or Monte Carlo
            var mean = returns.AverageSimd();
            var volatility = returns.CalculateVolatilitySimd();
            
            // Calculate VaR using the normal distribution assumption
            var zScore = GetZScore(confidence);
            return -(mean + zScore * volatility);
        }

        #endregion

        #region Scalar Fallback Implementations

        private static double CalculatePortfolioReturnScalar(ReadOnlySpan<double> returns, ReadOnlySpan<double> weights)
        {
            double result = 0;
            for (int i = 0; i < returns.Length; i++)
            {
                result += returns[i] * weights[i];
            }
            return result;
        }

        private static double CalculateVolatilityScalar(ReadOnlySpan<double> returns)
        {
            var mean = returns.AverageSimd();
            double variance = 0;
            
            foreach (var ret in returns)
            {
                var diff = ret - mean;
                variance += diff * diff;
            }
            
            variance /= (returns.Length - 1);
            return Math.Sqrt(variance);
        }

        private static double CalculateVaRScalar(ReadOnlySpan<double> returns, double confidence)
        {
            var mean = returns.AverageSimd();
            var volatility = returns.CalculateVolatilitySimd();
            var zScore = GetZScore(confidence);
            return -(mean + zScore * volatility);
        }

        #endregion

        private static double GetZScore(double confidence)
        {
            // Simplified z-score calculation
            // In a real implementation, you'd use a proper statistical library
            if (confidence >= 0.99) return 2.326;
            if (confidence >= 0.95) return 1.645;
            if (confidence >= 0.90) return 1.282;
            return 1.0;
        }
    }
}