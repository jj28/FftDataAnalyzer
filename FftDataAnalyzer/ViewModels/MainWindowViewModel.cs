using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Services;
using FftDataAnalyzer.Views;
using NLog;
using System;
using System.Windows;
using System.Windows.Input;

namespace FftDataAnalyzer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly IAuthService _authService;

        private object _currentPage;
        private string _currentPageTitle;

        public object CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public string CurrentPageTitle
        {
            get => _currentPageTitle;
            set => SetProperty(ref _currentPageTitle, value);
        }

        public string CurrentUsername => _authService?.CurrentUser?.FullName ?? _authService?.CurrentUser?.Username ?? "User";

        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateUploadCommand { get; }
        public ICommand NavigateSettingsCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainWindowViewModel()
        {
            _authService = ServiceLocator.Instance.Resolve<IAuthService>();

            NavigateHomeCommand = new RelayCommand(_ => NavigateToHome());
            NavigateUploadCommand = new RelayCommand(_ => NavigateToUpload());
            NavigateSettingsCommand = new RelayCommand(_ => NavigateToSettings());
            LogoutCommand = new RelayCommand(_ => Logout());

            // Navigate to home page by default
            NavigateToHome();
        }

        private void NavigateToHome()
        {
            CurrentPage = new HomePage();
            CurrentPageTitle = "Home - FFT Records";
            Logger.Info("Navigated to Home page");
        }

        private void NavigateToUpload()
        {
            CurrentPage = new UploadPage();
            CurrentPageTitle = "Upload FFT Data";
            Logger.Info("Navigated to Upload page");
        }

        private void NavigateToSettings()
        {
            CurrentPage = new SettingsPage();
            CurrentPageTitle = "Settings";
            Logger.Info("Navigated to Settings page");
        }

        private void Logout()
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to logout?",
                    "Confirm Logout",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _authService.Logout();
                    Logger.Info("User logged out");

                    // Show login window
                    var loginWindow = new LoginView();
                    loginWindow.Show();

                    // Close main window
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow)
                        {
                            window.Close();
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during logout");
                Utilities.ShowError($"Logout error: {ex.Message}");
            }
        }
    }
}
