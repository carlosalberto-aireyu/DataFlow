using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class UpdateConfigColumnCommandHandler : ICommandHandler<UpdateConfigColumnCommand, Result<ConfigColumn>>
    {
        private readonly IConfigColumnRepository _columnRepository;
        private readonly ILookupRepository _lookupRepository;
        private readonly ILogger<UpdateConfigColumnCommandHandler> _logger;

        public UpdateConfigColumnCommandHandler(IConfigColumnRepository columnRepository,
            ILogger<UpdateConfigColumnCommandHandler> logger,
            ILookupRepository lookupRepository)
        {
            _columnRepository = columnRepository;
            _logger = logger;
            _lookupRepository = lookupRepository;
        }

        public async Task<Result<ConfigColumn>> HandleAsync(UpdateConfigColumnCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (command == null)
            {
                _logger.LogWarning($"El comando no puede ser nulo. ({nameof(command)})");
                return Result<ConfigColumn>.Failure($"El comando no puede ser nulo. ({nameof(command)})");
            }

            if (command.IndexColumn < 0)
            {
                _logger.LogWarning("Intento de actualizar una columna con indice invalido.");
                return Result<ConfigColumn>.Failure("El indice de la columna es requerido y debe ser mayor o igual a cero.");
            }

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                _logger.LogWarning("Intento de crear una columna sin nombre.");
                return Result<ConfigColumn>.Failure("El nombre de la columna es requerido.");
            }

            if (string.IsNullOrWhiteSpace(command.NameDisplay))
            {
                _logger.LogWarning("Intento de crear una columna sin nombre para mostrar.");
                return Result<ConfigColumn>.Failure("El nombre para mostrar de la columna es requerido.");
            }

            if (command.DataTypeId <= 0)
            {
                _logger.LogWarning("Intento de crear una columna sin tipo de dato.");
                return Result<ConfigColumn>.Failure("El tipo de dato de la columna es requerido.");
            }

            if (!await _lookupRepository.DataTypeExistsAsync(command.DataTypeId, cancellationToken))
            {
                _logger.LogWarning("Intento de crear una columna con un tipo de dato inexistente.");
                return Result<ConfigColumn>.Failure("El tipo de dato de la columna no existe.");
            }

            if (command.ColumnTypeId <= 0)
            {
                _logger.LogWarning("Intento de crear una columna sin tipo de columna.");
                return Result<ConfigColumn>.Failure("El tipo de columna es requerido.");
            }

            if (!await _lookupRepository.ColumnTypeExistsAsync(command.ColumnTypeId, cancellationToken))
            {
                _logger.LogWarning("Intento de crear una columna con un tipo de columna inexistente.");
                return Result<ConfigColumn>.Failure("El tipo de columna no existe.");
            }

            try
            {
                var configColumn = await _columnRepository.GetWithDetailAsync(command.Id, cancellationToken);

                if (configColumn == null)
                {
                    _logger.LogWarning("El ID {Id}, de la columna no existe.", command.Id);
                    return Result<ConfigColumn>.Failure($"El ID {command.Id}, de la columna no existe.");
                }

                // Actualizar propiedades
                configColumn.IndexColumn = command.IndexColumn;
                if (command.Name != null)
                    configColumn.Name = command.Name;
                if (command.NameDisplay != null)
                    configColumn.NameDisplay = command.NameDisplay;
                if (command.Description != null)
                    configColumn.Description = command.Description;
                configColumn.DataTypeId = command.DataTypeId;
                configColumn.ColumnTypeId = command.ColumnTypeId;
                if (command.DefaultValue != null)
                    configColumn.DefaultValue = command.DefaultValue;

                configColumn.UpdatedAt = DateTime.UtcNow;

                cancellationToken.ThrowIfCancellationRequested();

                await _columnRepository.UpdateAsync(configColumn, cancellationToken);
                var change = await _columnRepository.SaveChangesAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                if (change <= 0)
                {
                    _logger.LogError($"No se pudo actualizar la columna con Id {command.Id}.");
                    return Result<ConfigColumn>.Failure($"No se pudo actualizar la columna con Id {command.Id}.");
                }

                
                var updatedColumn = await _columnRepository.GetWithDetailAsync(command.Id, cancellationToken);

                if (updatedColumn == null)
                {
                    _logger.LogWarning("No se pudo recargar la columna actualizada con Id {Id}", command.Id);
                    return Result<ConfigColumn>.Success(configColumn);
                }

                _logger.LogInformation("Columna {Id} actualizada exitosamente. Registros afectados: {Change}", command.Id, change);

                return Result<ConfigColumn>.Success(updatedColumn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar la columna con Id {command.Id}: {ex.Message}");
                return Result<ConfigColumn>.Failure($"Error al actualizar la columna con Id {command.Id}: {ex.Message}");
            }

        }
    }
}
