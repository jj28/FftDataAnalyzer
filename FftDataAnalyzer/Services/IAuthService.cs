using FftDataAnalyzer.Models;
using System.Threading.Tasks;

namespace FftDataAnalyzer.Services
{
    /// <summary>
    /// Service for authentication and authorization
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Current logged-in user
        /// </summary>
        User CurrentUser { get; }

        /// <summary>
        /// Check if user is authenticated
        /// </summary>
        bool IsAuthenticated { get; }

        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Plain text password</param>
        /// <returns>True if authentication successful</returns>
        Task<bool> LoginAsync(string username, string password);

        /// <summary>
        /// Register a new user
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Plain text password</param>
        /// <param name="email">Email</param>
        /// <param name="fullName">Full name</param>
        /// <returns>True if registration successful</returns>
        Task<bool> RegisterAsync(string username, string password, string email, string fullName);

        /// <summary>
        /// Logout current user
        /// </summary>
        void Logout();

        /// <summary>
        /// Hash a password using PBKDF2
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <returns>Hashed password with salt</returns>
        string HashPassword(string password);

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        /// <param name="password">Plain text password</param>
        /// <param name="hashedPassword">Hashed password with salt</param>
        /// <returns>True if password matches</returns>
        bool VerifyPassword(string password, string hashedPassword);
    }
}
