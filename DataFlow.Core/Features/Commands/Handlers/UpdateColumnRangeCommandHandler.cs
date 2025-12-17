using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class UpdateColumnRangeCommandHandler : ICommandHandler<UpdateColumnRangeCommand, Result<ColumnRange>>
    {
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly IColumnRangeRepository _columnRangeRepository;
        private readonly ILogger<UpdateColumnRangeCommandHandler> _logger;

        public UpdateColumnRangeCommandHandler(IConfigColumnRepository configColumnRepository,
            IColumnRangeRepository columnRangeRepository,
            ILogger<UpdateColumnRangeCommandHandler> logger
            )
        {
            _configColumnRepository = configColumnRepository;
            _columnRangeRepository = columnRangeRepository;
            _logger = logger;
        }

        public async Task<Result<ColumnRange>> HandleAsync(UpdateColumnRangeCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                _logger.LogError($"El comando {nameof(command)}, es nulo");
                return Result<ColumnRange>.Failure($"El comando {nameof(command)}, no puede ser nulo.");
            }
            try
            {
                var range = await _columnRangeRepository.GetByIdAsync(command.Id, cancellationToken);
                if (range == null)
                {
                    _logger.LogError($"El rango con Id {command.Id} no existe.");
                    return Result<ColumnRange>.Failure($"El rango con Id {command.Id} no existe.");
                }

                if (string.IsNullOrWhiteSpace(command.RFrom) || string.IsNullOrWhiteSpace(command.RTo))
                {
                    _logger.LogError("Debe especificar un rango valido.");
                    return Result<ColumnRange>.Failure("Debe especificar un rango valido.");
                }

                if (command.RFrom != null)
                    range.RFrom = command.RFrom.Trim();

                if (command.RTo != null)
                    range.RTo = command.RTo.Trim();
                if (command.DefaultValue != null)
                    range.DefaultValue = command.DefaultValue.Trim();

                range.UpdatedAt = DateTime.UtcNow;

                cancellationToken.ThrowIfCancellationRequested();
                await _columnRangeRepository.UpdateAsync(range);
                await _columnRangeRepository.SaveChangesAsync();
                cancellationToken.ThrowIfCancellationRequested();
                return Result<ColumnRange>.Success(range);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el rango de columna.");
                return Result<ColumnRange>.Failure($"Error al actualizar el rango de columna: {ex.Message}");
            }


        }
    }
}
