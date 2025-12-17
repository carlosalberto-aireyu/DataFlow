using DataFlow.Core.Common;
using DataFlow.Core.Features;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Features.Queries;
using DataFlow.Core.Models;
using DataFlow.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace DataFlow.UI.Services
{
    public class ConfigTemplateManager : IConfigTemplateManager
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILogger<ConfigTemplateManager> _logger;

        private bool _isBusy;
        private string? _errorMessage;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ConfigTemplateItemViewModel> Items { get; } = new();

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

        public ConfigTemplateManager(
            IQueryDispatcher queryDispatcher,
            ICommandDispatcher commandDispatcher,
            ILogger<ConfigTemplateManager> logger)
        {
            _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
            _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private void SetBusy(bool busy) => IsBusy = busy;

        public void SetError(string? error)
        {
            ErrorMessage = error;
            if (ErrorMessage != null)
                _logger.LogWarning("ConfigTemplateManager error: {ErrorMessage}", ErrorMessage);
        }

        public async Task<Result<ConfigTemplate>> CreateAsync(
            CreateConfigTemplateCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Creando plantilla: {Description}", cmd.Description);

                var result = await _commandDispatcher.DispatchAsync<CreateConfigTemplateCommand, Result<ConfigTemplate>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Items.Add(ConfigTemplateItemViewModel.FromModel(result.Value));
                    });
                    _logger.LogInformation("Plantilla creada con Id {Id}", result.Value.Id);
                }
                else
                {
                    SetError(result.Error ?? "Error desconocido en la creación de la plantilla.");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CreateAsync");
                SetError("Error al crear la plantilla");
                return Result<ConfigTemplate>.Failure("Error al crear la plantilla");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<bool>> DeleteAsync(
            DeleteConfigTemplateCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Eliminando plantilla con Id {Id}", cmd.Id);

                var result = await _commandDispatcher.DispatchAsync<DeleteConfigTemplateCommand, Result<bool>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(i => i.Id == cmd.Id);
                        if (existing != null) Items.Remove(existing);
                    });
                    _logger.LogInformation("Plantilla eliminada: {Id}", cmd.Id);
                }
                else
                {
                    SetError(result.Error ?? "Error al eliminar la plantilla");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en DeleteAsync");
                SetError("Error al eliminar plantilla.");
                return Result<bool>.Failure("Error al eliminar plantilla.");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<ConfigTemplate>> LoadByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Cargando plantilla con Id {Id}", id);

                var query = new GetConfigTemplateByIdQuery(id);
                var result = await _queryDispatcher.DispatchAsync<GetConfigTemplateByIdQuery, Result<ConfigTemplate>>(
                    query, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(i => i.Id == result.Value.Id);
                        if (existing == null)
                        {
                            Items.Add(ConfigTemplateItemViewModel.FromModel(result.Value));
                        }
                        else
                        {
                            existing.UpdateFromModel(result.Value);
                        }
                    });
                    _logger.LogInformation("Plantilla cargada: {Description}", result.Value.Description);
                    
                }
                else
                {
                    SetError(result.Error ?? "Plantilla no encontrada");
                    return Result<ConfigTemplate>.Failure("Error al consultar LoadByIdAsyn");
                }
                return Result<ConfigTemplate>.Success(result.Value!);
            }
            catch (OperationCanceledException)
            {
                SetError("Operación cancelada.");
                _logger.LogWarning("Operación cancelada en LoadByIdAsync");
                return Result<ConfigTemplate>.Failure("Operacion Cancelada LoadByIdAsyn");
            }
            catch (Exception ex)
            {
                SetError("Error al cargar plantilla.");
                _logger.LogError(ex, "Error en LoadByIdAsync para Id {Id}", id);
                return Result<ConfigTemplate>.Failure("Error al consultar LoadByIdAsyn");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task RefreshAllAsync(CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Cargando todas las plantillas");

                var query = new GetAllConfigTemplatesQuery();
                var result = await _queryDispatcher.DispatchAsync<GetAllConfigTemplatesQuery, Result<IReadOnlyList<ConfigTemplate>>>(
                    query, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Items.Clear();
                        foreach (var template in result.Value)
                        {
                            Items.Add(ConfigTemplateItemViewModel.FromModel(template));
                        }
                    });
                    _logger.LogInformation("Se cargaron {Count} plantillas", result.Value.Count);
                }
                else
                {
                    SetError(result.Error ?? "No se pudieron cargar las plantillas");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en RefreshAllAsync");
                SetError("Error al cargar las plantillas");
            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<ConfigTemplate>> UpdateAsync(
            UpdateConfigTemplateCommand cmd,
            CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Actualizando plantilla con Id {Id}", cmd.Id);

                var result = await _commandDispatcher.DispatchAsync<UpdateConfigTemplateCommand, Result<ConfigTemplate>>(
                    cmd, cancellationToken);

                if (result.IsSuccess && result.Value != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var existing = Items.FirstOrDefault(i => i.Id == result.Value.Id);
                        if (existing != null) existing.UpdateFromModel(result.Value);
                    });
                    _logger.LogInformation("Plantilla actualizada: {Description}", result.Value.Description);
                }
                else
                {
                    SetError(result.Error ?? "Error al actualizar la plantilla");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en UpdateAsync");
                SetError("Error al actualizar la plantilla");
                return Result<ConfigTemplate>.Failure("Error al actualizar la plantilla");
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}
