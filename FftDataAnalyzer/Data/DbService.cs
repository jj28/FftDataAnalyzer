using Dapper;
using FftDataAnalyzer.Models;
using NLog;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Data
{
    /// <summary>
    /// Database service implementation using Dapper and Npgsql
    /// </summary>
    public class DbService : Services.IDbService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;

        public DbService(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        private IDbConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        #region FFT Records

        public async Task<Guid> InsertFftRecordAsync(FftRecord record)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"
                        INSERT INTO fft_records
                        (id, display_name, source_filename, sample_rate, sample_count,
                         created_at, created_by, status, notes, peak_frequency, peak_amplitude)
                        VALUES
                        (@Id, @DisplayName, @SourceFilename, @SampleRate, @SampleCount,
                         @CreatedAt, @CreatedBy, @Status, @Notes, @PeakFrequency, @PeakAmplitude)
                        RETURNING id";

                    var id = await conn.QuerySingleAsync<Guid>(sql, record);
                    return id;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to insert FFT record");
                throw;
            }
        }

        public async Task<FftRecord> GetFftRecordAsync(Guid id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"SELECT
                        id AS Id,
                        display_name AS DisplayName,
                        source_filename AS SourceFilename,
                        sample_rate AS SampleRate,
                        sample_count AS SampleCount,
                        created_at AS CreatedAt,
                        created_by AS CreatedBy,
                        status AS Status,
                        notes AS Notes,
                        peak_frequency AS PeakFrequency,
                        peak_amplitude AS PeakAmplitude
                    FROM fft_records WHERE id = @Id";
                    return await conn.QuerySingleOrDefaultAsync<FftRecord>(sql, new { Id = id });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to get FFT record: {id}");
                throw;
            }
        }

        public async Task<List<FftRecord>> GetAllFftRecordsAsync()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"SELECT
                        id AS Id,
                        display_name AS DisplayName,
                        source_filename AS SourceFilename,
                        sample_rate AS SampleRate,
                        sample_count AS SampleCount,
                        created_at AS CreatedAt,
                        created_by AS CreatedBy,
                        status AS Status,
                        notes AS Notes,
                        peak_frequency AS PeakFrequency,
                        peak_amplitude AS PeakAmplitude
                    FROM fft_records ORDER BY created_at DESC";
                    var results = await conn.QueryAsync<FftRecord>(sql);
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to get all FFT records");
                throw;
            }
        }

        public async Task<bool> UpdateFftRecordAsync(FftRecord record)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"
                        UPDATE fft_records
                        SET display_name = @DisplayName,
                            source_filename = @SourceFilename,
                            sample_rate = @SampleRate,
                            sample_count = @SampleCount,
                            status = @Status,
                            notes = @Notes,
                            peak_frequency = @PeakFrequency,
                            peak_amplitude = @PeakAmplitude
                        WHERE id = @Id";

                    var rowsAffected = await conn.ExecuteAsync(sql, record);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to update FFT record: {record.Id}");
                throw;
            }
        }

        public async Task<bool> DeleteFftRecordAsync(Guid id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = "DELETE FROM fft_records WHERE id = @Id";
                    var rowsAffected = await conn.ExecuteAsync(sql, new { Id = id });
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to delete FFT record: {id}");
                throw;
            }
        }

        public async Task<List<FftRecord>> SearchFftRecordsAsync(string searchTerm, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = new StringBuilder(@"SELECT
                        id AS Id,
                        display_name AS DisplayName,
                        source_filename AS SourceFilename,
                        sample_rate AS SampleRate,
                        sample_count AS SampleCount,
                        created_at AS CreatedAt,
                        created_by AS CreatedBy,
                        status AS Status,
                        notes AS Notes,
                        peak_frequency AS PeakFrequency,
                        peak_amplitude AS PeakAmplitude
                    FROM fft_records WHERE 1=1");
                    var parameters = new DynamicParameters();

                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        sql.Append(" AND (display_name ILIKE @SearchTerm OR source_filename ILIKE @SearchTerm)");
                        parameters.Add("SearchTerm", $"%{searchTerm}%");
                    }

                    if (startDate.HasValue)
                    {
                        sql.Append(" AND created_at >= @StartDate");
                        parameters.Add("StartDate", startDate.Value);
                    }

                    if (endDate.HasValue)
                    {
                        sql.Append(" AND created_at <= @EndDate");
                        parameters.Add("EndDate", endDate.Value);
                    }

                    sql.Append(" ORDER BY created_at DESC");

                    var results = await conn.QueryAsync<FftRecord>(sql.ToString(), parameters);
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to search FFT records");
                throw;
            }
        }

        #endregion

        #region FFT Samples

        public async Task BatchInsertFftSamplesAsync(Guid recordId, double[] frequencies, double[] amplitudes)
        {
            try
            {
                if (frequencies == null || amplitudes == null)
                    throw new ArgumentNullException("Frequencies and amplitudes cannot be null");

                if (frequencies.Length != amplitudes.Length)
                    throw new ArgumentException("Frequencies and amplitudes must have the same length");

                using (var conn = GetConnection())
                {
                    conn.Open();
                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            var sql = @"
                                INSERT INTO fft_samples (fft_record_id, frequency, amplitude, sample_index)
                                VALUES (@RecordId, @Frequency, @Amplitude, @SampleIndex)";

                            var batchSize = 1000;
                            for (int i = 0; i < frequencies.Length; i += batchSize)
                            {
                                var batch = new List<object>();
                                var endIndex = Math.Min(i + batchSize, frequencies.Length);

                                for (int j = i; j < endIndex; j++)
                                {
                                    batch.Add(new
                                    {
                                        RecordId = recordId,
                                        Frequency = frequencies[j],
                                        Amplitude = amplitudes[j],
                                        SampleIndex = j
                                    });
                                }

                                await conn.ExecuteAsync(sql, batch, transaction);
                            }

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to batch insert FFT samples for record: {recordId}");
                throw;
            }
        }

        public async Task<List<FftSample>> GetFftSamplesAsync(Guid recordId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"SELECT
                        id AS Id,
                        fft_record_id AS FftRecordId,
                        frequency AS Frequency,
                        amplitude AS Amplitude,
                        sample_index AS SampleIndex
                    FROM fft_samples WHERE fft_record_id = @RecordId ORDER BY sample_index";
                    var results = await conn.QueryAsync<FftSample>(sql, new { RecordId = recordId });
                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to get FFT samples for record: {recordId}");
                throw;
            }
        }

        public async Task<int> GetFftSampleCountAsync(Guid recordId)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = "SELECT COUNT(*) FROM fft_samples WHERE fft_record_id = @RecordId";
                    return await conn.ExecuteScalarAsync<int>(sql, new { RecordId = recordId });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to get FFT sample count for record: {recordId}");
                throw;
            }
        }

        #endregion

        #region Users

        public async Task<Guid> InsertUserAsync(User user)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"
                        INSERT INTO users
                        (id, username, password_hash, email, full_name, created_at, last_login_at, is_active)
                        VALUES
                        (@Id, @Username, @PasswordHash, @Email, @FullName, @CreatedAt, @LastLoginAt, @IsActive)
                        RETURNING id";

                    var id = await conn.QuerySingleAsync<Guid>(sql, user);
                    return id;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to insert user: {user.Username}");
                throw;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"SELECT
                        id AS Id,
                        username AS Username,
                        password_hash AS PasswordHash,
                        email AS Email,
                        full_name AS FullName,
                        created_at AS CreatedAt,
                        last_login_at AS LastLoginAt,
                        is_active AS IsActive
                    FROM users WHERE username = @Username";
                    return await conn.QuerySingleOrDefaultAsync<User>(sql, new { Username = username });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to get user by username: {username}");
                throw;
            }
        }

        public async Task<User> GetUserByIdAsync(Guid id)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"SELECT
                        id AS Id,
                        username AS Username,
                        password_hash AS PasswordHash,
                        email AS Email,
                        full_name AS FullName,
                        created_at AS CreatedAt,
                        last_login_at AS LastLoginAt,
                        is_active AS IsActive
                    FROM users WHERE id = @Id";
                    return await conn.QuerySingleOrDefaultAsync<User>(sql, new { Id = id });
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to get user by ID: {id}");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = @"
                        UPDATE users
                        SET username = @Username,
                            password_hash = @PasswordHash,
                            email = @Email,
                            full_name = @FullName,
                            last_login_at = @LastLoginAt,
                            is_active = @IsActive
                        WHERE id = @Id";

                    var rowsAffected = await conn.ExecuteAsync(sql, user);
                    return rowsAffected > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to update user: {user.Id}");
                throw;
            }
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            try
            {
                using (var conn = GetConnection())
                {
                    var sql = "SELECT COUNT(*) FROM users WHERE username = @Username";
                    var count = await conn.ExecuteScalarAsync<int>(sql, new { Username = username });
                    return count > 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Failed to check if user exists: {username}");
                throw;
            }
        }

        #endregion

        #region Database Health

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();
                    var result = await conn.ExecuteScalarAsync<int>("SELECT 1");
                    return result == 1;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Database connection test failed");
                return false;
            }
        }

        public async Task InitializeDatabaseAsync()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    conn.Open();

                    // Create tables if they don't exist
                    var createTablesScript = @"
                        -- Create users table
                        CREATE TABLE IF NOT EXISTS users (
                            id uuid PRIMARY KEY,
                            username text NOT NULL UNIQUE,
                            password_hash text NOT NULL,
                            email text,
                            full_name text,
                            created_at timestamptz NOT NULL DEFAULT now(),
                            last_login_at timestamptz,
                            is_active boolean NOT NULL DEFAULT true
                        );

                        -- Create fft_records table
                        CREATE TABLE IF NOT EXISTS fft_records (
                            id uuid PRIMARY KEY,
                            display_name text NOT NULL,
                            source_filename text,
                            sample_rate integer NOT NULL,
                            sample_count integer NOT NULL,
                            created_at timestamptz NOT NULL DEFAULT now(),
                            created_by text,
                            status text,
                            notes text,
                            peak_frequency double precision,
                            peak_amplitude double precision
                        );

                        -- Create fft_samples table
                        CREATE TABLE IF NOT EXISTS fft_samples (
                            id bigserial PRIMARY KEY,
                            fft_record_id uuid NOT NULL REFERENCES fft_records(id) ON DELETE CASCADE,
                            frequency double precision NOT NULL,
                            amplitude double precision NOT NULL,
                            sample_index integer NOT NULL
                        );

                        -- Create indexes
                        CREATE INDEX IF NOT EXISTS idx_fft_record_id ON fft_samples(fft_record_id);
                        CREATE INDEX IF NOT EXISTS idx_fft_records_created_at ON fft_records(created_at DESC);
                        CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
                    ";

                    await conn.ExecuteAsync(createTablesScript);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to initialize database");
                throw;
            }
        }

        #endregion
    }
}
