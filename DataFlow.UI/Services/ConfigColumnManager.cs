using DataFlow.Core.Common;
using DataFlow.Core.Constants;
using DataFlow.Core.Features;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Features.Queries;
using DataFlow.Core.Models;
using DataFlow.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DataFlow.UI.Services
{
    public class ConfigColumnManager : IConfigColumnManager
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILogger<ConfigColumnManager> _logger;
        private readonly LookupIds _lookupIds;

        private bool _isBusy;
        private string? _errorMessage;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ConfigColumnItemViewModel> Items { get; } = new();

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    Raise(nameof(IsBusy));
                }
            }
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            private set
            {
                if (_errorMessage != value)
                {
                    _errorMessage = value;
                    Raise(nameof(ErrorMessage));
                }
            }
        }

        public ConfigColumnManager(
            IQueryDispatcher queryDispatcher,
            ICommandDispatcher commandDispatcher,
            LookupIds lookupIds, 
            ILogger<ConfigColumnManager> logger)
        {
            _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
            _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
            _lookupIds = lookupIds ?? throw new ArgumentNullException(nameof(lookupIds)); // ✅ Ya disponible
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void SetBusy(bool busy) => IsBusy = busy;

        public void SetError(string? error)
        {
            ErrorMessage = error;
            if (ErrorMessage != null)
                _logger.LogWarning("ConfigColumnManager error: {ErrorMessage}", ErrorMessage);
        }

        public async Task RefreshAllAsync(int configTemplateId, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Cargando columnas para plantilla {TemplateId}", configTemplateId);

                var query = new GetConfigColumnsByTemplateIdQuery(configTemplateId);
                var result = await _queryDispatcher.DispatchAsync<GetConfigColumnsByTemplateIdQuery, Result<IReadOnlyList<ConfigColumn>>>(
                    query, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            Items.Clear();
                            foreach (var column in result.Value)
                            {
                                var columnVm = ConfigColumnItemViewModel.FromModel(column);

                                
                                if (columnVm.Ranges != null)
                                {
                                    bool isDimension = columnVm.ColumnTypeId == _lookupIds.Dimension;
                                    foreach (var range in columnVm.Ranges)
                                    {
                                        range.IsDimensionColumn = isDimension;
                                    }
                                }

                                Items.Add(columnVm);
                            }
                            _logger.LogInformation("Columnas cargadas: {Count}", Items.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error al actualizar la colección Items");
                            SetError($"Error al actualizar la colección: {ex.Message}");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido al cargar columnas.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Carga de columnas cancelada");
                SetError("Operación cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar columnas");
                SetError($"Error al cargar columnas: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task LoadByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Cargando columna con Id {Id}", id);

                var query = new GetConfigColumnByIdQuery(id);
                var result = await _queryDispatcher.DispatchAsync<GetConfigColumnByIdQuery, Result<ConfigColumn>>(
                    query, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(x => x.Id == id);
                        if (existing != null)
                        {
                            existing.UpdateFromModel(result.Value);

                            
                            if (existing.Ranges != null)
                            {
                                bool isDimension = existing.ColumnTypeId == _lookupIds.Dimension;
                                foreach (var range in existing.Ranges)
                                {
                                    range.IsDimensionColumn = isDimension;
                                }
                            }
                        }
                        else
                        {
                            var columnVm = ConfigColumnItemViewModel.FromModel(result.Value);

                            if (columnVm.Ranges != null)
                            {
                                bool isDimension = columnVm.ColumnTypeId == _lookupIds.Dimension;
                                foreach (var range in columnVm.Ranges)
                                {
                                    range.IsDimensionColumn = isDimension;
                                }
                            }

                            Items.Add(columnVm);
                        }
                    });

                    _logger.LogInformation("Columna cargada: {Id}", id);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido al cargar la columna.");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Carga de columna cancelada");
                SetError("Operación cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar columna");
                SetError($"Error al cargar columna: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<ConfigColumn>> CreateAsync(
            CreateConfigColumnCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Creando columna: {Name}", cmd.Name);

                var result = await _commandDispatcher.DispatchAsync<CreateConfigColumnCommand, Result<ConfigColumn>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    await LoadByIdAsync(result.Value.Id, cancellationToken);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la creación de la columna.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear columna");
                SetError($"Error al crear columna: {ex.Message}");
                return Result<ConfigColumn>.Failure(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<ConfigColumn>> UpdateAsync(
            UpdateConfigColumnCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Actualizando columna: {Id}", cmd.Id);

                var result = await _commandDispatcher.DispatchAsync<UpdateConfigColumnCommand, Result<ConfigColumn>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var existing = Items.FirstOrDefault(x => x.Id == result.Value.Id);
                        if (existing != null)
                        {
                            existing.UpdateFromModel(result.Value);

                            
                            if (existing.Ranges != null)
                            {
                                bool isDimension = existing.ColumnTypeId == _lookupIds.Dimension;
                                foreach (var range in existing.Ranges)
                                {
                                    range.IsDimensionColumn = isDimension;
                                }
                            }

                            _logger.LogInformation(
                                "ViewModel actualizado - Id: {Id}, DataType: {DataType}, ColumnType: {ColumnType}",
                                existing.Id,
                                existing.DataType?.Code ?? "null",
                                existing.ColumnType?.Code ?? "null");
                        }
                    }, System.Windows.Threading.DispatcherPriority.Normal);

                    _logger.LogInformation("Columna actualizada: {Id}", result.Value.Id);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la actualización de la columna.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar columna");
                SetError($"Error al actualizar columna: {ex.Message}");
                return Result<ConfigColumn>.Failure(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<bool>> DeleteAsync(
            DeleteConfigColumnCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Eliminando columna: {Id}", cmd.Id);

                var result = await _commandDispatcher.DispatchAsync<DeleteConfigColumnCommand, Result<bool>>(
                    cmd, cancellationToken);

                if (result.IsSuccess)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var itemToRemove = Items.FirstOrDefault(x => x.Id == cmd.Id);
                        if (itemToRemove != null)
                        {
                            Items.Remove(itemToRemove);
                        }
                    });
                    _logger.LogInformation("Columna eliminada: {Id}", cmd.Id);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la eliminación de la columna.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar columna");
                SetError($"Error al eliminar columna: {ex.Message}");
                return Result<bool>.Failure(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<ColumnRange>> CreateRangeAsync(
            CreateColumnRangeCommand cmd, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Creando rango para columna: {ColumnId}", cmd.ConfigColumnId);

                var result = await _commandDispatcher.DispatchAsync<CreateColumnRangeCommand, Result<ColumnRange>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var column = Items.FirstOrDefault(x => x.Id == cmd.ConfigColumnId);
                        if (column != null)
                        {
                            var ranges = column.Ranges;
                            if (ranges != null)
                            {
                                var placeholder = ranges.FirstOrDefault(r =>
                                    r.Id == 0 &&
                                    string.Equals(r.RFrom?.Trim(), cmd.RFrom?.Trim(), StringComparison.Ordinal) &&
                                    string.Equals(r.RTo?.Trim(), cmd.RTo?.Trim(), StringComparison.Ordinal));

                                if (placeholder != null)
                                {
                                    placeholder.UpdateFromModel(result.Value);
                                    
                                    placeholder.IsDimensionColumn = column.ColumnTypeId == _lookupIds.Dimension;
                                }
                                else
                                {
                                    var newRangeVm = ColumnRangeItemViewModel.FromModel(result.Value);

                                    newRangeVm.IsDimensionColumn = column.ColumnTypeId == _lookupIds.Dimension;

                                    ranges.Add(newRangeVm);
                                }
                            }
                        }
                    });
                    _logger.LogInformation("Rango creado con Id {Id}", result.Value.Id);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la creación del rango.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rango");
                SetError($"Error al crear rango: {ex.Message}");
                return Result<ColumnRange>.Failure(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<ColumnRange>> UpdateRangeAsync(
            UpdateColumnRangeCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Actualizando rango: {Id}", cmd.Id);

                var result = await _commandDispatcher.DispatchAsync<UpdateColumnRangeCommand, Result<ColumnRange>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var columnHoldingRange = this.Items.FirstOrDefault(col =>
                            col.Ranges != null && col.Ranges.Any(rangeVm => rangeVm.Id == cmd.Id));

                        if (columnHoldingRange != null)
                        {
                            var existingRangeItemVm = columnHoldingRange.Ranges?.FirstOrDefault(x => x.Id == cmd.Id);
                            if (existingRangeItemVm != null)
                            {
                                existingRangeItemVm.UpdateFromModel(result.Value);

                                existingRangeItemVm.IsDimensionColumn = columnHoldingRange.ColumnTypeId == _lookupIds.Dimension;
                            }
                        }
                    });
                    _logger.LogInformation("Rango actualizado: {Id}", result.Value.Id);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la actualización del rango.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar rango");
                SetError($"Error al actualizar rango: {ex.Message}");
                return Result<ColumnRange>.Failure(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<bool>> DeleteRangeAsync(
            DeleteColumnRangeCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Eliminando rango: {Id}", cmd.Id);

                var result = await _commandDispatcher.DispatchAsync<DeleteColumnRangeCommand, Result<bool>>(
                    cmd, cancellationToken);

                if (result.IsSuccess)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var columnHoldingRange = this.Items.FirstOrDefault(col =>
                            col.Ranges != null && col.Ranges.Any(rangeVm => rangeVm.Id == cmd.Id));

                        if (columnHoldingRange != null)
                        {
                            var rangeToRemove = columnHoldingRange.Ranges?.FirstOrDefault(r => r.Id == cmd.Id);
                            if (rangeToRemove != null)
                            {
                                columnHoldingRange.Ranges?.Remove(rangeToRemove);
                            }
                        }
                    });
                    _logger.LogInformation("Rango eliminado: {Id}", cmd.Id);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la eliminación del rango.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar rango");
                SetError($"Error al eliminar rango: {ex.Message}");
                return Result<bool>.Failure(ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}