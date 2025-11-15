namespace FftDataAnalyzer.Models
{
    /// <summary>
    /// Options for FFT computation
    /// </summary>
    public class FftOptions
    {
        /// <summary>
        /// FFT length (power of 2). If null, uses next power of 2 >= sample count
        /// </summary>
        public int? FftLength { get; set; }

        /// <summary>
        /// Window function type
        /// </summary>
        public WindowType WindowType { get; set; }

        /// <summary>
        /// Whether to use logarithmic scale for amplitude
        /// </summary>
        public bool UseLogScale { get; set; }

        public FftOptions()
        {
            WindowType = WindowType.Hann;
            UseLogScale = false;
        }
    }

    /// <summary>
    /// Window function types for FFT
    /// </summary>
    public enum WindowType
    {
        None,
        Hann,
        Hamming,
        Blackman
    }
}
