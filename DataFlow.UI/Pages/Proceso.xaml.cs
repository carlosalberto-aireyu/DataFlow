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
        private readonly IUserPreferencesService _userPreferencesService;
        private readonly IProcessQueueService _queueService;

        private ProcessMonitorControl _processMonitor;
        private bool _isProcessing = false;

        public Proceso(IExcelProcessingService excelProcessingService,
                       IApplicationStateService appStateService,
                       IQueryDispatcher queryDispatcher,
                       IConfigTemplateManager templateManager,
                       IHistProcessManager histProcessManager,
                       IUserPreferencesService userPreferencesService,
                       IProcessQueueService queueService)
        {
            _excelProcessingService = excelProcessingService ?? throw new ArgumentNullException(nameof(excelProcessingService));
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));
            _templateManager = templateManager ?? throw new ArgumentNullException(nameof(templateManager));
            _histProcessManager = histProcessManager ?? throw new ArgumentNullException(nameof(histProcessManager));
            _userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));
            _queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));

            InitializeComponent();

            _processMonitor = new ProcessMonitorControl(_appStateService);
            MonitorGrid.Children.Add(_processMonitor);
            _excelProcessingService.NotificationReceived += OnNotificationReceived;

            _queueService.QueueChanged += QueueService_QueueChanged;
            _queueService.ItemProcessed += QueueService_ItemProcessed;
            _queueService.ItemFailed += QueueService_ItemFailed;

            Loaded += Proceso_Loaded;
            Unloaded += Proceso_Unloaded;
        }

        private void Proceso_Loaded(object sender, RoutedEventArgs e)
        {
            _appStateService.PropertyChanged += ApplicationStateService_PropertyChanged;
            Load_Notifications();
            InitializeDisplay();

            bool autoOpen = _userPreferencesService.GetAutoOpenExcelFile();
            AutoOpenFileCheckBox.IsChecked = autoOpen;

            UpdateQueueDisplay();
        }

        private void Proceso_Unloaded(object sender, RoutedEventArgs e)
        {
            _excelProcessingService.NotificationReceived -= OnNotificationReceived;
            _appStateService.PropertyChanged -= ApplicationStateService_PropertyChanged;
            _queueService.QueueChanged -= QueueService_QueueChanged;
            _queueService.ItemProcessed -= QueueService_ItemProcessed;
            _queueService.ItemFailed -= QueueService_ItemFailed;
        }

        private void OnNotificationReceived(object? sender, ProcessNotification notification)
        {
            _processMonitor.AddNotification(notification);
            _appStateService.NotificationsProcess.Add(notification);
        }

        private void Load_Notifications()
        {
            if (_appStateService.NotificationsProcess.Count > 0)
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

            UpdateStartButtonState();
        }

        private void UpdateStartButtonState()
        {
            bool hasQueuedFiles = _queueService.GetQueueCount() > 0;
            bool hasSelectedFile = !string.IsNullOrWhiteSpace(_appStateService.ExcelFilePath) &&
                                   File.Exists(_appStateService.ExcelFilePath);
            bool hasTemplate = _appStateService.SelectedTemplate != null;

            StartProcessButton.IsEnabled = (hasQueuedFiles || hasSelectedFile) && hasTemplate && !_isProcessing;
        }

        private void UpdateQueueDisplay()
        {
            var queueCount = _queueService.GetQueueCount();
            QueueCountTextBlock.Text = queueCount > 0
                ? $"Archivos en cola: {queueCount}"
                : "Cola vacía";

            QueueListBox.ItemsSource = _queueService.GetQueueItems();
        }

        private void QueueService_QueueChanged(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateQueueDisplay();
                UpdateStartButtonState();
            });
        }

        private void QueueService_ItemProcessed(object? sender, ProcessQueueItemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateQueueDisplay();
                if (_queueService.GetQueueCount() > 0)
                {
                    ProcessNextQueueItem();
                }
                else
                {
                    FinishProcessing();
                }
            });
        }

        private void QueueService_ItemFailed(object? sender, ProcessQueueItemEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateQueueDisplay();
                MessageBox.Show(
                    $"Error procesando {e.Item.FileName}:\n{e.Item.ErrorMessage}",
                    "Error en Cola",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                if (_queueService.GetQueueCount() > 0)
                {
                    ProcessNextQueueItem();
                }
                else
                {
                    FinishProcessing();
                }
            });
        }

        private void SelectExcelFileButton_Click(object sender, RoutedEventArgs e)
        {
            string initialDirectory = _userPreferencesService.GetLastExcelFolder();
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Archivos de Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Todos los archivos (*.*)|*.*",
                Title = "Seleccionar archivo(s) de Excel",
                InitialDirectory = initialDirectory,
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true && openFileDialog.FileNames.Length > 0)
            {
                // Si selecciona múltiples archivos, añadirlos todos a la cola
                if (openFileDialog.FileNames.Length > 1)
                {
                    _queueService.EnqueueFiles(openFileDialog.FileNames);
                    ExcelFilePathTextBox.Text = $"{openFileDialog.FileNames.Length} archivos añadidos a la cola";
                    ProcessStatusTextBlock.Text = "Archivos añadidos a la cola. Haz clic en 'Iniciar Proceso' para comenzar.";
                    ProcessStatusTextBlock.Foreground = Brushes.Blue;
                }
                else
                {
                    // Si selecciona un solo archivo, tratarlo como archivo actual
                    _appStateService.ExcelFilePath = openFileDialog.FileName;
                    ExcelFilePathTextBox.Text = openFileDialog.FileName;
                    ProcessStatusTextBlock.Text = "Archivo seleccionado. Haz clic en 'Iniciar Proceso' para comenzar.";
                    ProcessStatusTextBlock.Foreground = Brushes.Black;
                }

                string selectedFolder = System.IO.Path.GetDirectoryName(openFileDialog.FileNames[0]) ?? initialDirectory;
                _userPreferencesService.SaveLastExcelFolder(selectedFolder);

                System.Diagnostics.Debug.WriteLine($"[SelectExcelFileButton_Click] Nueva carpeta guardada: {selectedFolder}");
                UpdateStartButtonState();
            }
        }

        private async void StartProcessButton_Click(object sender, RoutedEventArgs e)
        {
            if (AlphaVersionService.IsExpired)
            {
                ProcessStatusTextBlock.Text = "Error: La versión ALPHA ha expirado.";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(
                    AlphaVersionService.GetExpiryMessage(),
                    "Versión ALPHA Expirada",
                    MessageBoxButton.OK,
                    MessageBoxImage.Stop);
                return;
            }

            // Validación básica de plantilla
            if (_appStateService.SelectedTemplate == null)
            {
                ProcessStatusTextBlock.Text = "Error: No se ha seleccionado una plantilla válida.";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(
                    "Debe seleccionar una plantilla válida en la página 'Plantillas'.",
                    "Error de Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            // Si hay archivos en la cola, procesarlos
            if (_queueService.GetQueueCount() > 0)
            {
                _isProcessing = true;
                StartProcessButton.IsEnabled = false;
                ProcessStatusTextBlock.Text = "Procesando cola de archivos...";
                ProcessStatusTextBlock.Foreground = Brushes.Blue;

                ProcessNextQueueItem();
            }
            // Si hay un archivo seleccionado, procesarlo
            else if (!string.IsNullOrWhiteSpace(_appStateService.ExcelFilePath) &&
                     File.Exists(_appStateService.ExcelFilePath))
            {
                _isProcessing = true;
                StartProcessButton.IsEnabled = false;

                _processMonitor.Clear();
                _appStateService.NotificationsProcess.Clear();

                await ProcessSingleFileAsync();
            }
            else
            {
                ProcessStatusTextBlock.Text = "Error: No hay archivos para procesar.";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(
                    "Seleccione al menos un archivo para procesar.",
                    "Error de Validación",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task ProcessSingleFileAsync()
        {
            string? inputFilePath = _appStateService.ExcelFilePath;
            ConfigTemplate? templateConfig = _appStateService.SelectedTemplate?.ToModel();

            ProcessStatusTextBlock.Text = "Iniciando...";
            ProcessStatusTextBlock.Foreground = Brushes.Black;
            OutputPathTextBlock.Text = "";

            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(inputFilePath) || !File.Exists(inputFilePath))
                {
                    ProcessStatusTextBlock.Text = "Error: Archivo no encontrado.";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(
                        "El archivo seleccionado no existe.",
                        "Error de Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                if (templateConfig == null || templateConfig.Id == 0)
                {
                    ProcessStatusTextBlock.Text = "Error: Plantilla no válida.";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(
                        "Debe seleccionar una plantilla válida.",
                        "Error de Validación",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                string? workDirectory = _appStateService.GetParametroValue(ParametroKey.WorkDirectory);
                if (string.IsNullOrWhiteSpace(workDirectory) || !Directory.Exists(workDirectory))
                {
                    ProcessStatusTextBlock.Text = "Error: Directorio de trabajo no configurado.";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(
                        $"El directorio de trabajo no es válido: {workDirectory}",
                        "Error de Configuración",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Cargar plantilla completa
                Result<ConfigTemplate> fullTemplateResult = await _templateManager.LoadByIdAsync(templateConfig.Id);

                if (fullTemplateResult.IsFailure || fullTemplateResult.Value == null)
                {
                    ProcessStatusTextBlock.Text = $"Error: No se pudo cargar la plantilla.";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(
                        $"Error cargando plantilla: {fullTemplateResult.Error}",
                        "Error de Carga de Plantilla",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                templateConfig = fullTemplateResult.Value;

                if (templateConfig.ConfigColumns == null || !templateConfig.ConfigColumns.Any())
                {
                    ProcessStatusTextBlock.Text = "Error: La plantilla no tiene columnas configuradas.";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(
                        "La plantilla debe tener columnas configuradas.",
                        "Error de Configuración",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                // Preparar rutas
                string inputFileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(inputFilePath);
                string outputFileName = $"{inputFileNameWithoutExtension}_normalizado_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string outputFilePath = System.IO.Path.Combine(workDirectory, outputFileName);

                ProcessStatusTextBlock.Text = "Procesando archivo Excel...";
                ProcessStatusTextBlock.Foreground = Brushes.Blue;

                // Procesar archivo
                var result = await _excelProcessingService.ProcessExcelFileAsync(
                    inputFilePath,
                    outputFilePath,
                    templateConfig);

                _processMonitor.MarkAsCompleted();

                if (result.IsSuccess)
                {
                    await SaveHistProcessAsync(templateConfig.Id, inputFilePath, outputFilePath, "success");
                    ProcessStatusTextBlock.Text = "✓ Proceso terminado exitosamente.";
                    ProcessStatusTextBlock.Foreground = Brushes.Green;
                    OutputPathTextBlock.Text = $"Archivo de salida: {result.Value}";

                    MessageBox.Show(
                        $"Archivo Excel procesado exitosamente y guardado en:\n{result.Value}",
                        "Éxito",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Abrir automáticamente si está configurado
                    if (AutoOpenFileCheckBox.IsChecked == true)
                    {
                        try
                        {
                            _ = System.Diagnostics.Process.Start(
                                new System.Diagnostics.ProcessStartInfo(result.Value!) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"No se pudo abrir el archivo automáticamente: {ex.Message}",
                                "Error al Abrir Archivo",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    await SaveHistProcessAsync(templateConfig.Id, inputFilePath, outputFilePath, "Error");
                    ProcessStatusTextBlock.Text = $"Error durante el procesamiento: {result.Error}";
                    ProcessStatusTextBlock.Foreground = Brushes.Red;
                    MessageBox.Show(
                        $"Error al procesar el archivo Excel:\n{result.Error}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                ProcessStatusTextBlock.Text = $"Error inesperado: {ex.Message}";
                ProcessStatusTextBlock.Foreground = Brushes.Red;
                MessageBox.Show(ProcessStatusTextBlock.Text, "Error Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                FinishProcessing();
            }
        }

        private void ProcessNextQueueItem()
        {
            var item = _queueService.DequeueFile();
            if (item == null)
            {
                FinishProcessing();
                return;
            }

            item.Status = ProcessQueueItemStatus.Processing;
            UpdateQueueDisplay();

            ProcessStatusTextBlock.Text = $"Procesando: {item.FileName}";
            ProcessStatusTextBlock.Foreground = Brushes.Blue;
            _ = ProcessQueueItemAsync(item);
        }

        private async Task ProcessQueueItemAsync(ProcessQueueItem queueItem)
        {
            try
            {
                // Validaciones previas
                if (!File.Exists(queueItem.FilePath))
                {
                    _queueService.NotifyItemFailed(queueItem, "El archivo no existe o fue eliminado.");
                    return;
                }

                // Obtener configuración de plantilla
                ConfigTemplate? templateConfig = _appStateService.SelectedTemplate?.ToModel();
                if (templateConfig == null || templateConfig.Id == 0)
                {
                    _queueService.NotifyItemFailed(queueItem, "No se ha seleccionado una plantilla válida.");
                    return;
                }

                // Validar directorio de trabajo
                string? workDirectory = _appStateService.GetParametroValue(ParametroKey.WorkDirectory);
                if (string.IsNullOrWhiteSpace(workDirectory) || !Directory.Exists(workDirectory))
                {
                    _queueService.NotifyItemFailed(queueItem, $"El directorio de trabajo no es válido: {workDirectory}");
                    return;
                }

                // Cargar plantilla completa
                Result<ConfigTemplate> fullTemplateResult;
                try
                {
                    fullTemplateResult = await _templateManager.LoadByIdAsync(templateConfig.Id);
                    if (fullTemplateResult.IsFailure || fullTemplateResult.Value == null)
                    {
                        _queueService.NotifyItemFailed(queueItem, $"Error cargando plantilla: {fullTemplateResult.Error}");
                        return;
                    }
                    templateConfig = fullTemplateResult.Value;

                    if (templateConfig.ConfigColumns == null || !templateConfig.ConfigColumns.Any())
                    {
                        _queueService.NotifyItemFailed(queueItem, "La plantilla no tiene columnas configuradas.");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _queueService.NotifyItemFailed(queueItem, $"Excepción al cargar plantilla: {ex.Message}");
                    return;
                }

                // Preparar rutas de salida
                string inputFileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(queueItem.FilePath);
                string outputFileName = $"{inputFileNameWithoutExtension}_normalizado_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string outputFilePath = System.IO.Path.Combine(workDirectory, outputFileName);

                // Limpiar notificaciones anteriores
                _appStateService.NotificationsProcess.Clear();
                _processMonitor.Clear();

                // Procesar archivo
                try
                {
                    var result = await _excelProcessingService.ProcessExcelFileAsync(
                        queueItem.FilePath,
                        outputFilePath,
                        templateConfig);

                    _processMonitor.MarkAsCompleted();

                    if (result.IsSuccess)
                    {
                        await SaveHistProcessAsync(templateConfig.Id, queueItem.FilePath, outputFilePath, "success");
                        queueItem.ErrorMessage = null;
                        _queueService.NotifyItemProcessed(queueItem);

                        // Mostrar resultado
                        ProcessStatusTextBlock.Text = $"✓ {queueItem.FileName} procesado correctamente.";
                        ProcessStatusTextBlock.Foreground = Brushes.Green;
                        OutputPathTextBlock.Text = $"Archivo de salida: {result.Value}";

                        // Abrir automáticamente si está configurado
                        if (AutoOpenFileCheckBox.IsChecked == true)
                        {
                            try
                            {
                                _ = System.Diagnostics.Process.Start(
                                    new System.Diagnostics.ProcessStartInfo(result.Value!) { UseShellExecute = true });
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"No se pudo abrir automáticamente: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        await SaveHistProcessAsync(templateConfig.Id, queueItem.FilePath, outputFilePath, "Error");
                        _queueService.NotifyItemFailed(queueItem, $"Ha ocurrido un error: {result.Error}" );
                                      
                    }
                }
                catch (Exception ex)
                {
                    await SaveHistProcessAsync(templateConfig.Id, queueItem.FilePath, outputFilePath, "Error");
                    _queueService.NotifyItemFailed(queueItem, ex.Message);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ProcessQueueItemAsync: {ex}");
                _queueService.NotifyItemFailed(queueItem, ex.Message);
            }
        }

        private void FinishProcessing()
        {
            _isProcessing = false;
            UpdateStartButtonState();
            ProcessStatusTextBlock.Text = "Proceso completado.";
            ProcessStatusTextBlock.Foreground = Brushes.Green;
        }

        private async Task SaveHistProcessAsync(int configtemplateid, string inputFile, string outputFile, string status)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = false
                };
                var dataNotifications = JsonSerializer.Serialize(_appStateService.NotificationsProcess);
                var cmd = new CreateHistProcessCommand(configtemplateid, dataNotifications, inputFile, outputFile, status);
                await _histProcessManager.CreateAsync(cmd);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al guardar histórico: {ex.Message}");
            }
        }

        private void AutoOpenFileCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            bool isChecked = AutoOpenFileCheckBox.IsChecked ?? false;
            _userPreferencesService.SaveAutoOpenExcelFile(isChecked);
        }

        private void RemoveFromQueueButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProcessQueueItem item)
            {
                var items = _queueService.GetQueueItems().ToList();
                var index = items.IndexOf(item);
                if (index >= 0)
                {
                    _queueService.RemoveAt(index);
                }
            }
        }

        private void ClearQueueButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "¿Desea vaciar toda la cola?",
                "Confirmar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _queueService.Clear();
            }
        }
    }
}