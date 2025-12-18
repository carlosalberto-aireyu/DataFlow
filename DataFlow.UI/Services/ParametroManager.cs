using DataFlow.Core.Common;
using DataFlow.Core.Constants;
using DataFlow.Core.Features;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Features.Queries;
using DataFlow.Core.Models; 
using DataFlow.UI.ViewModels;
using DocumentFormat.OpenXml.Office.Word;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; 

namespace DataFlow.UI.Services
{
    public class ParametroManager : IParametroManager
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILogger<ParametroManager> _logger;

        private bool _isBusy;
        private string? _errorMessage;

        public ParametroManager(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher, ILogger<ParametroManager> logger)
        {
            _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
            _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ObservableCollection<ParametroItemViewModel> Items { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void Raise(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

        private void SetBusy(bool busy) => IsBusy = busy;

        public void SetError(string? error)
        {
            ErrorMessage = error;
            if (ErrorMessage != null)
                _logger.LogWarning("ParametroManager error: {ErrorMessage}", ErrorMessage);
        }

        public async Task<Result<ParametroItemViewModel>> CreateAsync(string key, string name, string value, string? description = null, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Creando Parámetro con clave: {Key}", key);
                var cmd = new CreateParametroCommand
                {
                    ParametroKey = key,
                    Name = name,
                    ParametroValue = value,
                    Description = description
                };

                var result = await _commandDispatcher.DispatchAsync<CreateParametroCommand, Result<Parametro>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    ParametroItemViewModel newParametroVm = ParametroItemViewModel.FromModel(result.Value);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Items.Add(newParametroVm);
                    });
                    _logger.LogInformation("Parámetro '{Key}' creado con Id {Id}", key, result.Value.Id);
                    return Result<ParametroItemViewModel>.Success(newParametroVm);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la creación del parámetro.");
                    _logger.LogWarning("Error al crear el parámetro '{Key}': {Error}", key, result.Error);
                    return Result<ParametroItemViewModel>.Failure(result.Error ?? "Error desconocido en la creación del parámetro.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CreateAsync para el parámetro '{Key}'", key);
                SetError($"Error al crear el parámetro: {ex.Message}");
                return Result<ParametroItemViewModel>.Failure($"Error al crear el parámetro: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<bool>> DeleteAsync(ParametroItemViewModel parametroViewModel, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Eliminando Parámetro: {ParametroKey}", parametroViewModel.ParametroKey);
                var cmd = new DeleteParametroCommand
                {
                    ParametroKey = parametroViewModel.ParametroKey
                };
                var result = await _commandDispatcher.DispatchAsync<DeleteParametroCommand, Result<bool>>(cmd, cancellationToken);
                if (result.IsSuccess && result.Value)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Items.Remove(parametroViewModel);
                    });
                    _logger.LogInformation("Parámetro '{ParametroKey}' eliminado", parametroViewModel.ParametroKey);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido al eliminar el parámetro.");
                    _logger.LogWarning("Error al eliminar el parámetro '{ParametroKey}': {Error}", parametroViewModel.ParametroKey, result.Error);
                    return Result<bool>.Failure(result.Error ?? "Error desconocido al eliminar el parámetro.");
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el parámetro '{ParametroKey}'", parametroViewModel.ParametroKey);
                SetError($"Error al eliminar el parámetro: {ex.Message}");
                return Result<bool>.Failure($"Error al eliminar el parámetro: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<string?> GetParametroValueByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                var qry = new GetParametroByKeyQuery(key);
                var result = await _queryDispatcher.DispatchAsync<GetParametroByKeyQuery, Result<Parametro?>>(qry, cancellationToken);
                if (result.IsSuccess && result.Value != null)
                {
                    return result.Value.ParametroValue;
                }
                else
                {
                    SetError(result.Error ?? $"Error desconocido al obtener el valor del parámetro '{key}'.");
                    _logger.LogWarning("Error al obtener el valor del parámetro '{Key}': {Error}", key, result.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en GetParametroValueByKeyAsync para la clave {Key}", key);
                SetError($"Error al obtener el valor del parámetro: {ex.Message}");
                return null;
            }
            finally
            {
                SetBusy(false);
            }
        }
        public async Task<Result<List<ParametroItemViewModel>>> LoadAllAsync(CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                var qry = new GetAllParametrosQuery();
                var result = await _queryDispatcher.DispatchAsync<GetAllParametrosQuery, Result<IReadOnlyList<Parametro>>>
                        (qry, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    var parametroViewModels = result.Value
                        .Select(ParametroItemViewModel.FromModel)
                        .ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Items.Clear();
                        foreach (var parametroVm in parametroViewModels)
                        {
                            Items.Add(parametroVm);
                        }
                    });
                    _logger.LogInformation("Cargados {Count} parámetros del sistema.", parametroViewModels.Count);
                    return Result<List<ParametroItemViewModel>>.Success(parametroViewModels); // Devolver la lista
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido al cargar los parámetros.");
                    _logger.LogWarning("Error al cargar los parámetros: {Error}", result.Error);
                    return Result<List<ParametroItemViewModel>>.Failure(result.Error ?? "Error desconocido al cargar los parámetros.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LoadAllAsync parámetros");
                SetError($"Error al cargar los parámetros: {ex.Message}");
                return Result<List<ParametroItemViewModel>>.Failure($"Error al cargar los parámetros: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<bool>> SetParametroValueByKeyAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                var getQry = new GetParametroByKeyQuery(key);
                var getResult = await _queryDispatcher.DispatchAsync<GetParametroByKeyQuery, Result<Parametro?>>(getQry, cancellationToken);

                if (getResult.IsSuccess && getResult.Value != null)
                {
                    _logger.LogInformation("Actualizando Parámetro con clave: {Key}", key);
                    var updateCmd = new UpdateParametroCommand
                    {
                        ParametroKey = key,
                        ParametroValue = value,
                        Description = getResult.Value.Description
                    };
                    var updateResult = await _commandDispatcher.DispatchAsync<UpdateParametroCommand, Result<Parametro>>(updateCmd, cancellationToken);

                    if (updateResult.IsSuccess && updateResult.Value != null)
                    {
                        var updatedVm = ParametroItemViewModel.FromModel(updateResult.Value);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            var existing = Items.FirstOrDefault(p => p.ParametroKey == updatedVm.ParametroKey);
                            if (existing != null)
                            {
                                existing.UpdateFromModel(updatedVm.ToModel());
                            }
                            else
                            {
                                Items.Add(updatedVm);
                            }
                        });
                        return Result<bool>.Success(true);
                    }
                    else
                    {
                        SetError(updateResult.Error ?? $"Error desconocido al establecer el valor del parámetro '{key}'.");
                        _logger.LogWarning("Error al establecer el valor del parámetro '{Key}': {Error}", key, updateResult.Error);
                        return Result<bool>.Failure(updateResult.Error ?? $"Error desconocido al establecer el valor del parámetro '{key}'.");
                    }
                }
                else
                {
                    _logger.LogInformation("Creando Parámetro con clave: {Key} ya que no existe.", key);
                    var createCmd = new CreateParametroCommand
                    {
                        ParametroKey = key,
                        ParametroValue = value,
                        Description = null
                    };
                    var createResult = await _commandDispatcher.DispatchAsync<CreateParametroCommand, Result<Parametro>>(createCmd, cancellationToken);

                    if (createResult.IsSuccess && createResult.Value != null)
                    {
                        var createdVm = ParametroItemViewModel.FromModel(createResult.Value);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Items.Add(createdVm);
                        });
                        return Result<bool>.Success(true);
                    }
                    else
                    {
                        SetError(createResult.Error ?? $"Error desconocido al crear el parámetro '{key}'.");
                        _logger.LogWarning("Error al crear el parámetro '{Key}': {Error}", key, createResult.Error);
                        return Result<bool>.Failure(createResult.Error ?? $"Error desconocido al crear el parámetro '{key}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en SetParametroValueByKeyAsync para la clave '{Key}'", key);
                SetError($"Error al establecer el valor del parámetro: {ex.Message}");
                return Result<bool>.Failure($"Error al establecer el valor del parámetro: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<ParametroItemViewModel>> UpdateAsync(ParametroItemViewModel parametroViewModel, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Actualizando Parámetro con clave: {ParametroKey}", parametroViewModel.ParametroKey);
                var cmd = new UpdateParametroCommand
                {
                    ParametroKey = parametroViewModel.ParametroKey,
                    ParametroValue = parametroViewModel.ParametroValue,
                    Description = parametroViewModel.Description
                };

                var result = await _commandDispatcher.DispatchAsync<UpdateParametroCommand, Result<Parametro>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(p => p.Id == parametroViewModel.Id); // Buscar por Id es más robusto si la clave puede cambiar
                        if (existing != null)
                        {
                            existing.UpdateFromModel(result.Value);
                        }
                        else
                        {
                            Items.Add(ParametroItemViewModel.FromModel(result.Value));
                        }
                    });
                    _logger.LogInformation("Parámetro '{Key}' actualizado con Id {Id}", parametroViewModel.ParametroKey, result.Value.Id);
                    return Result<ParametroItemViewModel>.Success(parametroViewModel); // Devolver el mismo VM que ahora está actualizado
                }
                else
                {
                    SetError(result.Error ?? $"Error desconocido en la actualización del parámetro '{parametroViewModel.ParametroKey}'.");
                    _logger.LogWarning("Error al actualizar el parámetro '{Key}': {Error}", parametroViewModel.ParametroKey, result.Error);
                    return Result<ParametroItemViewModel>.Failure(result.Error ?? $"Error desconocido en la actualización del parámetro '{parametroViewModel.ParametroKey}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UpdateAsync para el parámetro '{Key}'", parametroViewModel.ParametroKey);
                SetError($"Error al actualizar el parámetro: {ex.Message}");
                return Result<ParametroItemViewModel>.Failure($"Error al actualizar el parámetro: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }
        public async Task<Result<bool>> ExportarInformacion(ParametroItemViewModel parametroViewModel, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                var cmd = new ExportarInformacionCommand(parametroViewModel.Id);
                var result = await _commandDispatcher.DispatchAsync<ExportarInformacionCommand, Result<bool>>(cmd, cancellationToken);
                if (result.IsFailure)
                {
                    _logger.LogError("Error al exportar la información del parámetro '{ParametroKey}': {Error}", parametroViewModel.ParametroKey, result.Error);
                    return Result<bool>.Failure($"Ha ocurrido un error al exportar la información : {result.Error}");
                }

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al exportar la información del parámetro '{ParametroKey}'", parametroViewModel.ParametroKey);
                SetError($"Error al exportar la información del parámetro: {ex.Message}");
                return Result<bool>.Failure($"Error al exportar la información del parámetro: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }
        public async Task<Result<bool>> ImportarInformacionJson((string Path, bool Overwrite) data, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                var cmd = new ImportarInformacionCommand(data.Path, data.Overwrite);
                var result = await _commandDispatcher.DispatchAsync<ImportarInformacionCommand, Result<bool>>(cmd, cancellationToken);
                if (result.IsFailure)
                {
                    _logger.LogError("Error al importar la información desde el archivo '{Path}': {Error}", data.Path, result.Error);
                    return Result<bool>.Failure($"Ha ocurrido un error al importar la información : {result.Error}");
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al importar la información desde el archivo '{FilePath}'", data.Path);
                SetError($"Error al importar la información del archivo: {ex.Message}");
                return Result<bool>.Failure($"Error al importar la información del archivo: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}