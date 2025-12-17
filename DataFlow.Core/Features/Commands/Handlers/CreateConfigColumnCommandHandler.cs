using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class CreateConfigColumnCommandHandler : ICommandHandler<CreateConfigColumnCommand, Result<ConfigColumn>>
    {
        private readonly IConfigTemplateRepository _templateRepository;
        private readonly IConfigColumnRepository _columnRepository;
        private readonly ILogger<CreateConfigColumnCommandHandler> _logger;

        public CreateConfigColumnCommandHandler(
            IConfigTemplateRepository templateRepository,
            IConfigColumnRepository columnRepository,
            ILogger<CreateConfigColumnCommandHandler> logger
            )
        {
            _templateRepository = templateRepository;
            _columnRepository = columnRepository;
            _logger = logger;
        }

        public async Task<Result<ConfigColumn>> HandleAsync(CreateConfigColumnCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command == null)
            {
                _logger.LogError($"CreateConfigColumnCommand is null. {nameof(command)}");
                return Result<ConfigColumn>.Failure($"Comando no valido. {nameof(command)}");
            }

            if(command.IndexColumn < 0)
            {
                _logger.LogWarning("Intento de crear una columna con indice invalido.");
                return Result<ConfigColumn>.Failure("El indice de la columna es requerido y debe ser mayor o igual a cero.");
            }



            if (string.IsNullOrWhiteSpace(command.Name))
            {
                _logger.LogWarning("Intento de crear una columna sin nombre.");
                return Result<ConfigColumn>.Failure("El nombre de la columna es requerido.");
            }

            if (string.IsNullOrWhiteSpace(command.NameDisplay))
            {
                _logger.LogWarning("Intento de crear una columna sin nombre para mostrar.");
                return Result<ConfigColumn>.Failure("El nombre para mostrar de la columna es requerido.");
            }

            if (command.DataTypeId <= 0)
            {
                _logger.LogWarning("Intento de crear una columna sin tipo de dato.");
                return Result<ConfigColumn>.Failure("El tipo de dato de la columna es requerido.");
            }

            
            if (command.ColumnTypeId <= 0)
            {
                _logger.LogWarning("Intento de crear una columna sin tipo de columna.");
                return Result<ConfigColumn>.Failure("El tipo de columna es requerido.");
            }
            var template = await _templateRepository.GetByIdAsync(command.ConfigTemplateId, cancellationToken);
            if (template == null)
            {
                _logger.LogError("Intento de crear una columna para una plantilla de configuracion inexistente. ConfigTemplateId: {ConfigTemplateId}", command.ConfigTemplateId);
                return Result<ConfigColumn>.Failure($"Plantilla de configuracion no encontrada: {command.ConfigTemplateId}");

            }

            var now = DateTime.UtcNow;
            var column = new ConfigColumn
            {
                ConfigTemplateId = template.Id,
                IndexColumn = command.IndexColumn,
                Name = command.Name,
                NameDisplay = command.NameDisplay,
                Description = command.Description,
                DataTypeId = command.DataTypeId,
                DefaultValue = command.DefaultValue,
                ColumnTypeId = command.ColumnTypeId,
                CreatedAt = now,
                UpdatedAt = now
            };
            cancellationToken.ThrowIfCancellationRequested();

            await _columnRepository.AddAsync(column, cancellationToken);
            await _templateRepository.SaveChangesAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return Result<ConfigColumn>.Success(column);
        }
    }
}
