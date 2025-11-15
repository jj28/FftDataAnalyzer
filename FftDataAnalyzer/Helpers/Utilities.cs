using System;
using System.Windows;

namespace FftDataAnalyzer.Helpers
{
    /// <summary>
    /// General utility methods
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Format file size in human-readable format
        /// </summary>
        public static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Format frequency in Hz, kHz, MHz
        /// </summary>
        public static string FormatFrequency(double hz)
        {
            if (hz >= 1000000)
                return $"{hz / 1000000:F2} MHz";
            else if (hz >= 1000)
                return $"{hz / 1000:F2} kHz";
            else
                return $"{hz:F2} Hz";
        }

        /// <summary>
        /// Format amplitude value
        /// </summary>
        public static string FormatAmplitude(double amplitude, bool isLogScale)
        {
            if (isLogScale)
                return $"{amplitude:F2} dB";
            else
                return $"{amplitude:F6}";
        }

        /// <summary>
        /// Show error message box
        /// </summary>
        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Show info message box
        /// </summary>
        public static void ShowInfo(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Show warning message box
        /// </summary>
        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Show confirmation dialog
        /// </summary>
        public static bool ShowConfirmation(string message, string title = "Confirm")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Execute action on UI thread
        /// </summary>
        public static void RunOnUiThread(Action action)
        {
            if (Application.Current?.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(action);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Clamp value between min and max
        /// </summary>
        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0) return min;
            if (value.CompareTo(max) > 0) return max;
            return value;
        }
    }
}
