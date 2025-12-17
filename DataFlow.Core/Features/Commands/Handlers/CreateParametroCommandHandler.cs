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
    public class CreateParametroCommandHandler : ICommandHandler<CreateParametroCommand, Result<Parametro>>
    {
        private readonly IParametroRepository _parametroRepository;
        private readonly ILogger<CreateParametroCommandHandler> _logger;
        public CreateParametroCommandHandler(IParametroRepository parametroRepository, 
            ILogger<CreateParametroCommandHandler> logger)
        {
            _parametroRepository = parametroRepository ?? throw new ArgumentNullException(nameof(parametroRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Parametro>> HandleAsync(CreateParametroCommand command, CancellationToken cancellationToken = default)
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
            var parametroEncontrado = await _parametroRepository.GetByKeyAsync(command.ParametroKey);
            if (parametroEncontrado != null)
            {
                _logger.LogError("El parametro con ID {ParametroKey} ya existe", command.ParametroKey);
                return Result<Parametro>.Failure($"El parametro con ID {command.ParametroKey} ya existe");
            }
            var now = DateTime.UtcNow;
            try
            {
                var parameter = new Parametro
                {
                    ParametroKey = command.ParametroKey,
                    Name = command.Name,
                    ParametroValue = command.ParametroValue,
                    Description = command.Description,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                cancellationToken.ThrowIfCancellationRequested();
                await _parametroRepository.AddAsync(parameter, cancellationToken);
                await _parametroRepository.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Parámetro del sistema creado exitosamente: {ParameterKey}", parameter.ParametroKey);
                return Result<Parametro>.Success(parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el parámetro del sistema");
                return Result<Parametro>.Failure("Error al crear el parámetro del sistema: " + ex.Message);
            }
        }
    }
}
