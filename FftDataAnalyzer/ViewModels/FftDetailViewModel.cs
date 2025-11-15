using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Models;
using FftDataAnalyzer.Services;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace FftDataAnalyzer.ViewModels
{
    public class FftDetailViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IDbService _dbService;
        private readonly IFileService _fileService;
        private readonly Guid _recordId;

        private FftRecord _record;
        private ObservableCollection<FftSample> _samples;
        private ObservableCollection<PeakInfo> _peaks;
        private bool _isLoading;
        private string _statusMessage;

        public FftRecord Record
        {
            get => _record;
            set => SetProperty(ref _record, value);
        }

        public ObservableCollection<FftSample> Samples
        {
            get => _samples;
            set => SetProperty(ref _samples, value);
        }

        public ObservableCollection<PeakInfo> Peaks
        {
            get => _peaks;
            set => SetProperty(ref _peaks, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand ExportCsvCommand { get; }
        public ICommand RefreshCommand { get; }

        public FftDetailViewModel(Guid recordId)
        {
            _recordId = recordId;
            _dbService = ServiceLocator.Instance.Resolve<IDbService>();
            _fileService = ServiceLocator.Instance.Resolve<IFileService>();

            Samples = new ObservableCollection<FftSample>();
            Peaks = new ObservableCollection<PeakInfo>();

            ExportCsvCommand = new RelayCommand(async _ => await ExportToCsvAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());

            _ = LoadDataAsync();
        }

        public async System.Threading.Tasks.Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading FFT data...";

                // Load record
                Record = await _dbService.GetFftRecordAsync(_recordId);
                if (Record == null)
                {
                    StatusMessage = "Record not found";
                    Utilities.ShowError($"FFT record not found: {_recordId}");
                    return;
                }

                // Load samples
                var samples = await _dbService.GetFftSamplesAsync(_recordId);
                Samples.Clear();
                foreach (var sample in samples)
                {
                    Samples.Add(sample);
                }

                // Find peaks
                FindPeaks();

                StatusMessage = $"Loaded {samples.Count:N0} samples with {Peaks.Count} peaks";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Logger.Error(ex, $"Failed to load FFT detail for record {_recordId}");
                Utilities.ShowError($"Failed to load FFT data: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FindPeaks()
        {
            try
            {
                Peaks.Clear();

                if (Samples.Count < 3)
                    return;

                var sampleList = Samples.OrderBy(s => s.SampleIndex).ToList();

                // Simple peak detection: find local maxima
                for (int i = 1; i < sampleList.Count - 1; i++)
                {
                    if (sampleList[i].Amplitude > sampleList[i - 1].Amplitude &&
                        sampleList[i].Amplitude > sampleList[i + 1].Amplitude)
                    {
                        Peaks.Add(new PeakInfo
                        {
                            Frequency = sampleList[i].Frequency,
                            Amplitude = sampleList[i].Amplitude,
                            Index = i
                        });
                    }
                }

                // Sort by amplitude and take top 10
                var topPeaks = Peaks.OrderByDescending(p => p.Amplitude).Take(10).OrderBy(p => p.Frequency).ToList();
                Peaks.Clear();
                foreach (var peak in topPeaks)
                {
                    Peaks.Add(peak);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error finding peaks");
            }
        }

        private async System.Threading.Tasks.Task ExportToCsvAsync()
        {
            try
            {
                if (Samples == null || Samples.Count == 0)
                {
                    Utilities.ShowWarning("No data to export");
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv",
                    FileName = $"{Record?.DisplayName ?? "FFT_Data"}_FFT.csv",
                    Title = "Export FFT Data to CSV"
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = "Exporting to CSV...";

                    var frequencies = Samples.Select(s => s.Frequency).ToArray();
                    var amplitudes = Samples.Select(s => s.Amplitude).ToArray();

                    await _fileService.ExportToCsvAsync(dialog.FileName, frequencies, amplitudes);

                    StatusMessage = $"Exported {Samples.Count:N0} samples to CSV";
                    Utilities.ShowInfo($"Successfully exported {Samples.Count:N0} samples to:\n{dialog.FileName}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Export failed: {ex.Message}";
                Logger.Error(ex, "Failed to export FFT data to CSV");
                Utilities.ShowError($"Failed to export CSV: {ex.Message}");
            }
        }
    }
}
