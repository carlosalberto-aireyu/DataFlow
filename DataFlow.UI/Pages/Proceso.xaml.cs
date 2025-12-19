using DataFlow.BL.Contracts;
using DataFlow.BL.Services;
using DataFlow.Core.Common;
using DataFlow.Core.Constants;
using DataFlow.Core.Features;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Features.Queries;
using DataFlow.Core.Models;
using DataFlow.UI.Controls;
using DataFlow.UI.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataFlow.UI.Pages
{
    /// <summary>
    /// Lógica de interacción para Proceso.xaml
    /// </summary>
    public partial class Proceso : Page
    {
        private readonly IExcelProcessingService _excelProcessingService;
        private readonly IApplicationStateService _appStateService;
        private readonly IConfigTemplateManager _templateManager;
        private readonly IHistProcessManager _histProcessManager;

        private ProcessMonitorControl _processMonitor;


        public Proceso(IExcelProcessingService excelProcessingService,
                       IApplicationStateService appStateService,
                       IQueryDispatcher queryDispatcher,
                       IConfigTemplateManager templateManager,
                       IHistProcessManager histProcessManager
                       )
        {
            InitializeComponent();
            _excelProcessingService = excelProcessingService ?? throw new ArgumentNullException(nameof(excelProcessingService));
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _histProcessManager = histProcessManager ?? throw new ArgumentNullException(nameof(histProcessManager));

            _processMonitor = new ProcessMonitorControl(_appStateService);
            MonitorGrid.Children.Add(_processMonitor);
            _excelProcessingService.NotificationReceived += OnNotificationReceived;


            Loaded += Proceso_Loaded;
            Unloaded += Proceso_Unloaded; 
        }

        private void Proceso_Loaded(object sender, RoutedEventArgs e)
        {
            
            _appStateService.PropertyChanged += ApplicationStateService_PropertyChanged;
            Load_Notifications();
            InitializeDisplay();

        }

        private void Proceso_Unloaded(object sender, RoutedEventArgs e)
        {
            _excelProcessingService.NotificationReceived -= OnNotificationReceived;
            _appStateService.PropertyChanged -= ApplicationStateService_PropertyChanged;
        }
        private void OnNotificationReceived(object? sender, ProcessNotification notification)
        {
            _processMonitor.AddNotification(notification);
            _appStateService.NotificationsProcess.Add(notification);
        }
        private void Load_Notifications()
        {
            if(_appStateService.NotificationsProcess.Count > 0)
            {
                foreach (ProcessNotification item in _appStateService.NotificationsProcess)
                {
                    _processMonitor.AddNotification(item);
                }
                _processMonitor.MarkAsCompleted();
            }
        }


        private void ApplicationStateService_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
       
            if (e.PropertyName == nameof(IApplicationStateService.ExcelFilePath) ||
                e.PropertyName == nameof(IApplicationStateService.SelectedTemplate))
            {
       
                Application.Current.Dispatcher.Invoke(InitializeDisplay);
            }
        }

        private void InitializeDisplay()
        {


            ExcelFilePathTextBox.Text = _appStateService.ExcelFilePath ?? "No se ha seleccionado archivo Excel.";
            ProcessStatusTextBlock.Text = "Listo para iniciar el proceso.";
            ProcessStatusTextBlock.Foreground = Brushes.Black;
            OutputPathTextBlock.Text = "";

            if (_appStateService.SelectedTemplate != null)
            {
                TemplateDescriptionTextBlock.Text = _appStateService.SelectedTemplate.Description;
            }
            else
            {
                TemplateDescriptionTextBlock.Text = "No se ha seleccionado ninguna plantilla.";
                TemplateDescriptionTextBlock.Foreground = Brushes.OrangeRed;
            }

            
            StartProcessButton.IsEnabled = !string.IsNullOrWhiteSpace(_appStateService.ExcelFilePath)
                                        && File.Exists(_appStateService.ExcelFilePath)
                                        && _appStateService.SelectedTemplate != null;
        }

        private async void StartProcessButton_Click(object sender, RoutedEventArgs e)
        {
            _processMonitor.Clear();
            _appStateService.NotificationsProcess.Clear();

            ProcessStatusTextBlock.Text = "Iniciando...";
            ProcessStatusTextBlock.Foreground = Brushes.Black;
            OutputPathTextBlock.Text = "";
            StartProcessButton.IsEnabled = false;

            string? inputFilePath = _appStateService.ExcelFilePath;
            ConfigTemplate? templateConfig = _appStateService.SelectedTemplate?.ToModel();

            if (string.IsNullOrWhiteSpace(inputFilePath) || !File.Exists(inputFilePath))
            {
                ProcessStatusTextBlock.Text = "Error: No se ha seleccionado una ruta de archivo Excel de origen válida, o el archivo no existe.";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(ProcessStatusTextBlock.Text, "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Error);
                StartProcessButton.IsEnabled = true;
                return;
            }

            if (templateConfig == null || templateConfig.Id == 0)
            {
                ProcessStatusTextBlock.Text = "Error: No se ha seleccionado una plantilla válida en la página 'Plantillas'.";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(ProcessStatusTextBlock.Text, "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Error);
                StartProcessButton.IsEnabled = true;
                return;
            }
            string? workDirectory = _appStateService.GetParametroValue(ParametroKey.WorkDirectory);
            if (string.IsNullOrWhiteSpace(workDirectory))
            {
                ProcessStatusTextBlock.Text = "Error: El 'Directorio de Trabajo' no está configurado en los Parámetros de la aplicación.";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(ProcessStatusTextBlock.Text, "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
                StartProcessButton.IsEnabled = true;
                return;
            }
            if (!Directory.Exists(workDirectory))
            {
                ProcessStatusTextBlock.Text = $"Error: El Directorio de Trabajo '{workDirectory}' no existe. Por favor, asegúrese de que el directorio existe o configure en Opciones de la aplicación.";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(ProcessStatusTextBlock.Text, "Error de Directorio", MessageBoxButton.OK, MessageBoxImage.Error);
                StartProcessButton.IsEnabled = true;
                return;
            }


            Result<ConfigTemplate> fullTemplateResult;
            try
            {
                fullTemplateResult = await _templateManager.LoadByIdAsync(templateConfig.Id);

                if (fullTemplateResult.IsFailure || fullTemplateResult.Value == null)
                {
                    ProcessStatusTextBlock.Text = $"Error: No se pudo cargar los detalles completos de la plantilla ID {templateConfig.Id}. {fullTemplateResult.Error}";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(ProcessStatusTextBlock.Text, "Error de Carga de Plantilla", MessageBoxButton.OK, MessageBoxImage.Error);
                    StartProcessButton.IsEnabled = true;
                    return;
                }
                templateConfig = fullTemplateResult.Value; 

                if (templateConfig.ConfigColumns == null || !templateConfig.ConfigColumns.Any())
                {
                    ProcessStatusTextBlock.Text = $"Error: La plantilla '{templateConfig.Description}' no tiene columnas configuradas.";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(ProcessStatusTextBlock.Text, "Error de Configuración", MessageBoxButton.OK, MessageBoxImage.Error);
                    StartProcessButton.IsEnabled = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                ProcessStatusTextBlock.Text = $"Excepción al cargar la plantilla completa: {ex.Message}";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(ProcessStatusTextBlock.Text, "Error Inesperado", MessageBoxButton.OK, MessageBoxImage.Error);
                StartProcessButton.IsEnabled = true;
                return;
            }

            string inputFileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(inputFilePath);
            string outputDirectory = workDirectory;

            string outputFileName = $"{inputFileNameWithoutExtension}_normalizado_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string outputFilePath = System.IO.Path.Combine(outputDirectory, outputFileName);

            ProcessStatusTextBlock.Text = "Procesando archivo Excel...";
            ProcessStatusTextBlock.Foreground = Brushes.Blue;

            try
            {
                var result = await _excelProcessingService.ProcessExcelFileAsync(
                    inputFilePath,
                    outputFilePath,
                    templateConfig);


                _processMonitor.MarkAsCompleted();

                if (result.IsSuccess)
                {
                    SaveHistProcess(templateConfig.Id, inputFilePath, outputFilePath, "success");
                    ProcessStatusTextBlock.Text = "Proceso terminado exitosamente.";
                    ProcessStatusTextBlock.Foreground = Brushes.Green;
                    OutputPathTextBlock.Text = $"Archivo de salida: {result.Value}";


                    MessageBox.Show($"Archivo Excel procesado exitosamente y guardado en:\n{result.Value}", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    _ = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(result.Value!) { UseShellExecute = true });
                }
                else
                {
                    SaveHistProcess(templateConfig.Id, inputFilePath, outputFilePath, "Error");
                    ProcessStatusTextBlock.Text = $"Error durante el procesamiento: {result.Error}";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show($"Error al procesar el archivo Excel:\n{result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                SaveHistProcess(templateConfig.Id, inputFilePath, outputFilePath, "Error");

                ProcessStatusTextBlock.Text = $"Error inesperado durante el procesamiento: {ex.Message}";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(ProcessStatusTextBlock.Text, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                StartProcessButton.IsEnabled = true;
            }
        }
        private async void SaveHistProcess(int configtemplateid, string inputFile, string outputFile, string status)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                var dataNotifications = JsonSerializer.Serialize(_appStateService.NotificationsProcess);
                var cmd = new CreateHistProcessCommand(configtemplateid, dataNotifications, inputFile, outputFile, status);
                var result = await _histProcessManager.CreateAsync(cmd);


            }
            catch (Exception ex)
            {
                   MessageBox.Show($"Error al guardar el historico :{ex.Message} ", "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SelectExcelFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos de Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar archivo de Excel",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (openFileDialog.ShowDialog() == true)
            {
                _appStateService.ExcelFilePath = openFileDialog.FileName;
                ExcelFilePathTextBox.Text = openFileDialog.FileName;
            }
        }
    }
}
