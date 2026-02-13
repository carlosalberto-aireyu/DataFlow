using DataFlow.Core.Data;
using DataFlow.Core.Extensions;
using DataFlow.UI.Pages;
using DataFlow.UI.Pages.Dialogs;
using DataFlow.UI.Services;
using DataFlow.UI.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using DataFlow.BL.Extensions;
using System;
using System.IO;
using System.Windows;

namespace DataFlow.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IHost? _host;
        private ILogger<App>? _logger;

        public IServiceProvider Services => _host?.Services
            ?? throw new InvalidOperationException("El host no está inicializado.");


        protected override async void OnStartup(StartupEventArgs e)
        {
            if (AlphaVersionService.IsExpired)
            {
                MessageBox.Show(
                    AlphaVersionService.GetExpiryMessage(),
                    "Versión ALPHA Expirada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            try
            {
                _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigureServices(services, hostContext.Configuration);
                })
                .Build();
                _logger = _host.Services.GetService<ILogger<App>>();
                _logger?.LogInformation("=== Aplicación iniciándose ==="); // Log de inicio
                await _host.StartAsync();

                var initializer = _host.Services.GetRequiredService<IDatabaseInitializer>();
                await initializer.InitializeAsync();

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();

                var appStateService = Services.GetRequiredService<IApplicationStateService>();
                await appStateService.RefreshParametros();


                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar la aplicación: {ex.Message}", "Error de Inicio", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }

                

        }

        private void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {

            services.AddDataFlowCoreServices(configuration);
            services.AddDataFlowBLServices();

            services.AddSingleton<IApplicationStateService, ApplicationStateService>();
            services.AddSingleton<IHistProcessManager, HistProcessManager>();
            services.AddSingleton<IConfigTemplateManager, ConfigTemplateManager>();
            services.AddSingleton<IConfigColumnManager, ConfigColumnManager>();
            services.AddSingleton<IParametroManager, ParametroManager>();
            services.AddSingleton<ILookupService, LookupService>();

            services.AddSingleton<IUserPreferencesService, UserPreferencesService>();

            services.AddSingleton<ConfigTemplatesViewModel>();
            services.AddSingleton<ConfigColumnsViewModel>();

            services.AddSingleton<ParametrosViewModel>();
            
            services.AddTransient<MainWindow>(); 
            services.AddTransient<Plantilla>();
            services.AddTransient<Columnas>();
            services.AddTransient<Historial>();
            services.AddTransient<Opciones>();
            services.AddTransient<Proceso>();


            services.AddTransient<AgregarPlantillaDialog>();
            services.AddTransient<AgregarColumnaDialog>();
            services.AddTransient<InputParametroDialog>();
            services.AddTransient<AgregarRangoDialog>();

            services.AddLogging(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
                builder.AddDebug();
            });

            services.AddDataFlowCoreServices(configuration);


        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== [APP EXIT] Iniciando limpieza de servicios ===");

            try
            {
                _host?.Dispose();
                System.Diagnostics.Debug.WriteLine("[APP EXIT] Host disponible correctamente");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[APP EXIT] Error disponiendo host: {ex.Message}");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("[APP EXIT] Terminando aplicación con Environment.Exit(0)");
                System.Environment.Exit(0);
            }
        }
    }
}
