using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DataFlow.Core.Features.Commands;
using DataFlow.UI.Commands;
using DataFlow.UI.ViewModels.Base;
using DataFlow.UI.Services;
using Microsoft.Extensions.Logging;

namespace DataFlow.UI.ViewModels
{
    public class ConfigColumnsViewModel : ViewModelBase, IDisposable
    {
        private readonly IConfigColumnManager _manager;
        private readonly ILogger<ConfigColumnsViewModel> _logger;
        private readonly ConfigTemplatesViewModel _templatesViewModel;

        private ConfigColumnItemViewModel? _selectedColumn;
        private ColumnRangeItemViewModel? _selectedRange;
        private string? _searchText;
        private ObservableCollection<ConfigColumnItemViewModel> _filteredColumns;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _selectedTemplateId;

        #region Propiedades Bindables
        public ObservableCollection<ConfigColumnItemViewModel> Columns => _manager.Items;

        public ObservableCollection<ConfigColumnItemViewModel> FilteredColumns
        {
            get => _filteredColumns;
            private set
            {
                if (_filteredColumns != value)
                {
                    _filteredColumns = value;
                    Raise(nameof(FilteredColumns));
                }
            }
        }

        public ConfigColumnItemViewModel? SelectedColumn
        {
            get => _selectedColumn;
            set
            {
                if (_selectedColumn != value)
                {
                    _selectedColumn = value;
                    Raise(nameof(SelectedColumn));
                    Raise(nameof(IsColumnSelected));
                    _selectedRange = null;
                    Raise(nameof(SelectedRange));
                }
            }
        }

        public bool IsColumnSelected => SelectedColumn != null;

        public ColumnRangeItemViewModel? SelectedRange
        {
            get => _selectedRange;
            set
            {
                if (_selectedRange != value)
                {
                    _selectedRange = value;
                    Raise(nameof(SelectedRange));
                    Raise(nameof(IsRangeSelected));
                }
            }
        }

        public bool IsRangeSelected => SelectedRange != null;

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
        public int ItemCount => Columns.Count;
        public int FilteredItemCount => FilteredColumns.Count;
        
        public string? SelectedTemplateDescription
        {
            get
            {
                return _templatesViewModel.SelectedItem?.Description;
            }
        }

        #endregion

        #region Comandos
        public ICommand RefreshCommand { get; }
        public ICommand CreateCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand LoadByIdCommand { get; }
        public ICommand CreateRangeCommand { get; }
        public ICommand EditRangeCommand { get; }
        public ICommand DeleteRangeCommand { get; }
        public ICommand ClearFilterCommand { get; }

        #endregion

        public ConfigColumnsViewModel(
            IConfigColumnManager manager,
            ConfigTemplatesViewModel templatesViewModel,
            ILogger<ConfigColumnsViewModel> logger)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _templatesViewModel = templatesViewModel ?? throw new ArgumentNullException(nameof(templatesViewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _filteredColumns = new ObservableCollection<ConfigColumnItemViewModel>();
            _cancellationTokenSource = new CancellationTokenSource();

            _manager.PropertyChanged += OnManagerPropertyChanged;
            Columns.CollectionChanged += (s, e) =>
            {
                ApplyFilter();
                Raise(nameof(ItemCount));
            };

            _templatesViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ConfigTemplatesViewModel.SelectedItem))
                {
                    Raise(nameof(SelectedTemplateDescription));
                }
            };

            // Inicializar comandos
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            CreateCommand = new AsyncRelayCommand<dynamic>(CreateAsync);
            EditCommand = new AsyncRelayCommand(EditSelectedAsync);
            DeleteCommand = new AsyncRelayCommand(DeleteSelectedAsync);
            LoadByIdCommand = new AsyncRelayCommand<int>(LoadByIdAsync);
            CreateRangeCommand = new AsyncRelayCommand<(string, string, string)>(CreateRangeAsync);
            EditRangeCommand = new AsyncRelayCommand(EditSelectedRangeAsync);
            DeleteRangeCommand = new AsyncRelayCommand(DeleteSelectedRangeAsync);
            ClearFilterCommand = new RelayCommand(ClearFilter);

            _logger.LogInformation("ConfigColumnsViewModel inicializado");
        }

        #region Métodos Privados
        private void OnManagerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IConfigColumnManager.IsBusy))
                Raise(nameof(IsBusy));
            else if (e.PropertyName == nameof(IConfigColumnManager.ErrorMessage))
                Raise(nameof(ErrorMessage));
        }

        private void ApplyFilter()
        {
            try
            {
                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? new ObservableCollection<ConfigColumnItemViewModel>(Columns)
                    : new ObservableCollection<ConfigColumnItemViewModel>(
                        Columns.Where(x =>
                            (x.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            (x.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                            x.Id.ToString().Contains(SearchText)).ToList());

                FilteredColumns = filtered;
                Raise(nameof(FilteredItemCount));
                _logger.LogInformation("Filtro aplicado. Resultados: {Count}", FilteredColumns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al aplicar filtro");
            }
        }

        private void ClearFilter()
        {
            SearchText = null;
            SelectedColumn = null;
            SelectedRange = null;
            _logger.LogInformation("Filtro limpiado");
        }

        #endregion

        #region Métodos Públicos (Async)
        private async Task RefreshAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Iniciando recarga de columnas para la plantilla {TemplateId}", SelectedTemplateId);
                await _manager.RefreshAllAsync(SelectedTemplateId, cancellationToken);
                SelectedColumn = Columns.FirstOrDefault();
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
                _logger.LogInformation("Cargando columna con Id {Id}", id);
                await _manager.LoadByIdAsync(id, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Carga cancelada para Id {Id}", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cargando columna {Id}", id);
            }
        }

        private async Task CreateAsync(dynamic? columnData, CancellationToken cancellationToken)
        {
            try
            {
                int indexColumn = columnData?.IndexColumn ?? 0;
                string? columnName = columnData?.ColumnName;
                string? displayName = columnData?.DisplayName;
                string? description = columnData?.Description;
                int dataTypeId = columnData?.DataTypeId;
                string? defaultValue = columnData?.DefaultValue;
                int columnTypeId = columnData?.ColumnTypeId;

                if (string.IsNullOrWhiteSpace(columnName))
                {
                    _logger.LogWarning("Intento de crear columna sin nombre");
                    return;
                }

                if (SelectedTemplateId == 0)
                {
                    _logger.LogWarning("Intento de crear columna sin plantilla seleccionada");
                    return;
                }

                _logger.LogInformation("Creando nueva columna: {ColumnName}", columnName);

                var command = new CreateConfigColumnCommand
                {
                    IndexColumn = indexColumn,
                    ConfigTemplateId = SelectedTemplateId,
                    Name = columnName.Trim(),
                    NameDisplay = displayName?.Trim() ?? columnName.Trim(),
                    DataTypeId = dataTypeId,
                    DefaultValue = defaultValue?.Trim() ?? string.Empty,
                    Description = description?.Trim() ?? string.Empty,
                    ColumnTypeId = columnTypeId
                };

                var result = await _manager.CreateAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Columna creada exitosamente con Id {Id}", result.Value?.Id);
                }
                else
                {
                    _logger.LogWarning("Error al crear columna: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Creación cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al crear columna");
            }
        }

        private async Task EditSelectedAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedColumn == null)
                {
                    _logger.LogWarning("Intento de editar sin columna seleccionada");
                    return;
                }

                _logger.LogInformation("Editando columna Id {Id}", SelectedColumn.Id);

                var command = new UpdateConfigColumnCommand
                {
                    Id = SelectedColumn.Id,
                    IndexColumn = SelectedColumn.IndexColumn,
                    Name = SelectedColumn.Name,
                    NameDisplay = SelectedColumn.NameDisplay,
                    Description = SelectedColumn.Description,
                    DataTypeId = SelectedColumn.DataTypeId,
                    DefaultValue = SelectedColumn.DefaultValue ?? string.Empty,
                    ColumnTypeId = SelectedColumn.ColumnTypeId
                };

                var result = await _manager.UpdateAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Columna actualizada: {Id}", SelectedColumn.Id);
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
                _logger.LogError(ex, "Error al editar columna");
            }
        }

        private async Task DeleteSelectedAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedColumn == null)
                {
                    _logger.LogWarning("Intento de eliminar sin columna seleccionada");
                    return;
                }

                _logger.LogInformation("Eliminando columna Id {Id}", SelectedColumn.Id);

                var command = new DeleteConfigColumnCommand(SelectedColumn.Id);
                var result = await _manager.DeleteAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    SelectedColumn = null;
                    _logger.LogInformation("Columna eliminada: {Id}", command.Id);
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
                _logger.LogError(ex, "Error al eliminar columna");
            }
        }

        private async Task CreateRangeAsync((string rFrom, string rTo, string defaultValue) rangeData, CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedColumn == null)
                {
                    _logger.LogWarning("Intento de crear rango sin columna seleccionada");
                    return;
                }

                if (string.IsNullOrWhiteSpace(rangeData.rFrom) && string.IsNullOrWhiteSpace(rangeData.rTo))
                {
                    _logger.LogWarning("Intento de crear rango sin valores");
                    return;
                }

                _logger.LogInformation("Creando nuevo rango para columna {ColumnId}", SelectedColumn.Id);

                var command = new CreateColumnRangeCommand
                {
                    ConfigColumnId = SelectedColumn.Id,
                    RFrom = rangeData.rFrom?.Trim(),
                    RTo = rangeData.rTo?.Trim(),
                    DefaultValue = rangeData.defaultValue?.Trim()
                };

                var result = await _manager.CreateRangeAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Rango creado exitosamente con Id {Id}", result.Value?.Id);
                }
                else
                {
                    _logger.LogWarning("Error al crear rango: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Creación de rango cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al crear rango");
            }
        }

        private async Task EditSelectedRangeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedRange == null)
                {
                    _logger.LogWarning("Intento de editar rango sin rango seleccionado");
                    return;
                }

                _logger.LogInformation("Editando rango Id {Id}", SelectedRange.Id);

                var command = new UpdateColumnRangeCommand
                {
                    Id = SelectedRange.Id,
                    RFrom = SelectedRange.RFrom,
                    RTo = SelectedRange.RTo,
                    DefaultValue = SelectedRange.DefaultValue
                };

                var result = await _manager.UpdateRangeAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Rango actualizado: {Id}", SelectedRange.Id);
                }
                else
                {
                    _logger.LogWarning("Error al actualizar rango: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Edición de rango cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al editar rango");
            }
        }

        private async Task DeleteSelectedRangeAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (SelectedRange == null)
                {
                    _logger.LogWarning("Intento de eliminar rango sin rango seleccionado");
                    return;
                }

                _logger.LogInformation("Eliminando rango Id {Id}", SelectedRange.Id);

                var command = new DeleteColumnRangeCommand(SelectedRange.Id);
                var result = await _manager.DeleteRangeAsync(command, cancellationToken);

                if (result.IsSuccess)
                {
                    SelectedRange = null;
                    _logger.LogInformation("Rango eliminado: {Id}", command.Id);
                }
                else
                {
                    _logger.LogWarning("Error al eliminar rango: {Error}", result.Error);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Eliminación de rango cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar rango");
            }
        }
        public int SelectedTemplateId
        {
            get => _selectedTemplateId;
            set
            {
                if (_selectedTemplateId != value)
                {
                    var previousId = _selectedTemplateId;
                    _selectedTemplateId = value;
                    Raise(nameof(SelectedTemplateId));
                    Raise(nameof(SelectedTemplateDescription));

                    if (previousId != 0 && value != 0 && previousId != value)
                    {
                        _logger.LogInformation("Cambiando de plantilla {OldId} a {NewId} - Limpiando selecciones", previousId, value);

                        SelectedColumn = null;
                        SelectedRange = null;
                    }
                }
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