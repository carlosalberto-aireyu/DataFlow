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
    public class SetParametroByKeyCommandHandler : ICommandHandler<SetParametroByKeyCommand, Result<Parametro>>
    {
        private readonly IParametroRepository _parametroRepository;
        private readonly ILogger<SetParametroByKeyCommandHandler> _logger;
        public SetParametroByKeyCommandHandler(IParametroRepository parametroRepository, ILogger<SetParametroByKeyCommandHandler> logger)
        {
            _parametroRepository = parametroRepository ?? throw new ArgumentNullException(nameof(parametroRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<Result<Parametro>> HandleAsync(SetParametroByKeyCommand command, CancellationToken cancellationToken = default)
        {
            if(command == null)
            {
                _logger.LogError("SetParametroByKey el comando es null");
                return Result<Parametro>.Failure("SetParametroByKey el comando es null");
            }
            try
            {
                var parametro = await _parametroRepository.GetByKeyAsync(command.ParametroKey, cancellationToken);
                if(parametro == null)
                {
                    parametro = new Parametro
                    {
                        ParametroKey = command.ParametroKey,
                        ParametroValue = command.ParametroValue,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _parametroRepository.AddAsync(parametro, cancellationToken);
                } 
                else
                {
                    parametro.ParametroValue = command.ParametroValue;
                    parametro.UpdatedAt = DateTime.UtcNow;
                    await _parametroRepository.UpdateAsync(parametro, cancellationToken);
                }
                await _parametroRepository.SaveChangesAsync(cancellationToken);
                return Result<Parametro>.Success(parametro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar SetParametroByKey");
                return Result<Parametro>.Failure($"Error al ejecutar SetParametroByKey: {ex.Message}");
            }
        }
    }
}
