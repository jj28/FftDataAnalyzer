using System;

namespace FftDataAnalyzer.Models
{
    /// <summary>
    /// Represents a single FFT sample point (frequency and amplitude)
    /// </summary>
    public class FftSample
    {
        public long Id { get; set; }
        public Guid FftRecordId { get; set; }
        public double Frequency { get; set; }
        public double Amplitude { get; set; }
        public int SampleIndex { get; set; }
    }
}
