using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Service for file operations
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Save uploaded file to staging area
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="displayName">Display name for the file</param>
        /// <returns>Staged file path</returns>
        Task<string> SaveToStagingAsync(string sourceFilePath, string displayName);

        /// <summary>
        /// Move file from staging to success folder
        /// </summary>
        /// <param name="stagingFilePath">Staged file path</param>
        /// <param name="recordId">FFT record ID</param>
        /// <returns>Success file path</returns>
        Task<string> MoveToSuccessAsync(string stagingFilePath, System.Guid recordId);

        /// <summary>
        /// Move file from staging to fail folder
        /// </summary>
        /// <param name="stagingFilePath">Staged file path</param>
        /// <param name="errorMessage">Error message</param>
        /// <returns>Fail file path</returns>
        Task<string> MoveToFailAsync(string stagingFilePath, string errorMessage);

        /// <summary>
        /// Ensure required directories exist
        /// </summary>
        void EnsureDirectoriesExist();

        /// <summary>
        /// Export FFT result to CSV
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="frequencies">Frequency array</param>
        /// <param name="amplitudes">Amplitude array</param>
        Task ExportToCsvAsync(string filePath, double[] frequencies, double[] amplitudes);
    }
}
