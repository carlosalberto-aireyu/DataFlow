using DataFlow.Core.Common;
using DataFlow.Core.Features;
using DataFlow.Core.Features.Commands;
using DataFlow.Core.Features.Queries;
using DataFlow.Core.Models;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.UI.Services
{
    public class HistProcessManager : ManagerBase, IHistProcessManager
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILogger<HistProcessManager> _logger;

        private bool _isBusy;
        private string? _errorMessage;

        public HistProcessManager(IQueryDispatcher queryDispatcher, ICommandDispatcher commandDispatcher, ILogger<HistProcessManager> logger)
        {
            _queryDispatcher = queryDispatcher ?? throw new ArgumentNullException(nameof(queryDispatcher));
            _commandDispatcher = commandDispatcher ?? throw new ArgumentNullException(nameof(commandDispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        private void SetBusy(bool busy) => _isBusy = busy;
        public void SetError(string? error)
        {
            _errorMessage = error;
            if (_errorMessage != null)
            {
                _logger.LogWarning("HistProcessManager error: {ErrorMessage}", _errorMessage);
            }
        }


        public async Task<Result<HistProcess>> CreateAsync(CreateHistProcessCommand cmd, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Creando nuevo HistProcess para plantilla #{configTemplateId}", cmd.ConfigTemplateId);
                var result = await _commandDispatcher.DispatchAsync<CreateHistProcessCommand, Result<HistProcess>>(cmd, cancellationToken);
                if (result.IsSuccess && result.Value != null)
                {
                    _logger.LogInformation("HistProcess creado exitosamente para plantilla #{configTemplateId}", cmd.ConfigTemplateId);
                    return result;
                }
                else
                {
                    var errorMsg = result.Error ?? $"No se pudo crear HistProcess para plantilla #{cmd.ConfigTemplateId}";
                    SetError(errorMsg);
                    _logger.LogWarning(errorMsg);
                    return Result<HistProcess>.Failure(errorMsg);
                }

            }
            catch (Exception ex)
            {
                var errorMsg = $"Error al crear HistProcess: {ex.Message}";
                SetError(errorMsg);
                _logger.LogError(ex, errorMsg);
                return Result<HistProcess>.Failure(errorMsg);
            }
            finally
            {
                SetBusy(false);
            }
        }
        public Task<Result<bool>> DeleteAsync(DeleteHistProcessCommand cmd, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<IReadOnlyList<HistProcess>>> LoadByConfigTemplateIdAsync(int configTemplateId, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Cargando las configuraciones de la plantilla seleccionada #{configTemplateId}", configTemplateId);

                var query = new GetHistProcessByConfigTemplateIdQuery(configTemplateId);
                var result = await _queryDispatcher.DispatchAsync<GetHistProcessByConfigTemplateIdQuery, Result<IReadOnlyList<HistProcess>>>(query, cancellationToken);
                if (result.IsSuccess && result.Value != null)
                {
                    _logger.LogInformation("Configuraciones de la plantilla #{configTemplateId} cargadas exitosamente", configTemplateId);
                    return result;
                }
                else
                {
                    var errorMsg = result.Error ?? $"No se encontraron configuraciones para la plantilla #{configTemplateId}";
                    SetError(errorMsg);
                    _logger.LogWarning(errorMsg);
                    return Result<IReadOnlyList<HistProcess>>.Failure(errorMsg);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error al cargar Historia de Proceso para plantilla #{configTemplateId}: {ex.Message}";
                SetError(errorMsg);
                _logger.LogError(ex, errorMsg);
                return Result<IReadOnlyList<HistProcess>>.Failure(errorMsg);

            }
            finally
            {
                SetBusy(false);
            }
        }

        public async Task<Result<HistProcess>> LoadByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            SetBusy(true);
            SetError(null);
            try
            {
                _logger.LogInformation("Cargando HistProcess para plantilla #{id}", id);
                var query = new GetHistProcessByIdQuery(id);
                var result = await _queryDispatcher.DispatchAsync<GetHistProcessByIdQuery, Result<HistProcess>>(query, cancellationToken);
                if (result.IsSuccess && result.Value != null)
                {
                    _logger.LogInformation("HistProcess cargado exitosamente para plantilla #{configTemplateId}", id);
                    return result;
                }
                else
                {
                    var errorMsg = result.Error ?? $"HistProcess no encontrado para plantilla #{id}";
                    SetError(errorMsg);
                    _logger.LogWarning(errorMsg);
                    return Result<HistProcess>.Failure(errorMsg);
                }

            }
            catch (Exception ex)
            {
                var errorMsg = $"Error al cargar Historia de Proceso para plantilla #{id}: {ex.Message}";
                SetError(errorMsg);
                _logger.LogError(ex, errorMsg);
                return Result<HistProcess>.Failure(errorMsg);
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}
