using FftDataAnalyzer.Models;
using Newtonsoft.Json;
using NLog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Service for application configuration management
    /// </summary>
    public class ConfigService : IConfigService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private const string ConfigFileName = "appsettings.json";
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "FFTStudio",
            ConfigFileName);

        private AppConfig _currentConfig;

        public ConfigService()
        {
            EnsureConfigDirectoryExists();
        }

        private void EnsureConfigDirectoryExists()
        {
            var directory = Path.GetDirectoryName(ConfigFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                Logger.Info($"Created config directory: {directory}");
            }
        }

        /// <summary>
        /// Load configuration from file
        /// </summary>
        public async Task<AppConfig> LoadConfigAsync()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    var json = await Task.Run(() => File.ReadAllText(ConfigFilePath));
                    _currentConfig = JsonConvert.DeserializeObject<AppConfig>(json);
                    Logger.Info("Configuration loaded from file");
                }
                else
                {
                    // Create default configuration
                    _currentConfig = new AppConfig
                    {
                        ConnectionString = "Host=localhost;Port=5432;Database=fftdb;Username=postgres;Password=",
                        UploadPath = @"C:\FFT\Upload",
                        SuccessPath = @"C:\FFT\Upload\Success",
                        FailPath = @"C:\FFT\Upload\Fail",
                        StagingPath = @"C:\FFT\Upload\Staging",
                        RetentionDays = 30
                    };

                    await SaveConfigAsync(_currentConfig);
                    Logger.Info("Created default configuration file");
                }

                return _currentConfig;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load configuration");
                throw;
            }
        }

        /// <summary>
        /// Save configuration to file
        /// </summary>
        public async Task SaveConfigAsync(AppConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config, Formatting.Indented);
                await Task.Run(() => File.WriteAllText(ConfigFilePath, json));
                _currentConfig = config;
                Logger.Info("Configuration saved to file");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to save configuration");
                throw;
            }
        }

        /// <summary>
        /// Get current configuration
        /// </summary>
        public AppConfig GetConfig()
        {
            if (_currentConfig == null)
            {
                // Load synchronously if not loaded
                _currentConfig = LoadConfigAsync().GetAwaiter().GetResult();
            }

            return _currentConfig;
        }

        /// <summary>
        /// Update connection string
        /// </summary>
        public async Task UpdateConnectionStringAsync(string connectionString)
        {
            var config = GetConfig();
            config.ConnectionString = connectionString;
            await SaveConfigAsync(config);
            Logger.Info("Connection string updated");
        }
    }
}
