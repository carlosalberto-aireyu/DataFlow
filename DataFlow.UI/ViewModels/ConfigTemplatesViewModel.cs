using DataFlow.Core.Features.Commands;
using DataFlow.UI.Commands;
using DataFlow.UI.Services;
using DataFlow.UI.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;

namespace DataFlow.UI.ViewModels
{
    public class ConfigTemplatesViewModel : ViewModelBase, IDisposable
    {
        private readonly IConfigTemplateManager _manager;
        private readonly ILogger<ConfigTemplatesViewModel> _logger;

        private ConfigTemplateItemViewModel? _selectedItem;
        private string? _searchText;
        private ObservableCollection<ConfigTemplateItemViewModel> _filteredItems;
        private CancellationTokenSource? _cancellationTokenSource;
        

        #region Propiedades Bindables
        public ObservableCollection<ConfigTemplateItemViewModel> Items => _manager.Items;
        public ObservableCollection<ConfigTemplateItemViewModel> FilteredItems
        {
            get => _filteredItems;
            private set
            {
                if (_filteredItems != value)
                {
                    _filteredItems = value;
                    Raise(nameof(FilteredItems));
                }
            }
        }
        public ConfigTemplateItemViewModel? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (_selectedItem != value)
                {
                    _selectedItem = value;
                    Raise(nameof(SelectedItem));
                    Raise(nameof(IsItemSelected));
                }
            }
        }
        public bool IsItemSelected => SelectedItem != null;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    Raise(nameof(SearchText));
                    ApplyFilter();
                }
            }
        }
        public bool IsBusy => _manager.IsBusy;
        public string? ErrorMessage => _manager.ErrorMessage;
        public int ItemCount => Items.Count;
        public int FilteredItemCount => FilteredItems.Count;

        #endregion

        #region Comandos
        public ICommand RefreshCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand LoadByIdCommand { get; }
        public ICommand ClearFilterCommand { get; }

        #endregion
        public ConfigTemplatesViewModel(
            IConfigTemplateManager manager,
            ILogger<ConfigTemplatesViewModel> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _filteredItems = new ObservableCollection<ConfigTemplateItemViewModel>();

            _cancellationTokenSource = new CancellationTokenSource();
            _manager.PropertyChanged += OnManagerPropertyChanged;
            Items.CollectionChanged += (s, e) =>
            {
                ApplyFilter();
                Raise(nameof(ItemCount));
            };

            // Inicializar comandos
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            CreateCommand = new AsyncRelayCommand<string>(CreateAsync);
            EditCommand = new AsyncRelayCommand(EditSelectedAsync);
            DeleteCommand = new AsyncRelayCommand(DeleteSelectedAsync);
            LoadByIdCommand = new AsyncRelayCommand<int>(LoadByIdAsync);
            ClearFilterCommand = new RelayCommand(ClearFilter);

            _logger.LogInformation("ConfigTemplatesViewModel inicializado");
        }

        #region Métodos Privados
        private void OnManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IConfigTemplateManager.IsBusy))
                Raise(nameof(IsBusy));
            else if (e.PropertyName == nameof(IConfigTemplateManager.ErrorMessage))
                Raise(nameof(ErrorMessage));
        }
        private void ApplyFilter()
        {
            try
            {
                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? new ObservableCollection<ConfigTemplateItemViewModel>(Items)
                    : new ObservableCollection<ConfigTemplateItemViewModel>(
                        Items.Where(x =>
                            x.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false ||
                            x.Id.ToString().Contains(SearchText)).ToList());

                FilteredItems = filtered;
                Raise(nameof(FilteredItemCount));
                _logger.LogInformation("Filtro aplicado. Resultados: {Count}", FilteredItems.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar filtro");
            }
        }
        private void ClearFilter()
        {
            SearchText = null;
            SelectedItem = null;
            _logger.LogInformation("Filtro limpiado");
        }

        #endregion

        #region Métodos Públicos (Async)
        private async Task RefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Iniciando recarga de plantillas");
                await _manager.RefreshAllAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Recarga cancelada por el usuario");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la recarga");
            }
        }
        private async Task LoadByIdAsync(int id, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Cargando plantilla con Id {Id}", id);
                await _manager.LoadByIdAsync(id, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Carga cancelada para Id {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando plantilla {Id}", id);
            }
        }
        private async Task CreateAsync(string? description, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(description))
                {
                    _logger.LogWarning("Intento de crear plantilla sin descripción");
                    return;
                }

                _logger.LogInformation("Creando nueva plantilla: {Description}", description);

                var command = new CreateConfigTemplateCommand
                {
                    Description = description.Trim(),
                    Columns = new List<DataFlow.Core.Models.ConfigColumn>()
                };

                var result = await _manager.CreateAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Plantilla creada exitosamente con Id {Id}", result.Value?.Id);
                }
                else
                {
                    _logger.LogWarning("Error al crear plantilla: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Creación cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al crear plantilla");
            }
        }
        private async Task EditSelectedAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedItem == null)
                {
                    _logger.LogWarning("Intento de editar sin item seleccionado");
                    return;
                }

                _logger.LogInformation("Editando plantilla Id {Id}", SelectedItem.Id);

                var command = new UpdateConfigTemplateCommand
                {
                    Id = SelectedItem.Id,
                    Description = SelectedItem.Description,
                    Columns = new List<DataFlow.Core.Models.ConfigColumn>()
                };

                var result = await _manager.UpdateAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Plantilla actualizada: {Id}", SelectedItem.Id);
                }
                else
                {
                    _logger.LogWarning("Error al actualizar: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Edición cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar plantilla");
            }
        }
        private async Task DeleteSelectedAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedItem == null)
                {
                    _logger.LogWarning("Intento de eliminar sin item seleccionado");
                    return;
                }

                _logger.LogInformation("Eliminando plantilla Id {Id}", SelectedItem.Id);

                var command = new DeleteConfigTemplateCommand(SelectedItem.Id);
                var result = await _manager.DeleteAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    SelectedItem = null;
                    _logger.LogInformation("Plantilla eliminada: {Id}", command.Id);
                }
                else
                {
                    _logger.LogWarning("Error al eliminar: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Eliminación cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar plantilla");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
            if (_manager != null)
                _manager.PropertyChanged -= OnManagerPropertyChanged;

        }

        #endregion
    }
}
