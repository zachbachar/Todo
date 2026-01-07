using CityShob.ToDo.Client.Services;
using CityShob.ToDo.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace CityShob.ToDo.Client
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// Handles application startup, dependency injection configuration, and global exception handling.
    /// </summary>
    public partial class App : Application
    {
        #region Properties

        /// <summary>
        /// Gets the service provider for dependency injection.
        /// </summary>
        public IServiceProvider ServiceProvider { get; private set; }

        #endregion

        #region Application Lifecycle

        protected override void OnStartup(StartupEventArgs e)
        {
            // Setup global exception handling before anything else
            SetupExceptionHandling();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Resolve and show the main window
            try
            {
                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();

                // Start data loading asynchronously without blocking UI startup
                if (mainWindow.DataContext is MainViewModel vm)
                {
                    _ = vm.LoadDataAsync();
                }

                Log.Information("Client Application Started successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to resolve or show MainWindow. Application is aborting.");
                MessageBox.Show("A critical error occurred during startup. Please check the logs.", "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Client Application Exiting.");
            Log.CloseAndFlush();
            base.OnExit(e);
        }

        #endregion

        #region Configuration

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Configure Logging
            ConfigureLogging(services);

            // 2. Configuration
            string serverUrl = ConfigurationManager.AppSettings["ServerUrl"];
            if (string.IsNullOrEmpty(serverUrl))
            {
                serverUrl = "https://localhost:44307/";
                Log.Warning("ServerUrl not found in App.config. Defaulting to {DefaultUrl}", serverUrl);
            }

            // 3. Register Core Services
            services.AddSingleton(new HttpClient
            {
                BaseAddress = new Uri(serverUrl)
            });

            services.AddSingleton<ITodoService>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<TodoService>>();
                var httpClient = provider.GetRequiredService<HttpClient>();
                return new TodoService(httpClient, serverUrl, logger);
            });

            services.AddSingleton<IDataCacheService>(provider =>
            {
                // We map the implementation to the interface explicitly
                var logger = provider.GetRequiredService<ILogger<DataCacheService>>();
                return new DataCacheService(logger);
            });

            services.AddSingleton<IDispatcherService, WpfDispatcherService>();

            // 4. Register ViewModels
            services.AddTransient<MainViewModel>();

            // 5. Register Views (for injection)
            services.AddTransient<MainWindow>();
        }

        private void ConfigureLogging(IServiceCollection services)
        {
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CityShob.ToDo",
                "logs/client-.txt");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddSerilog(dispose: true);
            });
        }

        #endregion

        #region Exception Handling

        private void SetupExceptionHandling()
        {
            // 1. UI Thread Exceptions
            DispatcherUnhandledException += (sender, args) =>
            {
                Log.Fatal(args.Exception, "Unhandled UI Exception");
                // Optional: Notify user or attempt to recover
                // args.Handled = true; // Uncomment if you want to prevent crash
            };

            // 2. Background Thread Exceptions (Task)
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                Log.Error(args.Exception, "Unobserved Task Exception");
                args.SetObserved(); // Prevent process termination
            };

            // 3. Global AppDomain Exceptions (The Catch-All)
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var ex = args.ExceptionObject as Exception;
                Log.Fatal(ex, "Fatal AppDomain Exception. Terminating: {IsTerminating}", args.IsTerminating);
                Log.CloseAndFlush();
            };
        }

        #endregion
    }
}