using System;

namespace FftDataAnalyzer.Models
{
    /// <summary>
    /// Represents an FFT record with metadata
    /// </summary>
    public class FftRecord
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; }
        public string SourceFilename { get; set; }
        public int SampleRate { get; set; }
        public int SampleCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
        public double? PeakFrequency { get; set; }
        public double? PeakAmplitude { get; set; }

        public FftRecord()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
            Status = "Pending";
        }
    }
}
