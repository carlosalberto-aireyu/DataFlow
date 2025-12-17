using DataFlow.Core.Constants;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Models;
using DataFlow.UI.Commands;
using DataFlow.UI.Pages;
using DataFlow.UI.Pages.Dialogs;
using DataFlow.UI.Services;
using DataFlow.UI.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace DataFlow.UI.ViewModels
{
    public class ParametrosViewModel : ViewModelBase, IDisposable
    {
        private readonly IParametroManager _manager;
        private readonly ILogger<ParametrosViewModel> _logger;
        private readonly IServiceProvider _serviceProvider;

        public ObservableCollection<ParametroItemViewModel> Items => _manager.Items;
        private CancellationTokenSource? _cancellationTokenSource;

        private ParametroItemViewModel? _selectedItem;
        public ParametroItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (SetProperty(ref _selectedItem, value))
                {
                    // Asegurarse de que los comandos actualicen su estado CanExecute
                    ((AsyncRelayCommand)EditParametroCommand).RaiseCanExecuteChanged();
                    ((AsyncRelayCommand)DeleteParametroCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)SelectDirectoryCommand).RaiseCanExecuteChanged();

                    // *** Mensaje de depuración para verificar la clave del parámetro seleccionado ***
                    _logger.LogDebug($"SelectedItem.ParametroKey cambiado a: {value?.ParametroKey ?? "NULO"}");
                }
            }
        }

        public bool IsBusy => _manager.IsBusy;
        public string? ErrorMessage => _manager.ErrorMessage;
        public int ItemCount => Items.Count;

        public ICommand RefreshCommand { get; }
        public ICommand AddParametroCommand { get; }
        public ICommand EditParametroCommand { get; }
        public ICommand DeleteParametroCommand { get; }
        public ICommand SelectDirectoryCommand { get; }
        public ICommand ExportarInformacionCommand { get; }

        public ParametrosViewModel(
            IParametroManager manager,
            ILogger<ParametrosViewModel> logger,
            IServiceProvider serviceProvider)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _cancellationTokenSource = new CancellationTokenSource();
            _manager.PropertyChanged += OnParametroManagerPropertyChanged;
            Items.CollectionChanged += (s, e) =>
            {
                Raise(nameof(ItemCount));
            };


            RefreshCommand = new AsyncRelayCommand(LoadParametrosAsync);
            AddParametroCommand = new AsyncRelayCommand(AddParametroAsync);
            
            EditParametroCommand = new AsyncRelayCommand(EditParametroAsync, CanEditOrDeleteParametro);
            DeleteParametroCommand = new AsyncRelayCommand(DeleteParametroAsync, CanEditOrDeleteParametro);
            SelectDirectoryCommand = new RelayCommand(ExecuteSelectDirectory, CanExecuteSelectDirectory);
            ExportarInformacionCommand = new AsyncRelayCommand(ExportarInformacion,CanExecuteSelectDirectory);

        }

        private void OnParametroManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IParametroManager.IsBusy))
            {
                Raise(nameof(IsBusy));
            }
            else if (e.PropertyName == nameof(IParametroManager.ErrorMessage))
            {
                Raise(nameof(ErrorMessage));
            }
        }

        private bool CanExecuteSelectDirectory(object? parameter)
        {
            return SelectedItem != null &&
                (SelectedItem.ParametroKey == ParametroKey.WorkDirectory.ToString("G") ||
                 SelectedItem.ParametroKey == ParametroKey.DataToJsonExporter.ToString("G"));
        }


        private bool CanEditOrDeleteParametro(object? parameter) => SelectedItem != null;


        private async void ExecuteSelectDirectory(object? parameter)
        {
            ParametroItemViewModel currentParam = SelectedItem!;
            var dialog = new OpenFolderDialog
            {
                Title = "Seleccionar Directorio de Trabajo",
                InitialDirectory = currentParam.ParametroValue
            };

            if (dialog.ShowDialog() == true)
            {

                currentParam.ParametroValue = dialog.FolderName;

                var result = await _manager.UpdateAsync(currentParam, _cancellationTokenSource?.Token ?? CancellationToken.None);
                if (!result.IsSuccess)
                {
                    _logger.LogError($"Error al actualizar el parámetro '{currentParam.Name}': {result.Error}");
                    MessageBox.Show(
                        $"Error al guardar la nueva ruta: {result.Error}",
                        "Error de guardado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                else
                {
                    _logger.LogInformation($"Parámetro '{currentParam.Name}' actualizado con la nueva ruta: {currentParam.ParametroValue}");
                }
            }
        }


        private async Task LoadParametrosAsync(CancellationToken cancellationToken = default)
        {
            var result = await _manager.LoadAllAsync(cancellationToken);

            if (result.IsSuccess)
            {
                // Si la selección es nula y hay elementos en la lista, selecciona el primero por defecto.
                if (SelectedItem == null && Items.Any())
                {
                    //SelectedItem = Items.FirstOrDefault();
                }
                _logger.LogInformation("Parámetros cargados exitosamente.");
            }
            else
            {
                MessageBox.Show($"Error al cargar los parámetros: {result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task AddParametroAsync(CancellationToken cancellationToken = default)
        {
            var dialog = _serviceProvider.GetRequiredService<InputParametroDialog>();
            dialog.Owner = Application.Current.MainWindow;
            dialog.ClearInputs();
            dialog.DisableKeyEdit = false;

            if (dialog.ShowDialog() == true)
            {
                var result = await _manager.CreateAsync(
                    dialog.ParametroKey,
                    dialog.ParametroKey,
                    dialog.ParametroValue,
                    dialog.Description,
                    cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    MessageBox.Show("Parámetro añadido exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    SelectedItem = result.Value;
                }
                else
                {
                    MessageBox.Show($"Error al añadir parámetro: {_manager.ErrorMessage ?? result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private async Task EditParametroAsync(CancellationToken cancellationToken = default)
        {
            // La llamada a CanEditOrDeleteParametro() dentro del método ya la controla el CanExecute del comando
            if (SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un parámetro para editar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = _serviceProvider.GetRequiredService<InputParametroDialog>();

            ParametroItemViewModel currentSelectedItem = SelectedItem!;

            dialog.ParametroKey = currentSelectedItem.ParametroKey;
            dialog.ParametroValue = currentSelectedItem.ParametroValue;
            dialog.Description = currentSelectedItem.Description ?? string.Empty;
            dialog.DisableKeyEdit = true;
            dialog.Owner = Application.Current.MainWindow;

            if (dialog.ShowDialog() == true)
            {
                currentSelectedItem.ParametroValue = dialog.ParametroValue;
                currentSelectedItem.Description = dialog.Description;

                var result = await _manager.UpdateAsync(currentSelectedItem, cancellationToken);

                if (result.IsSuccess)
                {
                    MessageBox.Show("Parámetro editado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Error al editar parámetro: {_manager.ErrorMessage ?? result.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task DeleteParametroAsync(CancellationToken cancellationToken = default)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar un parámetro para eliminar.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ParametroItemViewModel currentSelectedItem = SelectedItem!;

            var result = MessageBox.Show(
                $"¿Está seguro de que desea eliminar el parámetro '{currentSelectedItem.ParametroKey}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                var deleteResult = await _manager.DeleteAsync(currentSelectedItem, cancellationToken);
                if (deleteResult.IsSuccess)
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        Items.Remove(currentSelectedItem);
                        // Después de eliminar, selecciona el primer elemento restante si existe
                        SelectedItem = Items.FirstOrDefault();
                    });
                    MessageBox.Show("Parámetro eliminado exitosamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Error al eliminar parámetro: {_manager.ErrorMessage ?? deleteResult.Error}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async Task ExportarInformacion(CancellationToken cancellationToken = default)
        {
            if(SelectedItem == null)
            {
                MessageBox.Show("Debe seleccionar alguna opción para continuar (Exportar Información)");
            }

            ParametroItemViewModel currentOption = SelectedItem!;
            var result = MessageBox.Show(
                $"¿Está seguro de que desea exportar su información a: '{currentOption.ParametroValue}'?",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if(result == MessageBoxResult.Yes)
            {             
                var resultExp = await _manager.ExportarInformacion(currentOption, cancellationToken);
                if(resultExp.IsSuccess && resultExp.Value)
                {
                    MessageBox.Show("La exportacion se realizo de manera exitosa", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            if (_manager != null)
            {
                _manager.PropertyChanged -= OnParametroManagerPropertyChanged;
            }
        }
    }
}