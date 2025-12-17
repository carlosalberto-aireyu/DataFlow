using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class DeleteColumnRangeCommandHandler : ICommandHandler<DeleteColumnRangeCommand, Result<bool>>
    {
        private readonly IColumnRangeRepository _columnRangeRepository;
        private readonly ILogger<DeleteColumnRangeCommandHandler> _logger;
        public DeleteColumnRangeCommandHandler(
            IColumnRangeRepository columnRangeRepository,
            ILogger<DeleteColumnRangeCommandHandler> logger)
        {
            _columnRangeRepository = columnRangeRepository;
            _logger = logger;
        }


        public async Task<Result<bool>> HandleAsync(DeleteColumnRangeCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                _logger.LogError($"El comando {nameof(command)}, es nulo");
                return Result<bool>.Failure($"El comando {nameof(command)}, no puede ser nulo.");
            }

            try
            {
                var columnRange = await _columnRangeRepository.GetByIdAsync(command.Id, cancellationToken);
                if (columnRange == null)
                {
                    _logger.LogError($"El Id del rango {command.Id} no existe.");
                    return Result<bool>.Failure($"El Id del rango {command.Id} no existe.");
                }

                cancellationToken.ThrowIfCancellationRequested();
                await _columnRangeRepository.DeleteAsync(columnRange, cancellationToken);
                var changes = await _columnRangeRepository.SaveChangesAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (changes <= 0)
                {
                    _logger.LogError("No se pudieron guardar los cambios al eliminar el rango de columna.");
                    return Result<bool>.Failure("No se pudieron guardar los cambios al eliminar el rango de columna.");
                }
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el rango de columna con Id: {Id}", command.Id);
                return Result<bool>.Failure($"Error al eliminar el rango de columna: {ex.Message}");
            }
        }
    }
}
