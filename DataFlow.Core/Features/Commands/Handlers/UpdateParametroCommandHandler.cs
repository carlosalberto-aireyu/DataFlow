using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands.Handlers
{
    public class UpdateParametroCommandHandler : ICommandHandler<UpdateParametroCommand, Result<Parametro>>
    {
        private readonly IParametroRepository _parametroRepository;
        private readonly ILogger<UpdateParametroCommandHandler> _logger;
        public UpdateParametroCommandHandler(IParametroRepository parametroRepository, ILogger<UpdateParametroCommandHandler> logger)
        {
            _parametroRepository = parametroRepository ?? throw new ArgumentNullException(nameof(parametroRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<Result<Parametro>> HandleAsync(UpdateParametroCommand command, CancellationToken cancellationToken = default)
        {
            if (command == null)
            {
                _logger.LogError("CreateParametroCommand es null");
                return Result<Parametro>.Failure("El comando es nulo");
            }
            if (string.IsNullOrWhiteSpace(command.ParametroKey))
            {
                _logger.LogError("El ID del parametro es nulo o vacio");
                return Result<Parametro>.Failure("El ID del parametro es nulo o vacio");
            }
            if (string.IsNullOrWhiteSpace(command.ParametroValue))
            {
                _logger.LogError("El valor del parametro es nulo o vacio");
                return Result<Parametro>.Failure("El valor del parametro es nulo o vacio");
            }
            var parametro= await _parametroRepository.GetByKeyAsync(command.ParametroKey);
            if (parametro is null)
            {
                _logger.LogError("El parametro con ID {ParametroKey} no existe", command.ParametroKey);
                return Result<Parametro>.Failure($"El parametro con ID {command.ParametroKey} no existe");
            }
            parametro.ParametroKey = command.ParametroKey;
            parametro.ParametroValue = command.ParametroValue;
            parametro.Description = command.Description;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _parametroRepository.UpdateAsync(parametro);
                await _parametroRepository.SaveChangesAsync();

                _logger.LogInformation("Parametro con ID {ParametroKey} actualizado exitosamente", command.ParametroKey);
                return Result<Parametro>.Success(parametro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar el parametro con ID {ParametroKey}", command.ParametroKey);
                return Result<Parametro>.Failure($"Error al actualizar el parametro: {ex.Message}");
            }
        }
    }
}
