using DataFlow.BL.Constants;
using DataFlow.BL.Services;
using DataFlow.Core.Models;
using DataFlow.UI.Controls;
using DataFlow.UI.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DataFlow.UI.Pages
{
    /// <summary>
    /// Lógica de interacción para Historial.xaml
    /// </summary>
    public partial class Historial : Page
    {
        private readonly IHistProcessManager _histProcessManager;
        private readonly IApplicationStateService _appStateService;
        private readonly ObservableCollection<HistProcess> _items;
        private CancellationTokenSource? _cts;
        private ProcessMonitorControl? _processMonitor;

        public Historial(IHistProcessManager histProcessManager, IApplicationStateService appStateService)
        {
            InitializeComponent();

            _histProcessManager = histProcessManager ?? throw new ArgumentNullException(nameof(histProcessManager));
            _appStateService = appStateService ?? throw new ArgumentNullException(nameof(appStateService));

            _items = new ObservableCollection<HistProcess>();
            HistDataGrid.ItemsSource = _items;

            Loaded += Historial_Loaded;
            Unloaded += Historial_Unloaded;
        }

        private void Historial_Loaded(object? sender, RoutedEventArgs e)
        {
         
            if (_processMonitor == null)
            {
                _processMonitor = new ProcessMonitorControl(_appStateService);
                MonitorHost.Content = _processMonitor;
            }

            _appStateService.PropertyChanged += AppState_PropertyChanged;
            _ = LoadHistAsync();
        }

        private void Historial_Unloaded(object? sender, RoutedEventArgs e)
        {
            _appStateService.PropertyChanged -= AppState_PropertyChanged;
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private void AppState_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IApplicationStateService.SelectedTemplate))
            {
                Application.Current.Dispatcher.InvokeAsync(async () => await LoadHistAsync());
            }
        }

        private async Task LoadHistAsync()
        {
            try
            {
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();

                _items.Clear();
                StatusTextBlock.Text = "Cargando historial...";
                if (_appStateService.SelectedTemplate == null)
                {
                    StatusTextBlock.Text = "Seleccione una plantilla en la página 'Plantillas' para ver su historial.";
                    return;
                }

                int templateId = _appStateService.SelectedTemplate.Id;
                var result = await _histProcessManager.LoadByConfigTemplateIdAsync(templateId, _cts.Token);

                if (result.IsSuccess && result.Value != null)
                {
                    foreach (var hp in result.Value)
                    {
                        _items.Add(hp);
                    }
                    StatusTextBlock.Text = $"{_items.Count} registros cargados.";
                }
                else
                {
                    var error = _histProcessManager.ErrorMessage ?? result.Error ?? "Error al cargar historial.";
                    StatusTextBlock.Text = error;
                    MessageBox.Show(error, "Error al cargar historial", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error inesperado: {ex.Message}";
                MessageBox.Show(StatusTextBlock.Text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadHistAsync();
        }

        private void HistDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistDataGrid.SelectedItem is not HistProcess selected) return;
            LoadNotificationsFromHist(selected);
        }

        private void HistDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HistDataGrid.SelectedItem is not HistProcess selected) return;

            string detail = selected.DataProcess ?? string.Empty;
            MessageBox.Show(string.IsNullOrWhiteSpace(detail) ? "(sin datos)" : detail, $"Detalle proceso #{selected.Id}", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadNotificationsFromHist(HistProcess hist)
        {
            if (_processMonitor == null) return;

            _processMonitor.Clear();

            var data = hist.DataProcess;
            if (string.IsNullOrWhiteSpace(data)) return;

            try
            {
                
                var notifications = JsonSerializer.Deserialize<List<ProcessNotification>>(data);
                if (notifications == null) return;

                foreach (var n in notifications)
                {
                    _processMonitor.AddNotification(n);
                }

                _processMonitor.MarkAsCompleted();
            }
            catch (Exception)
            {
                
                var fallback = new ProcessNotification
                {
                    Level = ProcessNotificationLevel.Info,
                    Message = data,
                    Timestamp = DateTime.Now,
                    Details = null
                };
                _processMonitor.AddNotification(fallback);
            }
        }
    }
}