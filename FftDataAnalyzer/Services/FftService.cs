using FftDataAnalyzer.Models;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Implementation of FFT computation service using MathNet.Numerics
    /// </summary>
    public class FftService : IFftService
    {
        /// <summary>
        /// Compute FFT from time-domain samples
        /// </summary>
        public async Task<FftResult> ComputeFftAsync(double[] samples, int sampleRate, FftOptions options)
        {
            return await Task.Run(() => ComputeFft(samples, sampleRate, options));
        }

        private FftResult ComputeFft(double[] samples, int sampleRate, FftOptions options)
        {
            if (samples == null || samples.Length == 0)
                throw new ArgumentException("Samples array cannot be null or empty", nameof(samples));

            if (sampleRate <= 0)
                throw new ArgumentException("Sample rate must be positive", nameof(sampleRate));

            int n = samples.Length;
            int fftLength = options?.FftLength ?? NextPowerOfTwo(n);

            // Ensure FFT length is at least as large as sample count
            if (fftLength < n)
                fftLength = NextPowerOfTwo(n);

            // Create complex array and copy samples
            var complex = new Complex[fftLength];
            for (int i = 0; i < n && i < fftLength; i++)
            {
                complex[i] = new Complex(samples[i], 0);
            }

            // Apply window function
            ApplyWindow(complex, n, options?.WindowType ?? WindowType.Hann);

            // Perform FFT
            Fourier.Forward(complex, FourierOptions.Matlab);

            // Extract frequency and amplitude data (only positive frequencies)
            int half = fftLength / 2;
            var frequencies = new double[half];
            var amplitudes = new double[half];

            double frequencyResolution = (double)sampleRate / fftLength;

            for (int k = 0; k < half; k++)
            {
                frequencies[k] = k * frequencyResolution;

                // Compute magnitude
                double magnitude = complex[k].Magnitude;

                // Normalize by FFT length and apply scaling
                magnitude = magnitude / fftLength;

                // For single-sided spectrum, multiply by 2 (except DC component)
                if (k > 0)
                    magnitude *= 2;

                // Convert to dB if requested
                if (options?.UseLogScale == true)
                {
                    // Avoid log(0) by adding small epsilon
                    amplitudes[k] = 20 * Math.Log10(magnitude + 1e-10);
                }
                else
                {
                    amplitudes[k] = magnitude;
                }
            }

            var result = new FftResult
            {
                Frequencies = frequencies,
                Amplitudes = amplitudes,
                SampleRate = sampleRate,
                SampleCount = n
            };

            // Find peaks
            result.Peaks = FindPeaks(result, 10);

            return result;
        }

        /// <summary>
        /// Find peak frequencies in the FFT result
        /// </summary>
        public List<PeakInfo> FindPeaks(FftResult result, int topN = 10)
        {
            if (result == null || result.Amplitudes == null || result.Amplitudes.Length == 0)
                return new List<PeakInfo>();

            var peaks = new List<PeakInfo>();

            // Simple peak detection: find local maxima
            for (int i = 1; i < result.Amplitudes.Length - 1; i++)
            {
                if (result.Amplitudes[i] > result.Amplitudes[i - 1] &&
                    result.Amplitudes[i] > result.Amplitudes[i + 1])
                {
                    peaks.Add(new PeakInfo
                    {
                        Frequency = result.Frequencies[i],
                        Amplitude = result.Amplitudes[i],
                        Index = i
                    });
                }
            }

            // Sort by amplitude (descending) and take top N
            return peaks.OrderByDescending(p => p.Amplitude)
                       .Take(topN)
                       .OrderBy(p => p.Frequency)
                       .ToList();
        }

        /// <summary>
        /// Apply window function to reduce spectral leakage
        /// </summary>
        private void ApplyWindow(Complex[] data, int validLength, WindowType windowType)
        {
            if (windowType == WindowType.None)
                return;

            double[] window = null;

            switch (windowType)
            {
                case WindowType.Hann:
                    window = Window.Hann(validLength);
                    break;
                case WindowType.Hamming:
                    window = Window.Hamming(validLength);
                    break;
                case WindowType.Blackman:
                    window = Window.Blackman(validLength);
                    break;
                default:
                    return;
            }

            for (int i = 0; i < validLength && i < data.Length; i++)
            {
                data[i] *= window[i];
            }
        }

        /// <summary>
        /// Calculate next power of two greater than or equal to n
        /// </summary>
        private int NextPowerOfTwo(int n)
        {
            if (n <= 0)
                return 1;

            // Check if already power of two
            if ((n & (n - 1)) == 0)
                return n;

            // Find next power of two
            int power = 1;
            while (power < n)
                power <<= 1;

            return power;
        }
    }
}
