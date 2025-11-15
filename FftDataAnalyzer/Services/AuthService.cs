using FftDataAnalyzer.Models;
using NLog;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Authentication service implementation with PBKDF2 password hashing
    /// </summary>
    public class AuthService : IAuthService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IDbService _dbService;

        private const int SaltSize = 16; // 128 bits
        private const int HashSize = 20; // 160 bits
        private const int Iterations = 10000;

        public User CurrentUser { get; private set; }
        public bool IsAuthenticated => CurrentUser != null;

        public AuthService(IDbService dbService)
        {
            _dbService = dbService ?? throw new ArgumentNullException(nameof(dbService));
        }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    Logger.Warn("Login attempt with empty username or password");
                    return false;
                }

                var user = await _dbService.GetUserByUsernameAsync(username);

                if (user == null)
                {
                    Logger.Warn($"Login failed: User not found - {username}");
                    return false;
                }

                if (!user.IsActive)
                {
                    Logger.Warn($"Login failed: User is inactive - {username}");
                    return false;
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    Logger.Warn($"Login failed: Invalid password - {username}");
                    return false;
                }

                // Update last login time
                user.LastLoginAt = DateTime.Now;
                await _dbService.UpdateUserAsync(user);

                CurrentUser = user;
                Logger.Info($"User logged in successfully: {username}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Login error for user: {username}");
                return false;
            }
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        public async Task<bool> RegisterAsync(string username, string password, string email, string fullName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    Logger.Warn("Registration attempt with empty username or password");
                    return false;
                }

                // Check if user already exists
                if (await _dbService.UserExistsAsync(username))
                {
                    Logger.Warn($"Registration failed: Username already exists - {username}");
                    return false;
                }

                // Create new user
                var user = new User
                {
                    Username = username,
                    PasswordHash = HashPassword(password),
                    Email = email,
                    FullName = fullName,
                    IsActive = true
                };

                await _dbService.InsertUserAsync(user);
                Logger.Info($"User registered successfully: {username}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Registration error for user: {username}");
                return false;
            }
        }

        /// <summary>
        /// Logout current user
        /// </summary>
        public void Logout()
        {
            if (CurrentUser != null)
            {
                Logger.Info($"User logged out: {CurrentUser.Username}");
                CurrentUser = null;
            }
        }

        /// <summary>
        /// Hash a password using PBKDF2
        /// </summary>
        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // Generate random salt
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash password with salt using PBKDF2
            // Note: Using default SHA-1 for .NET Framework 4.8 compatibility
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);

                // Combine salt and hash
                byte[] hashBytes = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, hashBytes, 0, SaltSize);
                Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                // Convert to base64 for storage
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                Logger.Debug("VerifyPassword: password is null or empty");
                return false;
            }

            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                Logger.Debug("VerifyPassword: hashedPassword is null or empty");
                return false;
            }

            try
            {
                Logger.Debug($"VerifyPassword: Input password length = {password.Length}, HashedPassword length = {hashedPassword.Length}");

                // Extract hash bytes from base64
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);
                Logger.Debug($"VerifyPassword: Decoded hash bytes length = {hashBytes.Length}");

                // Extract salt from stored hash
                byte[] salt = new byte[SaltSize];
                Array.Copy(hashBytes, 0, salt, 0, SaltSize);

                // Hash the input password with the extracted salt
                // Note: Using default SHA-1 for .NET Framework 4.8 compatibility
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                {
                    byte[] hash = pbkdf2.GetBytes(HashSize);

                    Logger.Debug($"VerifyPassword: Computed hash length = {hash.Length}");
                    Logger.Debug($"VerifyPassword: Stored hash preview = {Convert.ToBase64String(hashBytes, SaltSize, Math.Min(8, HashSize))}");
                    Logger.Debug($"VerifyPassword: Computed hash preview = {Convert.ToBase64String(hash, 0, Math.Min(8, HashSize))}");

                    // Compare the computed hash with the stored hash
                    for (int i = 0; i < HashSize; i++)
                    {
                        if (hashBytes[i + SaltSize] != hash[i])
                        {
                            Logger.Debug($"VerifyPassword: Hash mismatch at byte {i}");
                            return false;
                        }
                    }

                    Logger.Debug("VerifyPassword: Hash match successful");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error verifying password");
                return false;
            }
        }
    }
}
