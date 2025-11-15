using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace FftDataAnalyzer.Helpers
{
    /// <summary>
    /// CSV file parser for time-domain and frequency-domain data
    /// </summary>
    public class CsvParser
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Parse CSV file containing time-domain samples
        /// Expected format: Index,Amplitude or Time,Amplitude
        /// </summary>
        public static CsvParseResult ParseTimeDomain(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("CSV file not found", filePath);

                var lines = File.ReadAllLines(filePath);
                var samples = new List<double>();
                int lineNumber = 0;

                foreach (var line in lines)
                {
                    lineNumber++;

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip header line
                    if (lineNumber == 1 && (line.ToLower().Contains("time") ||
                                           line.ToLower().Contains("index") ||
                                           line.ToLower().Contains("amplitude")))
                        continue;

                    var parts = line.Split(',', ';', '\t');
                    if (parts.Length < 2)
                    {
                        Logger.Warn($"Invalid line format at line {lineNumber}: {line}");
                        continue;
                    }

                    // Try to parse the amplitude value (second column)
                    if (double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var amplitude))
                    {
                        samples.Add(amplitude);
                    }
                    else if (double.TryParse(parts[1].Trim().Replace('.', ','), NumberStyles.Float, CultureInfo.CurrentCulture, out amplitude))
                    {
                        samples.Add(amplitude);
                    }
                    else
                    {
                        Logger.Warn($"Failed to parse amplitude at line {lineNumber}: {parts[1]}");
                    }
                }

                Logger.Info($"Parsed {samples.Count} samples from CSV file: {filePath}");

                return new CsvParseResult
                {
                    Success = samples.Count > 0,
                    Samples = samples.ToArray(),
                    SampleCount = samples.Count,
                    DataType = CsvDataType.TimeDomain
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to parse CSV file: {filePath}");
                return new CsvParseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Parse CSV file containing frequency-domain data
        /// Expected format: FrequencyHz,Amplitude
        /// </summary>
        public static CsvParseResult ParseFrequencyDomain(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("CSV file not found", filePath);

                var lines = File.ReadAllLines(filePath);
                var frequencies = new List<double>();
                var amplitudes = new List<double>();
                int lineNumber = 0;

                foreach (var line in lines)
                {
                    lineNumber++;

                    // Skip empty lines
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Skip header line
                    if (lineNumber == 1 && (line.ToLower().Contains("frequency") ||
                                           line.ToLower().Contains("amplitude")))
                        continue;

                    var parts = line.Split(',', ';', '\t');
                    if (parts.Length < 2)
                    {
                        Logger.Warn($"Invalid line format at line {lineNumber}: {line}");
                        continue;
                    }

                    // Try to parse frequency and amplitude
                    if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var frequency) &&
                        double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var amplitude))
                    {
                        frequencies.Add(frequency);
                        amplitudes.Add(amplitude);
                    }
                }

                Logger.Info($"Parsed {frequencies.Count} frequency points from CSV file: {filePath}");

                return new CsvParseResult
                {
                    Success = frequencies.Count > 0,
                    Frequencies = frequencies.ToArray(),
                    Amplitudes = amplitudes.ToArray(),
                    SampleCount = frequencies.Count,
                    DataType = CsvDataType.FrequencyDomain
                };
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to parse frequency-domain CSV file: {filePath}");
                return new CsvParseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Auto-detect CSV format and parse accordingly
        /// </summary>
        public static CsvParseResult ParseAuto(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("CSV file not found", filePath);

                // Read first few lines to detect format
                var lines = File.ReadAllLines(filePath).Take(5).ToArray();
                if (lines.Length < 2)
                    throw new InvalidDataException("CSV file does not contain enough data");

                var headerLine = lines[0].ToLower();

                // Check if it's frequency-domain data
                if (headerLine.Contains("frequency") || headerLine.Contains("freq") || headerLine.Contains("hz"))
                {
                    Logger.Info("Auto-detected frequency-domain CSV format");
                    return ParseFrequencyDomain(filePath);
                }
                else
                {
                    Logger.Info("Auto-detected time-domain CSV format");
                    return ParseTimeDomain(filePath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to auto-parse CSV file: {filePath}");
                return new CsvParseResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Try to detect sample rate from time column (if present)
        /// </summary>
        public static int? DetectSampleRate(string filePath)
        {
            try
            {
                var lines = File.ReadAllLines(filePath).Take(100).ToArray();
                var times = new List<double>();

                foreach (var line in lines.Skip(1)) // Skip header
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var parts = line.Split(',', ';', '\t');
                    if (parts.Length < 2)
                        continue;

                    if (double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var time))
                    {
                        times.Add(time);
                    }

                    if (times.Count >= 10)
                        break;
                }

                if (times.Count >= 2)
                {
                    // Calculate average time difference
                    var diffs = new List<double>();
                    for (int i = 1; i < times.Count; i++)
                    {
                        diffs.Add(times[i] - times[i - 1]);
                    }

                    var avgDiff = diffs.Average();
                    if (avgDiff > 0)
                    {
                        int sampleRate = (int)Math.Round(1.0 / avgDiff);
                        Logger.Info($"Detected sample rate: {sampleRate} Hz");
                        return sampleRate;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to detect sample rate");
            }

            return null;
        }
    }

    /// <summary>
    /// Result of CSV parsing operation
    /// </summary>
    public class CsvParseResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public double[] Samples { get; set; }
        public double[] Frequencies { get; set; }
        public double[] Amplitudes { get; set; }
        public int SampleCount { get; set; }
        public CsvDataType DataType { get; set; }
    }

    /// <summary>
    /// Type of CSV data
    /// </summary>
    public enum CsvDataType
    {
        TimeDomain,
        FrequencyDomain
    }
}
