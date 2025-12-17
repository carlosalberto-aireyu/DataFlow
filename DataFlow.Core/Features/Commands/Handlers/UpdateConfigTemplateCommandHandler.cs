using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class UpdateConfigTemplateCommandHandler : ICommandHandler<UpdateConfigTemplateCommand, Result<ConfigTemplate>>
    {
        private readonly IConfigTemplateRepository _templateRepository;
        private readonly ILogger<UpdateConfigTemplateCommandHandler> _logger;

        public UpdateConfigTemplateCommandHandler(IConfigTemplateRepository templateRepository,
            ILogger<UpdateConfigTemplateCommandHandler> logger)
        {
            _templateRepository = templateRepository;
            _logger = logger;
        }

        public async Task<Result<ConfigTemplate>> HandleAsync(UpdateConfigTemplateCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command is null)
            {
                _logger.LogWarning($"{nameof(command)}, valor de comando nulo");
                return Result<ConfigTemplate>.Failure($"{nameof(command)} no puede ser nulo.");
            }

            try
            {
                var template = await _templateRepository.GetByIdAsync(command.Id, cancellationToken);
                if (template is null)
                {
                    _logger.LogWarning("Intento de actualizar una plantilla de configuracion inexistente. Id: {ConfigTemplateId}", command.Id);
                    return Result<ConfigTemplate>.Failure($"Plantilla de configuracion con Id {command.Id}, no existe.");
                }

                var now = DateTime.UtcNow;

                cancellationToken.ThrowIfCancellationRequested();

                template.Description = command.Description;
                template.UpdatedAt = now;

                var change = await _templateRepository.SaveChangesAsync(cancellationToken);
                if (change <= 0)
                {
                    _logger.LogError("No se pudieron guardar los cambios al actualizar la plantilla de configuración con Id: {Id}", command.Id);
                    return Result<ConfigTemplate>.Failure($"No se pudieron guardar los cambios al actualizar la plantilla de configuración con Id: {command.Id}");
                }
                cancellationToken.ThrowIfCancellationRequested();
                return Result<ConfigTemplate>.Success(template);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la plantilla de configuración con Id: {Id}", command.Id);
                return Result<ConfigTemplate>.Failure($"Error al actualizar la plantilla de configuración: {ex.Message}");
            }
        }
    }
}
