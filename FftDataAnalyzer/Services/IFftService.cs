using FftDataAnalyzer.Models;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Service for FFT computation
    /// </summary>
    public interface IFftService
    {
        /// <summary>
        /// Compute FFT from time-domain samples
        /// </summary>
        /// <param name="samples">Time-domain samples</param>
        /// <param name="sampleRate">Sample rate in Hz</param>
        /// <param name="options">FFT computation options</param>
        /// <returns>FFT result with frequencies and amplitudes</returns>
        Task<FftResult> ComputeFftAsync(double[] samples, int sampleRate, FftOptions options);

        /// <summary>
        /// Find peak frequencies in the FFT result
        /// </summary>
        /// <param name="result">FFT result</param>
        /// <param name="topN">Number of top peaks to find</param>
        /// <returns>List of peak information</returns>
        System.Collections.Generic.List<PeakInfo> FindPeaks(FftResult result, int topN = 10);
    }
}
