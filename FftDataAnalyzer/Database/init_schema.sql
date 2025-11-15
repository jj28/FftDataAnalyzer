-- FFT Studio Database Schema Initialization Script
-- This script creates all required tables and indexes for the FFT Data Analyzer application
-- Run this script manually if you need to set up the database schema without running the app
-- Note: The application can automatically create these tables on first run

-- Create database (run this separately as a superuser if needed)
-- CREATE DATABASE fftdb;

-- Connect to the database
-- \c fftdb

-- ============================================================================
-- Users Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS users (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    email text,
    full_name text,
    created_at timestamptz NOT NULL DEFAULT now(),
    last_login_at timestamptz,
    is_active boolean NOT NULL DEFAULT true
);

-- Create index on username for faster lookups
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- ============================================================================
-- FFT Records Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS fft_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
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

-- Create indexes for common queries
CREATE INDEX IF NOT EXISTS idx_fft_records_created_at ON fft_records(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_fft_records_status ON fft_records(status);
CREATE INDEX IF NOT EXISTS idx_fft_records_created_by ON fft_records(created_by);

-- ============================================================================
-- FFT Samples Table
-- ============================================================================
CREATE TABLE IF NOT EXISTS fft_samples (
    id bigserial PRIMARY KEY,
    fft_record_id uuid NOT NULL REFERENCES fft_records(id) ON DELETE CASCADE,
    frequency double precision NOT NULL,
    amplitude double precision NOT NULL,
    sample_index integer NOT NULL
);

-- Create index for fast retrieval by record ID
CREATE INDEX IF NOT EXISTS idx_fft_samples_record_id ON fft_samples(fft_record_id);
CREATE INDEX IF NOT EXISTS idx_fft_samples_record_index ON fft_samples(fft_record_id, sample_index);

-- ============================================================================
-- Optional: Create a default admin user
-- ============================================================================
-- Password for this user is 'admin123' - CHANGE THIS IN PRODUCTION!
-- The password hash below is generated using PBKDF2 with SHA256
-- You can create users through the application's registration page instead

-- INSERT INTO users (id, username, password_hash, email, full_name, is_active)
-- VALUES (
--     gen_random_uuid(),
--     'admin',
--     'YOUR_HASHED_PASSWORD_HERE',  -- Generate this using the app
--     'admin@fft studio.local',
--     'Administrator',
--     true
-- )
-- ON CONFLICT (username) DO NOTHING;

-- ============================================================================
-- Helpful Queries for Administration
-- ============================================================================

-- View all users
-- SELECT id, username, email, full_name, created_at, last_login_at, is_active FROM users;

-- View all FFT records
-- SELECT id, display_name, sample_rate, sample_count, created_at, status FROM fft_records ORDER BY created_at DESC;

-- Count total FFT records
-- SELECT COUNT(*) as total_records FROM fft_records;

-- Count total samples stored
-- SELECT COUNT(*) as total_samples FROM fft_samples;

-- View storage size of tables
-- SELECT
--     tablename,
--     pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
-- FROM pg_tables
-- WHERE schemaname = 'public'
-- ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;

-- ============================================================================
-- Cleanup/Reset Queries (USE WITH CAUTION)
-- ============================================================================

-- Delete all FFT samples and records
-- TRUNCATE TABLE fft_samples, fft_records CASCADE;

-- Delete all users (except keep one admin)
-- DELETE FROM users WHERE username != 'admin';

-- Drop all tables (complete reset)
-- DROP TABLE IF EXISTS fft_samples CASCADE;
-- DROP TABLE IF EXISTS fft_records CASCADE;
-- DROP TABLE IF EXISTS users CASCADE;

-- ============================================================================
-- Performance Tuning (Optional)
-- ============================================================================

-- For large datasets, you may want to partition the fft_samples table
-- or use table compression to save storage space

-- Example: Analyze tables to update statistics
-- ANALYZE users;
-- ANALYZE fft_records;
-- ANALYZE fft_samples;

-- Example: Vacuum tables to reclaim storage
-- VACUUM ANALYZE users;
-- VACUUM ANALYZE fft_records;
-- VACUUM ANALYZE fft_samples;

COMMENT ON TABLE users IS 'User accounts for authentication and authorization';
COMMENT ON TABLE fft_records IS 'Metadata for FFT analysis records';
COMMENT ON TABLE fft_samples IS 'Individual frequency-amplitude data points from FFT analysis';

-- Grant permissions to application user (if using dedicated user)
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO fftuser;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO fftuser;
