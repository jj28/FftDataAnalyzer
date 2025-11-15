using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Services;
using NLog;
using System;
using System.Windows;
using System.Windows.Input;

namespace FftDataAnalyzer.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IAuthService _authService;

        private string _username;
        private string _email;
        private string _fullName;
        private string _statusMessage;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string FullName
        {
            get => _fullName;
            set => SetProperty(ref _fullName, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand RegisterCommand { get; }
        public ICommand CancelCommand { get; }

        public RegisterViewModel()
        {
            _authService = ServiceLocator.Instance.Resolve<IAuthService>();

            RegisterCommand = new RelayCommand(async param => await RegisterAsync(param), param => !IsLoading);
            CancelCommand = new RelayCommand(param => CloseWindow(param));
        }

        private async System.Threading.Tasks.Task RegisterAsync(object parameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    StatusMessage = "Please enter a username";
                    return;
                }

                if (string.IsNullOrWhiteSpace(Email))
                {
                    StatusMessage = "Please enter an email";
                    return;
                }

                // Get password from PasswordBox parameters (passed as array)
                var passwordBoxes = parameter as object[];
                if (passwordBoxes == null || passwordBoxes.Length < 2)
                {
                    StatusMessage = "Password boxes not found";
                    return;
                }

                var passwordBox = passwordBoxes[0] as System.Windows.Controls.PasswordBox;
                var confirmPasswordBox = passwordBoxes[1] as System.Windows.Controls.PasswordBox;

                if (passwordBox == null || confirmPasswordBox == null)
                {
                    StatusMessage = "Password boxes not found";
                    return;
                }

                if (string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    StatusMessage = "Please enter a password";
                    return;
                }

                if (passwordBox.Password != confirmPasswordBox.Password)
                {
                    StatusMessage = "Passwords do not match";
                    return;
                }

                if (passwordBox.Password.Length < 6)
                {
                    StatusMessage = "Password must be at least 6 characters";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Creating account...";

                var success = await _authService.RegisterAsync(Username, passwordBox.Password, Email, FullName);

                if (success)
                {
                    Logger.Info($"Registration successful: {Username}");
                    Utilities.ShowInfo("Account created successfully! You can now login.", "Success");

                    // Close window
                    CloseWindow(null);
                }
                else
                {
                    StatusMessage = "Registration failed. Username may already exist.";
                    Logger.Warn($"Registration failed: {Username}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Registration error: {ex.Message}";
                Logger.Error(ex, "Registration error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CloseWindow(object parameter)
        {
            foreach (Window window in Application.Current.Windows)
            {
                if (window.DataContext == this)
                {
                    window.Close();
                    break;
                }
            }
        }
    }
}
