using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Models;
using FftDataAnalyzer.Services;
using Microsoft.Win32;
using NLog;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace FftDataAnalyzer.ViewModels
{
    public class UploadViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IFileService _fileService;
        private readonly IFftService _fftService;
        private readonly IDbService _dbService;
        private readonly IAuthService _authService;

        private string _selectedFilePath;
        private string _displayName;
        private int _sampleRate = 25600;
        private bool _autoDetectSampleRate = true;
        private WindowType _windowType = WindowType.Hann;
        private bool _isProcessing;
        private string _statusMessage;
        private double _progressValue;

        public string SelectedFilePath
        {
            get => _selectedFilePath;
            set
            {
                if (SetProperty(ref _selectedFilePath, value))
                {
                    // Auto-generate display name from filename
                    if (!string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(DisplayName))
                    {
                        DisplayName = Path.GetFileNameWithoutExtension(value);
                    }
                }
            }
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public int SampleRate
        {
            get => _sampleRate;
            set => SetProperty(ref _sampleRate, value);
        }

        public bool AutoDetectSampleRate
        {
            get => _autoDetectSampleRate;
            set => SetProperty(ref _autoDetectSampleRate, value);
        }

        public WindowType WindowType
        {
            get => _windowType;
            set => SetProperty(ref _windowType, value);
        }

        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        public ICommand BrowseFileCommand { get; }
        public ICommand ProcessFileCommand { get; }
        public ICommand ClearCommand { get; }

        public UploadViewModel()
        {
            _fileService = ServiceLocator.Instance.Resolve<IFileService>();
            _fftService = ServiceLocator.Instance.Resolve<IFftService>();
            _dbService = ServiceLocator.Instance.Resolve<IDbService>();
            _authService = ServiceLocator.Instance.Resolve<IAuthService>();

            BrowseFileCommand = new RelayCommand(_ => BrowseFile());
            ProcessFileCommand = new RelayCommand(async _ => await ProcessFileAsync(), _ => CanProcessFile());
            ClearCommand = new RelayCommand(_ => Clear());

            StatusMessage = "Ready to upload FFT data";
        }

        private void BrowseFile()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "CSV Files (*.csv)|*.csv|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Select FFT Data File"
                };

                if (dialog.ShowDialog() == true)
                {
                    SelectedFilePath = dialog.FileName;
                    StatusMessage = $"Selected: {Path.GetFileName(SelectedFilePath)}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error browsing for file");
                Utilities.ShowError($"Error selecting file: {ex.Message}");
            }
        }

        private bool CanProcessFile()
        {
            return !string.IsNullOrWhiteSpace(SelectedFilePath) &&
                   !string.IsNullOrWhiteSpace(DisplayName) &&
                   !IsProcessing;
        }

        private async System.Threading.Tasks.Task ProcessFileAsync()
        {
            try
            {
                IsProcessing = true;
                ProgressValue = 0;
                StatusMessage = "Starting file processing...";

                // Step 1: Save to staging
                ProgressValue = 10;
                StatusMessage = "Saving file to staging area...";
                var stagedPath = await _fileService.SaveToStagingAsync(SelectedFilePath, DisplayName);

                // Step 2: Parse CSV
                ProgressValue = 25;
                StatusMessage = "Parsing CSV file...";
                var parseResult = CsvParser.ParseAuto(stagedPath);

                if (!parseResult.Success)
                {
                    await _fileService.MoveToFailAsync(stagedPath, parseResult.ErrorMessage);
                    throw new Exception($"Failed to parse CSV: {parseResult.ErrorMessage}");
                }

                // Step 3: Detect or use sample rate
                ProgressValue = 40;
                if (AutoDetectSampleRate)
                {
                    StatusMessage = "Detecting sample rate...";
                    var detectedRate = CsvParser.DetectSampleRate(stagedPath);
                    if (detectedRate.HasValue)
                    {
                        SampleRate = detectedRate.Value;
                    }
                }

                // Step 4: Compute FFT (if time-domain data)
                FftResult fftResult = null;
                if (parseResult.DataType == CsvDataType.TimeDomain)
                {
                    ProgressValue = 50;
                    StatusMessage = $"Computing FFT on {parseResult.SampleCount} samples...";

                    fftResult = await _fftService.ComputeFftAsync(
                        parseResult.Samples,
                        SampleRate,
                        new FftOptions
                        {
                            WindowType = WindowType,
                            UseLogScale = false
                        });
                }

                // Step 5: Create FFT record
                ProgressValue = 70;
                StatusMessage = "Saving to database...";

                var record = new FftRecord
                {
                    DisplayName = DisplayName,
                    SourceFilename = Path.GetFileName(SelectedFilePath),
                    SampleRate = SampleRate,
                    SampleCount = parseResult.SampleCount,
                    CreatedBy = _authService.CurrentUser?.Username,
                    Status = "Success"
                };

                if (fftResult != null && fftResult.Peaks.Count > 0)
                {
                    var topPeak = fftResult.Peaks[0];
                    record.PeakFrequency = topPeak.Frequency;
                    record.PeakAmplitude = topPeak.Amplitude;
                }

                var recordId = await _dbService.InsertFftRecordAsync(record);

                // Step 6: Save samples
                ProgressValue = 85;
                StatusMessage = "Saving FFT samples...";

                if (fftResult != null)
                {
                    await _dbService.BatchInsertFftSamplesAsync(recordId, fftResult.Frequencies, fftResult.Amplitudes);
                }
                else if (parseResult.DataType == CsvDataType.FrequencyDomain)
                {
                    await _dbService.BatchInsertFftSamplesAsync(recordId, parseResult.Frequencies, parseResult.Amplitudes);
                }

                // Step 7: Move to success folder
                ProgressValue = 95;
                StatusMessage = "Finalizing...";
                await _fileService.MoveToSuccessAsync(stagedPath, recordId);

                ProgressValue = 100;
                StatusMessage = $"✅ Processing complete! Record '{DisplayName}' created successfully.";

                Utilities.ShowInfo($"FFT data processed successfully!\n\nRecord: {DisplayName}\nSamples: {parseResult.SampleCount:N0}\nPeaks found: {fftResult?.Peaks.Count ?? 0}");

                // Clear form
                Clear();
            }
            catch (Exception ex)
            {
                ProgressValue = 0;
                StatusMessage = $"❌ Error: {ex.Message}";
                Logger.Error(ex, "File processing failed");
                Utilities.ShowError($"Failed to process file:\n\n{ex.Message}\n\nCheck logs for details.");
            }
            finally
            {
                IsProcessing = false;
            }
        }

        private void Clear()
        {
            SelectedFilePath = null;
            DisplayName = string.Empty;
            SampleRate = 25600;
            AutoDetectSampleRate = true;
            WindowType = WindowType.Hann;
            ProgressValue = 0;
            StatusMessage = "Ready to upload FFT data";
        }

        public void HandleFileDrop(string[] files)
        {
            if (files != null && files.Length > 0)
            {
                SelectedFilePath = files[0];
                StatusMessage = $"Dropped: {Path.GetFileName(SelectedFilePath)}";
            }
        }
    }
}
