
using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class CreateColumnRangeCommandHandler : ICommandHandler<CreateColumnRangeCommand, Result<ColumnRange>>
    {
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly IColumnRangeRepository _columnRangeRepository;
        private readonly ILogger<CreateColumnRangeCommandHandler> _logger;


        public CreateColumnRangeCommandHandler(
            IConfigColumnRepository configColumnRepository,
            IColumnRangeRepository columnRangeRepository,
            ILogger<CreateColumnRangeCommandHandler> logger)
        {
            _configColumnRepository = configColumnRepository;
            _columnRangeRepository = columnRangeRepository;
            _logger = logger;
        }


        public async Task<Result<ColumnRange>> HandleAsync(CreateColumnRangeCommand command, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (command is null)
            {
                _logger.LogWarning($"{nameof(command)}, valor de comando nulo");
                return Result<ColumnRange>.Failure($"{nameof(command)} no puede ser null.");
            }

            var column = await _configColumnRepository.GetByIdAsync(command.ConfigColumnId, cancellationToken);
            if (column is null)
            {
                _logger.LogWarning("Intento de crear un rango para una columna inexistente. ConfigColumnId: {ConfigColumnId}", command.ConfigColumnId);
                return Result<ColumnRange>.Failure($"Intento de crear un rango para una columna inexistente: {command.ConfigColumnId}");
            }

            if (string.IsNullOrWhiteSpace(command.RFrom) && string.IsNullOrWhiteSpace(command.RTo))
            {
                _logger.LogWarning("Intento de crear un rango sin valores RFrom o RTo.");
                return Result<ColumnRange>.Failure("Debe especificar al menos un valor para RFrom o RTo.");
            }

            var now = DateTime.UtcNow;
            var newRange = new ColumnRange
            {
                ConfigColumnId = command.ConfigColumnId,
                DefaultValue = command.DefaultValue,
                RFrom = command.RFrom,
                RTo = command.RTo,
                CreatedAt = now,
                UpdatedAt = now
            };

            cancellationToken.ThrowIfCancellationRequested();

            await _columnRangeRepository.AddAsync(newRange, cancellationToken);
            await _columnRangeRepository.SaveChangesAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            return Result<ColumnRange>.Success(newRange);
        }
    }
}
