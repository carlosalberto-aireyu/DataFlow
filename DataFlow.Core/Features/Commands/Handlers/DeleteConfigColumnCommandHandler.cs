using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class DeleteConfigColumnCommandHandler : ICommandHandler<DeleteConfigColumnCommand, Result<bool>>
    {
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly IColumnRangeRepository _rangeRepository;
        private readonly ILogger<DeleteConfigColumnCommandHandler> _logger;

        public DeleteConfigColumnCommandHandler(
            IConfigColumnRepository configColumnRepository,
            IColumnRangeRepository rangeRepository,
            ILogger<DeleteConfigColumnCommandHandler> logger)
        {
            _configColumnRepository = configColumnRepository;
            _rangeRepository = rangeRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> HandleAsync(DeleteConfigColumnCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                _logger.LogError("El comando no puede ser nulo. ({CommandName})", nameof(command));
                return Result<bool>.Failure($"El comando no puede ser nulo. ({nameof(command)})");
            }

            try
            {
                var configColumn = await _configColumnRepository.GetWithDetailAsync(command.Id);
                if (configColumn == null)
                {
                    _logger.LogError($"La columna con el Id {command.Id} no existe.");
                    return Result<bool>.Failure($"La columna con el Id {command.Id} no existe.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                await _configColumnRepository.DeleteAsync(configColumn, cancellationToken);
                var changes = await _configColumnRepository.SaveChangesAsync(cancellationToken);
                if (changes <= 0)
                {
                    _logger.LogError($"No se pudieron guardar los cambios al eliminar la columna con Id {command.Id}.");
                    return Result<bool>.Failure($"No se pudieron guardar los cambios al eliminar la columna con Id {command.Id}.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la columna con Id {command.Id}: {ex.Message}");
                return Result<bool>.Failure($"Error al eliminar la columna con Id {command.Id}: {ex.Message}");
            }
        }
    }
}
