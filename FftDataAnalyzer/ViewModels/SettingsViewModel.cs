using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Models;
using FftDataAnalyzer.Services;
using NLog;
using System;
using System.Windows.Input;

namespace FftDataAnalyzer.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IConfigService _configService;
        private readonly IDbService _dbService;

        private AppConfig _config;
        private string _connectionString;
        private string _uploadPath;
        private string _successPath;
        private string _failPath;
        private int _retentionDays;
        private string _statusMessage;
        private bool _isTestingConnection;

        public string ConnectionString
        {
            get => _connectionString;
            set => SetProperty(ref _connectionString, value);
        }

        public string UploadPath
        {
            get => _uploadPath;
            set => SetProperty(ref _uploadPath, value);
        }

        public string SuccessPath
        {
            get => _successPath;
            set => SetProperty(ref _successPath, value);
        }

        public string FailPath
        {
            get => _failPath;
            set => SetProperty(ref _failPath, value);
        }

        public int RetentionDays
        {
            get => _retentionDays;
            set => SetProperty(ref _retentionDays, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsTestingConnection
        {
            get => _isTestingConnection;
            set => SetProperty(ref _isTestingConnection, value);
        }

        public ICommand TestConnectionCommand { get; }
        public ICommand SaveSettingsCommand { get; }
        public ICommand ResetToDefaultsCommand { get; }

        public SettingsViewModel()
        {
            _configService = ServiceLocator.Instance.Resolve<IConfigService>();
            _dbService = ServiceLocator.Instance.Resolve<IDbService>();

            TestConnectionCommand = new RelayCommand(async _ => await TestConnectionAsync(), _ => !IsTestingConnection);
            SaveSettingsCommand = new RelayCommand(async _ => await SaveSettingsAsync());
            ResetToDefaultsCommand = new RelayCommand(_ => ResetToDefaults());

            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                _config = _configService.GetConfig();

                ConnectionString = _config.ConnectionString;
                UploadPath = _config.UploadPath;
                SuccessPath = _config.SuccessPath;
                FailPath = _config.FailPath;
                RetentionDays = _config.RetentionDays;

                StatusMessage = "Settings loaded";
                Logger.Info("Settings loaded");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading settings: {ex.Message}";
                Logger.Error(ex, "Failed to load settings");
                Utilities.ShowError($"Failed to load settings: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task TestConnectionAsync()
        {
            try
            {
                IsTestingConnection = true;
                StatusMessage = "Testing database connection...";

                // Create temporary DbService with current connection string
                var tempDbService = new Data.DbService(ConnectionString);
                var success = await tempDbService.TestConnectionAsync();

                if (success)
                {
                    StatusMessage = "✅ Database connection successful!";
                    Logger.Info("Database connection test successful");
                    Utilities.ShowInfo("Database connection test successful!");
                }
                else
                {
                    StatusMessage = "❌ Database connection failed";
                    Logger.Warn("Database connection test failed");
                    Utilities.ShowError("Database connection failed. Please check your connection string.");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Connection error: {ex.Message}";
                Logger.Error(ex, "Database connection test error");
                Utilities.ShowError($"Database connection error:\n\n{ex.Message}\n\nPlease verify your connection string and ensure PostgreSQL is running.");
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        private async System.Threading.Tasks.Task SaveSettingsAsync()
        {
            try
            {
                StatusMessage = "Saving settings...";

                // Update config object
                _config.ConnectionString = ConnectionString;
                _config.UploadPath = UploadPath;
                _config.SuccessPath = SuccessPath;
                _config.FailPath = FailPath;
                _config.RetentionDays = RetentionDays;

                // Save to file
                await _configService.SaveConfigAsync(_config);

                StatusMessage = "✅ Settings saved successfully!";
                Logger.Info("Settings saved");
                Utilities.ShowInfo("Settings saved successfully!\n\nRestart the application for all changes to take effect.");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving settings: {ex.Message}";
                Logger.Error(ex, "Failed to save settings");
                Utilities.ShowError($"Failed to save settings: {ex.Message}");
            }
        }

        private void ResetToDefaults()
        {
            try
            {
                var result = Utilities.ShowConfirmation(
                    "Reset all settings to default values?",
                    "Confirm Reset");

                if (!result)
                    return;

                ConnectionString = "Host=localhost;Port=5432;Database=fftdb;Username=postgres;Password=";
                UploadPath = @"C:\FFT\Upload";
                SuccessPath = @"C:\FFT\Upload\Success";
                FailPath = @"C:\FFT\Upload\Fail";
                RetentionDays = 30;

                StatusMessage = "Settings reset to defaults (not saved)";
                Logger.Info("Settings reset to defaults");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error resetting settings");
                Utilities.ShowError($"Error resetting settings: {ex.Message}");
            }
        }
    }
}
