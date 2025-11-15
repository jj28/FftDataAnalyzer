using System.Collections.Generic;

namespace FftDataAnalyzer.Models
{
    /// <summary>
    /// Result of FFT computation
    /// </summary>
    public class FftResult
    {
        /// <summary>
        /// Frequency values in Hz
        /// </summary>
        public double[] Frequencies { get; set; }

        /// <summary>
        /// Amplitude values corresponding to each frequency
        /// </summary>
        public double[] Amplitudes { get; set; }

        /// <summary>
        /// Peak frequencies found in the signal
        /// </summary>
        public List<PeakInfo> Peaks { get; set; }

        /// <summary>
        /// Sample rate used for computation
        /// </summary>
        public int SampleRate { get; set; }

        /// <summary>
        /// Number of samples processed
        /// </summary>
        public int SampleCount { get; set; }

        public FftResult()
        {
            Peaks = new List<PeakInfo>();
        }
    }

    /// <summary>
    /// Information about a peak in the FFT
    /// </summary>
    public class PeakInfo
    {
        public double Frequency { get; set; }
        public double Amplitude { get; set; }
        public int Index { get; set; }
    }
}
