using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class CreateHistProcessCommandHandler : ICommandHandler<CreateHistProcessCommand, Result<HistProcess>>
    {
        private readonly IConfigTemplateRepository _configTemplateRepository;
        private readonly IHistProcessRepository _histProcessRepository;
        private readonly ILogger<CreateHistProcessCommandHandler> _logger;

        public CreateHistProcessCommandHandler(IConfigTemplateRepository configTemplateRepository,
            IHistProcessRepository histProcessRepository,
            ILogger<CreateHistProcessCommandHandler> logger
            )
        {
            _configTemplateRepository = configTemplateRepository ?? throw new ArgumentNullException(nameof(configTemplateRepository));
            _histProcessRepository = histProcessRepository ?? throw new ArgumentNullException(nameof(histProcessRepository));
            _logger = logger;
        }

        public async Task<Result<HistProcess>> HandleAsync(CreateHistProcessCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                _logger.LogError($"El comando {nameof(command)}, es nulo");
                return Result<HistProcess>.Failure($"El comando {nameof(command)}, no puede ser nulo.");
            }

            var template = await _configTemplateRepository.GetByIdAsync(command.ConfigTemplateId);
            if (template == null)
            {
                _logger.LogError($"El ID {command.ConfigTemplateId} de la plantilla no existe.");
                return Result<HistProcess>.Failure($"El ID {command.ConfigTemplateId} de la plantilla no existe.");
            }

            if (string.IsNullOrWhiteSpace(command.FileSource) || string.IsNullOrWhiteSpace(command.FinalStatus))
            {
                _logger.LogError("Debe especificar al archivo a procesar  y el de salida.");
                return Result<HistProcess>.Failure("Debe especificar al archivo a procesar  y el de salida.");
            }

            if (string.IsNullOrWhiteSpace(command.FinalStatus))
            {
                _logger.LogError("Debe especificar el estado final del proceso.");
                return Result<HistProcess>.Failure("Debe especificar el estado final del proceso.");
            }


            var now = DateTime.UtcNow;
            var newHistProcess = new HistProcess
            {
                ConfigTemplateId = command.ConfigTemplateId,
                DataProcess = command.DataProcess ?? string.Empty,
                FileSource = command.FileSource?.Trim(),
                FileTarget = command.FileTarget?.Trim(),
                FinalStatus = command.FinalStatus?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
            cancellationToken.ThrowIfCancellationRequested();
            await _histProcessRepository.AddAsync(newHistProcess);
            await _histProcessRepository.SaveChangesAsync();
            cancellationToken.ThrowIfCancellationRequested();

            return Result<HistProcess>.Success(newHistProcess);
        }
    }
}
