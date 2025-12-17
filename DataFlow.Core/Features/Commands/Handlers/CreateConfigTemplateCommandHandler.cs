using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;


namespace DataFlow.Core.Features.Commands.Handlers
{
    public class CreateConfigTemplateCommandHandler : ICommandHandler<CreateConfigTemplateCommand, Result<ConfigTemplate>>
    {
        private readonly IConfigTemplateRepository _repo;
        private readonly ILogger<CreateConfigTemplateCommandHandler> _logger;

        public CreateConfigTemplateCommandHandler(IConfigTemplateRepository repo,
            ILogger<CreateConfigTemplateCommandHandler> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Result<ConfigTemplate>> HandleAsync(CreateConfigTemplateCommand cmd, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (cmd is null)
            {
                _logger.LogError($"comando {nameof(cmd)}, es nulo.");
                return Result<ConfigTemplate>.Failure($"El comando {nameof(cmd)}, no puede ser nulo.");
            }
            var now = DateTime.UtcNow;

            var configTemplate = new ConfigTemplate
            {
                Description = cmd.Description,
                CreatedAt = now,
                UpdatedAt = now
            };

            cancellationToken.ThrowIfCancellationRequested();
            await _repo.AddAsync(configTemplate, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            return Result<ConfigTemplate>.Success(configTemplate);
        }
    }
}
