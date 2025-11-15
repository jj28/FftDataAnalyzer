using FftDataAnalyzer.Data;
using FftDataAnalyzer.Helpers;
using FftDataAnalyzer.Services;
using FftDataAnalyzer.Views;
using NLog;
using System;
using System.Windows;

namespace FftDataAnalyzer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                Logger.Info("Application starting...");

                // Initialize services
                await InitializeServicesAsync();

                // Show login window
                var loginWindow = new LoginView();
                loginWindow.Show();

                Logger.Info("Application started successfully");
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Fatal error during application startup");
                MessageBox.Show($"Failed to start application: {ex.Message}", "Startup Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private async System.Threading.Tasks.Task InitializeServicesAsync()
        {
            var serviceLocator = ServiceLocator.Instance;

            // Load configuration
            var configService = new ConfigService();
            var config = await configService.LoadConfigAsync();
            serviceLocator.RegisterSingleton<IConfigService>(configService);
            serviceLocator.RegisterSingleton(config);

            // Register database service
            var dbService = new DbService(config.ConnectionString);
            serviceLocator.RegisterSingleton<IDbService>(dbService);

            // Initialize database (create tables if needed)
            try
            {
                await dbService.InitializeDatabaseAsync();
                Logger.Info("Database initialized");
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Database initialization failed - PostgreSQL may not be configured");
            }

            // Register other services
            serviceLocator.RegisterSingleton<IFftService>(new FftService());
            serviceLocator.RegisterSingleton<IFileService>(new FileService(config));
            serviceLocator.RegisterSingleton<IAuthService>(new AuthService(dbService));

            Logger.Info("All services registered");
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Logger.Info("Application shutting down");
            LogManager.Shutdown();
        }
    }
}
