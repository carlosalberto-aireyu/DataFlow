using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using DataFlow.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class ChangeColumnReferenceCommandHandler : ICommandHandler<ChangeColumnReferenceCommand, Result<ConfigColumn>>

    {
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly ILogger<ChangeColumnReferenceCommandHandler> _logger;

        public ChangeColumnReferenceCommandHandler(IConfigColumnRepository configColumnRepository, ILogger<ChangeColumnReferenceCommandHandler> logger)
        {
            _configColumnRepository = configColumnRepository ?? throw new ArgumentNullException(nameof(configColumnRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ConfigColumn>> HandleAsync(ChangeColumnReferenceCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                _logger.LogWarning("Comando nulo en ChangeColumnReferenceCommandHandler");
                return Result<ConfigColumn>.Failure("El comando no puede ser nulo.");
            }
            if(command.ConfigColumnId <= 0)
            {
                _logger.LogWarning("ID de Columna no valido: {ConfigColumnId}", command.ConfigColumnId);
                return Result<ConfigColumn>.Failure("El ID de la columna de configuración es inválido.");
            }

            var column = await _configColumnRepository.GetWithDetailAsync(command.ConfigColumnId, cancellationToken);
            if(column == null)
            {
                _logger.LogWarning("Columna de configuración no encontrada: {ConfigColumnId}", command.ConfigColumnId);
                return Result<ConfigColumn>.Failure("Columna de configuración no encontrada.");
            }
            if(column.Ranges.Count <= 0)
            {
                _logger.LogWarning("La columna de configuración no tiene rangos asociados: {ConfigColumnId}", command.ConfigColumnId);
                return Result<ConfigColumn>.Failure("La columna de configuración no tiene rangos asociados.");
            }
            try
            {
                try
                {
                    ExcelAddressConverter.ColumnLettersToNumber(command.NewColumnLetter);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning("Letra de la nueva columna inválida: {NewColumnLetter}", command.NewColumnLetter);
                    return Result<ConfigColumn>.Failure("La letra de columna proporcionada no es válida.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                if(column.Ranges != null && column.Ranges.Count > 0)
                {
                    int rangesUpdated = 0;
                    foreach (var range in column.Ranges)
                    {
                        if(string.IsNullOrWhiteSpace(range.RFrom) || string.IsNullOrWhiteSpace(range.RTo))
                        {
                            _logger.LogWarning("Rango inválido en la columna de configuración: {ConfigColumnId}, RFrom: {RFrom}, RTo: {RTo}", command.ConfigColumnId, range.RFrom, range.RTo);
                            continue;
                        }

                        try
                        {
                            var (newRFrom, newRTo) = ExcelAddressConverter.ChangeColumnInRange(
                                range.RFrom, range.RTo, command.NewColumnLetter);
                            _logger.LogInformation(
                                "Actualizando rango de {OldRange} a {NewRange}",
                                $"{range.RFrom}:{range.RTo}",
                                $"{newRFrom}:{newRTo}");
                            range.RFrom = newRFrom;
                            range.RTo = newRTo;
                            range.UpdatedAt = DateTime.UtcNow;

                            rangesUpdated++;
                        }
                        catch (ArgumentException ex)
                        {
                            _logger.LogError(
                               ex,
                               "Error al convertir rango: {RFrom}:{RTo}",
                               range.RFrom,
                               range.RTo);
                            return Result<ConfigColumn>.Failure(
                                $"Error al convertir rango {range.RFrom}:{range.RTo}: {ex.Message}");
                        }   
                    }
                    _logger.LogInformation(
                        "{RangesUpdated} rangos actualizados en columna '{ColumnName}' a nueva columna {NewColumn}",
                        rangesUpdated,
                        column.Name,
                        command.NewColumnLetter);
                    column.UpdatedAt = DateTime.UtcNow;
                } else
                {
                    _logger.LogWarning("La columna {ColumnId} no tiene rangos asociados", column.Id);
                    return Result<ConfigColumn>.Failure("La columna no tiene rangos asociados.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                await _configColumnRepository.UpdateAsync(column, cancellationToken);
                await _configColumnRepository.SaveChangesAsync(cancellationToken);
                _logger.LogInformation(
                   "Cambio de referencias completado exitosamente para columna {ColumnId}",
                   column.Id);


                return Result<ConfigColumn>.Success(column);

            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Operación cancelada en ChangeColumnReferenceCommandHandler");
                return Result<ConfigColumn>.Failure("La operación fue cancelada.");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error crítico en ChangeColumnReferenceCommandHandler para columna {ColumnId}",
                    command.ConfigColumnId);
                return Result<ConfigColumn>.Failure($"Error al cambiar referencias: {ex.Message}");
            }
        }
    }
}
