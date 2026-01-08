using DataFlow.Core.Common;
using DataFlow.Core.Features;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class CopyConfigColumnCommandHandler : ICommandHandler<CopyConfigColumnCommand, Result<ConfigColumn>>
    {
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly ILogger<DeleteConfigColumnCommandHandler> _logger;

        public CopyConfigColumnCommandHandler(IConfigColumnRepository configColumnRepository, ILogger<DeleteConfigColumnCommandHandler> logger)
        {
            _configColumnRepository = configColumnRepository ?? throw new ArgumentNullException(nameof(configColumnRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        public async Task<Result<ConfigColumn>> HandleAsync(CopyConfigColumnCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command is null)
            {
                _logger.LogWarning($"{nameof(command)}, valor de comando nulo");
                return Result<ConfigColumn>.Failure($"{nameof(command)} no puede ser null.");
            }
            var existingColumn = await _configColumnRepository.GetWithDetailAsync(command.Id, cancellationToken);
            if(existingColumn == null)
            {
                _logger.LogWarning("Intento de copiar una columna inexistente. ID: {Id}", command.Id);
                return Result<ConfigColumn>.Failure($"Intento de copiar una columna inexistente: {command.Id}");
            }

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var now = DateTime.UtcNow;
                var newColumn = new ConfigColumn
                {
                    ConfigTemplateId = existingColumn.ConfigTemplateId,
                    IndexColumn = existingColumn.IndexColumn + 1,
                    Name = existingColumn.Name + " (Copia)",
                    NameDisplay = existingColumn.NameDisplay + " (Copia)",
                    Description = existingColumn.Description,
                    DataTypeId = existingColumn.DataTypeId,
                    ColumnTypeId = existingColumn.ColumnTypeId,
                    DefaultValue = existingColumn.DefaultValue,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                //Vemos si existen rangos asociados
                if (existingColumn.Ranges != null && existingColumn.Ranges.Count > 0)
                {
                    foreach (var range in existingColumn.Ranges)
                    {
                        newColumn.Ranges.Add(new ColumnRange
                        {
                            RFrom = range.RFrom,
                            RTo = range.RTo,
                            DefaultValue = range.DefaultValue,
                            CreatedAt = now,
                            UpdatedAt = now
                        });
                    }

                }

                await _configColumnRepository.AddAsync(newColumn, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                await _configColumnRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Columna copiada exitosamente. OriginalId: {OriginalId}, NewId: {NewId}, RangesCount: {RangesCount}",
                    existingColumn.Id,
                    newColumn.Id,
                    newColumn.Ranges.Count);
                return Result<ConfigColumn>.Success(newColumn);

            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Copia de columna cancelada. ID: {Id}", command.Id);
                return Result<ConfigColumn>.Failure("Operación cancelada");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al copiar columna con ID: {Id}", command.Id);
                return Result<ConfigColumn>.Failure($"Error al copiar la columna: {ex.Message}");
            }
        }
    }
}
