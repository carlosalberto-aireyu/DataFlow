using DataFlow.Core.Common;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class DeleteConfigTemplateCommandHandler : ICommandHandler<DeleteConfigTemplateCommand, Result<bool>>
    {
        private readonly IConfigTemplateRepository _templateRepository;
        private readonly ILogger<DeleteConfigTemplateCommandHandler> _logger;

        public DeleteConfigTemplateCommandHandler(
            IConfigTemplateRepository templateRepository,
            ILogger<DeleteConfigTemplateCommandHandler> logger)
        {
            _templateRepository = templateRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> HandleAsync(DeleteConfigTemplateCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                _logger.LogError($"El comando {nameof(command)}, es nulo");
                return Result<bool>.Failure($"El comando {nameof(command)}, no puede ser nulo.");
            }

            try
            {
                var template = await _templateRepository.GetByIdAsync(command.Id);
                if (template == null)
                {
                    _logger.LogError($"El Id de la plantilla de configuración {command.Id} no existe.");
                    return Result<bool>.Failure($"El Id de la plantilla de configuración {command.Id} no existe.");
                }
                cancellationToken.ThrowIfCancellationRequested();
                await _templateRepository.DeleteAsync(template, cancellationToken);
                var changes = await _templateRepository.SaveChangesAsync(cancellationToken);
                if (changes <= 0)
                {
                    _logger.LogError($"No se pudieron guardar los cambios al eliminar la plantilla de configuración con Id {command.Id}");
                    return Result<bool>.Failure($"No se pudieron guardar los cambios al eliminar la plantilla de configuración con Id {command.Id}");
                }
                cancellationToken.ThrowIfCancellationRequested();

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar la plantilla de configuración con Id {command.Id}");
                return Result<bool>.Failure($"Error al eliminar la plantilla de configuración: {ex.Message}");
            }
        }
    }
}
