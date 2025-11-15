using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Services;
using FftDataAnalyzer.Views;
using NLog;
using System;
using System.Windows;
using System.Windows.Input;

namespace FftDataAnalyzer.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IAuthService _authService;

        private string _username;
        private string _statusMessage;
        private bool _isLoading;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
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

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }

        public LoginViewModel()
        {
            _authService = ServiceLocator.Instance.Resolve<IAuthService>();

            LoginCommand = new RelayCommand(async param => await LoginAsync(param), param => !IsLoading);
            RegisterCommand = new RelayCommand(NavigateToRegister, param => !IsLoading);
        }

        private async System.Threading.Tasks.Task LoginAsync(object parameter)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Username))
                {
                    StatusMessage = "Please enter a username";
                    return;
                }

                // Get password from PasswordBox (passed as parameter)
                var passwordBox = parameter as System.Windows.Controls.PasswordBox;
                if (passwordBox == null || string.IsNullOrWhiteSpace(passwordBox.Password))
                {
                    StatusMessage = "Please enter a password";
                    return;
                }

                IsLoading = true;
                StatusMessage = "Logging in...";

                var success = await _authService.LoginAsync(Username, passwordBox.Password);

                if (success)
                {
                    Logger.Info($"Login successful: {Username}");

                    // Open main window
                    var mainWindow = new MainWindow();
                    mainWindow.Show();

                    // Close login window
                    Application.Current.Windows[0]?.Close();
                }
                else
                {
                    StatusMessage = "Invalid username or password";
                    Logger.Warn($"Login failed: {Username}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Login error: {ex.Message}";
                Logger.Error(ex, "Login error");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void NavigateToRegister(object parameter)
        {
            var registerWindow = new RegisterView();
            registerWindow.ShowDialog();
        }
    }
}
