using FftDataAnalyzer.Models;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Service for application configuration
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// Load configuration from file
        /// </summary>
        Task<AppConfig> LoadConfigAsync();

        /// <summary>
        /// Save configuration to file
        /// </summary>
        Task SaveConfigAsync(AppConfig config);

        /// <summary>
        /// Get current configuration
        /// </summary>
        AppConfig GetConfig();

        /// <summary>
        /// Update connection string
        /// </summary>
        Task UpdateConnectionStringAsync(string connectionString);
    }
}
