using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class DeleteHistProcessCommandHandler : ICommandHandler<DeleteHistProcessCommand, Result<bool>>
    {
        private readonly IHistProcessRepository _histProcessRepository;
        private readonly ILogger<DeleteHistProcessCommandHandler> _logger;

        public DeleteHistProcessCommandHandler(
            IHistProcessRepository histProcessRepository,
            ILogger<DeleteHistProcessCommandHandler> logger)
        {
            _histProcessRepository = histProcessRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> HandleAsync(DeleteHistProcessCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                _logger.LogError($"El comando {nameof(command)}, no puede ser nulo.");
                return Result<bool>.Failure($"El comando {nameof(command)}, es nulo.");
            }
            try
            {
                var histProcess = await _histProcessRepository.GetByIdAsync(command.Id, cancellationToken);
                if (histProcess == null)
                {
                    _logger.LogError($"El historico Id {command.Id}, no existe.");
                    return Result<bool>.Failure($"El historico Id {command.Id}, no existe.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                await _histProcessRepository.DeleteAsync(histProcess, cancellationToken);
                var changes = await _histProcessRepository.SaveChangesAsync(cancellationToken);
                if (changes <= 0)
                {
                    _logger.LogError($"No se pudo eliminar el historico Id {command.Id}.");
                    return Result<bool>.Failure($"No se pudo eliminar el historico Id {command.Id}.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar el historico Id {command.Id}: {ex.Message}");
                return Result<bool>.Failure($"Error al eliminar el historico Id {command.Id}: {ex.Message}");
            }
        }
    }
}
