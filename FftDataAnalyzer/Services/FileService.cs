using FftDataAnalyzer.Models;
using NLog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Implementation of file operations service
    /// </summary>
    public class FileService : IFileService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly AppConfig _config;

        public FileService(AppConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            EnsureDirectoriesExist();
        }

        /// <summary>
        /// Ensure required directories exist
        /// </summary>
        public void EnsureDirectoriesExist()
        {
            try
            {
                Directory.CreateDirectory(_config.UploadPath);
                Directory.CreateDirectory(_config.SuccessPath);
                Directory.CreateDirectory(_config.FailPath);
                Directory.CreateDirectory(_config.StagingPath);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to create required directories");
                throw;
            }
        }

        /// <summary>
        /// Save uploaded file to staging area
        /// </summary>
        public async Task<string> SaveToStagingAsync(string sourceFilePath, string displayName)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                    throw new FileNotFoundException("Source file not found", sourceFilePath);

                var fileName = Path.GetFileName(sourceFilePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var stagedFileName = $"{timestamp}_{Guid.NewGuid():N}_{fileName}";
                var stagedFilePath = Path.Combine(_config.StagingPath, stagedFileName);

                await Task.Run(() => File.Copy(sourceFilePath, stagedFilePath, true));

                return stagedFilePath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to save file to staging: {sourceFilePath}");
                throw;
            }
        }

        /// <summary>
        /// Move file from staging to success folder
        /// </summary>
        public async Task<string> MoveToSuccessAsync(string stagingFilePath, Guid recordId)
        {
            try
            {
                if (!File.Exists(stagingFilePath))
                    throw new FileNotFoundException("Staged file not found", stagingFilePath);

                var fileName = Path.GetFileName(stagingFilePath);
                var successFileName = $"{recordId:N}_{fileName}";
                var successFilePath = Path.Combine(_config.SuccessPath, successFileName);

                await Task.Run(() =>
                {
                    File.Move(stagingFilePath, successFilePath);
                });

                // Create metadata JSON file
                var metadataPath = Path.ChangeExtension(successFilePath, ".json");
                var metadata = new
                {
                    RecordId = recordId,
                    OriginalFileName = fileName,
                    ProcessedAt = DateTime.Now,
                    Status = "Success"
                };

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(metadata, Newtonsoft.Json.Formatting.Indented);
                await Task.Run(() => File.WriteAllText(metadataPath, json));

                return successFilePath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to move file to success: {stagingFilePath}");
                throw;
            }
        }

        /// <summary>
        /// Move file from staging to fail folder
        /// </summary>
        public async Task<string> MoveToFailAsync(string stagingFilePath, string errorMessage)
        {
            try
            {
                if (!File.Exists(stagingFilePath))
                {
                    Logger.Warn($"Staged file not found for fail move: {stagingFilePath}");
                    return null;
                }

                var fileName = Path.GetFileName(stagingFilePath);
                var failFilePath = Path.Combine(_config.FailPath, fileName);

                await Task.Run(() =>
                {
                    File.Move(stagingFilePath, failFilePath);
                });

                // Create error log file
                var errorLogPath = Path.ChangeExtension(failFilePath, ".error.txt");
                var errorLog = new StringBuilder();
                errorLog.AppendLine($"Failed at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                errorLog.AppendLine($"Error: {errorMessage}");

                await Task.Run(() => File.WriteAllText(errorLogPath, errorLog.ToString()));

                return failFilePath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to move file to fail folder: {stagingFilePath}");
                throw;
            }
        }

        /// <summary>
        /// Export FFT result to CSV
        /// </summary>
        public async Task ExportToCsvAsync(string filePath, double[] frequencies, double[] amplitudes)
        {
            try
            {
                if (frequencies == null || amplitudes == null)
                    throw new ArgumentNullException("Frequencies and amplitudes cannot be null");

                if (frequencies.Length != amplitudes.Length)
                    throw new ArgumentException("Frequencies and amplitudes must have the same length");

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    // Write header
                    await writer.WriteLineAsync("FrequencyHz,Amplitude");

                    // Write data rows
                    for (int i = 0; i < frequencies.Length; i++)
                    {
                        await writer.WriteLineAsync($"{frequencies[i]:F6},{amplitudes[i]:F6}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to export to CSV: {filePath}");
                throw;
            }
        }
    }
}
