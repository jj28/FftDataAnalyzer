using FftDataAnalyzer.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Service for database operations
    /// </summary>
    public interface IDbService
    {
        // FFT Records
        Task<Guid> InsertFftRecordAsync(FftRecord record);
        Task<FftRecord> GetFftRecordAsync(Guid id);
        Task<List<FftRecord>> GetAllFftRecordsAsync();
        Task<bool> UpdateFftRecordAsync(FftRecord record);
        Task<bool> DeleteFftRecordAsync(Guid id);
        Task<List<FftRecord>> SearchFftRecordsAsync(string searchTerm, DateTime? startDate, DateTime? endDate);

        // FFT Samples
        Task BatchInsertFftSamplesAsync(Guid recordId, double[] frequencies, double[] amplitudes);
        Task<List<FftSample>> GetFftSamplesAsync(Guid recordId);
        Task<int> GetFftSampleCountAsync(Guid recordId);

        // Users
        Task<Guid> InsertUserAsync(User user);
        Task<User> GetUserByUsernameAsync(string username);
        Task<User> GetUserByIdAsync(Guid id);
        Task<bool> UpdateUserAsync(User user);
        Task<bool> UserExistsAsync(string username);

        // Database health
        Task<bool> TestConnectionAsync();
        Task InitializeDatabaseAsync();
    }
}
