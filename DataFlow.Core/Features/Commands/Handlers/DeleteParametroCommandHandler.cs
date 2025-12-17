using DataFlow.Core.Common;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class DeleteParametroCommandHandler : ICommandHandler<DeleteParametroCommand, Result<bool>>
    {
        private readonly IParametroRepository _parametroRepository;
        private readonly ILogger<DeleteParametroCommandHandler> _logger;
        public DeleteParametroCommandHandler(IParametroRepository parametroRepository, ILogger<DeleteParametroCommandHandler> logger)
        {
            _parametroRepository = parametroRepository ?? throw new ArgumentNullException(nameof(parametroRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<Result<bool>> HandleAsync(DeleteParametroCommand command, CancellationToken cancellationToken = default)
        {
            if(command is null)
            {
                _logger.LogError("DeleteParametroCommand es  null");
                return Result<bool>.Failure("Command es null");
            }
            var toDelete = await _parametroRepository.GetByKeyAsync(command.ParametroKey, cancellationToken);
            if (toDelete is null)
            {
                _logger.LogWarning("No se encontro el parametro con ID {ParametroKey}", command.ParametroKey);
                return Result<bool>.Failure($"No se encontro el parametro con ID {command.ParametroKey}");
            }
            try
            {
                await _parametroRepository.DeleteAsync(toDelete, cancellationToken);
                await _parametroRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Parametro con key {ParametroKey} eliminado exitosamente", command.ParametroKey);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el parametro con key {ParametroKey}", command.ParametroKey);
                return Result<bool>.Failure($"Error al eliminar el parametro: {ex.Message}");
            }

        }
    }
}
