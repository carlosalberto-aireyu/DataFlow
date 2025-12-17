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

            base.OnStartup(e);

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
            _logger?.LogInformation("=== Aplicación cerrando ===");

            try
            {
                if (_host != null)
                {
                    _logger?.LogInformation("Deteniendo host...");

                    Task.Run(() => _host.StopAsync(TimeSpan.FromSeconds(3))).Wait(TimeSpan.FromSeconds(4));

                    _logger?.LogInformation("Host detenido correctamente o el tiempo de espera se agotó");
                }
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Timeout al detener el host");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error al detener host");
            }
            finally
            {
                _logger?.LogInformation("Liberando recursos del host");
                _host?.Dispose();
                _logger?.LogInformation("Host deshechado.");
            }

            base.OnExit(e);
        }
    }
}
