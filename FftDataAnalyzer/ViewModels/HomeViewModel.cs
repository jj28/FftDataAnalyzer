using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Models;
using FftDataAnalyzer.Services;
using FftDataAnalyzer.Views;
using NLog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FftDataAnalyzer.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IDbService _dbService;
        private readonly IAuthService _authService;

        private ObservableCollection<FftRecord> _fftRecords;
        private FftRecord _selectedRecord;
        private string _searchText;
        private bool _isLoading;
        private string _statusMessage;

        public ObservableCollection<FftRecord> FftRecords
        {
            get => _fftRecords;
            set => SetProperty(ref _fftRecords, value);
        }

        public FftRecord SelectedRecord
        {
            get => _selectedRecord;
            set => SetProperty(ref _selectedRecord, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    SearchCommand.Execute(null);
                }
            }
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

        public ICommand LoadRecordsCommand { get; }
        public ICommand ViewRecordCommand { get; }
        public ICommand DeleteRecordCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand RefreshCommand { get; }

        public HomeViewModel()
        {
            _dbService = ServiceLocator.Instance.Resolve<IDbService>();
            _authService = ServiceLocator.Instance.Resolve<IAuthService>();

            FftRecords = new ObservableCollection<FftRecord>();

            LoadRecordsCommand = new RelayCommand(async _ => await LoadRecordsAsync());
            ViewRecordCommand = new RelayCommand(ViewRecord, _ => SelectedRecord != null);
            DeleteRecordCommand = new RelayCommand(async param => await DeleteRecordAsync(param));
            SearchCommand = new RelayCommand(async _ => await SearchRecordsAsync());
            RefreshCommand = new RelayCommand(async _ => await LoadRecordsAsync());

            // Load records on initialization
            LoadRecordsCommand.Execute(null);
        }

        private async System.Threading.Tasks.Task LoadRecordsAsync()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading FFT records...";

                var records = await _dbService.GetAllFftRecordsAsync();

                FftRecords.Clear();
                foreach (var record in records)
                {
                    FftRecords.Add(record);
                }

                StatusMessage = $"Loaded {records.Count} records";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading records: {ex.Message}";
                Logger.Error(ex, "Failed to load FFT records");
                Utilities.ShowError($"Failed to load FFT records: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async System.Threading.Tasks.Task SearchRecordsAsync()
        {
            try
            {
                IsLoading = true;

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    await LoadRecordsAsync();
                    return;
                }

                StatusMessage = $"Searching for: {SearchText}";

                var records = await _dbService.SearchFftRecordsAsync(SearchText, null, null);

                FftRecords.Clear();
                foreach (var record in records)
                {
                    FftRecords.Add(record);
                }

                StatusMessage = $"Found {records.Count} records matching '{SearchText}'";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Search error: {ex.Message}";
                Logger.Error(ex, "Search failed");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ViewRecord(object parameter)
        {
            try
            {
                var record = parameter as FftRecord ?? SelectedRecord;
                if (record == null)
                {
                    Utilities.ShowWarning("Please select a record to view");
                    return;
                }

                var detailWindow = new FftDetailView(record.Id);
                detailWindow.ShowDialog();

                // Refresh the list after closing detail window
                RefreshCommand.Execute(null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to open FFT detail");
                Utilities.ShowError($"Failed to open FFT detail: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task DeleteRecordAsync(object parameter)
        {
            try
            {
                var record = parameter as FftRecord ?? SelectedRecord;
                if (record == null)
                {
                    Utilities.ShowWarning("Please select a record to delete");
                    return;
                }

                var result = MessageBox.Show(
                    $"Are you sure you want to delete '{record.DisplayName}'?\n\nThis will permanently delete the record and all associated samples.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                IsLoading = true;
                StatusMessage = $"Deleting {record.DisplayName}...";

                var success = await _dbService.DeleteFftRecordAsync(record.Id);

                if (success)
                {
                    FftRecords.Remove(record);
                    StatusMessage = "Record deleted successfully";
                    Utilities.ShowInfo("Record deleted successfully");
                }
                else
                {
                    StatusMessage = "Failed to delete record";
                    Utilities.ShowError("Failed to delete record");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Delete error: {ex.Message}";
                Logger.Error(ex, "Failed to delete FFT record");
                Utilities.ShowError($"Failed to delete record: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
