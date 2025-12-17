using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries.Handlers
{
    public class GetParametroByKeyQueryHandler : IQueryHandler<GetParametroByKeyQuery, Result<Parametro?>>
    {
        private readonly IParametroRepository _parametroRepository;
        private readonly ILogger<GetParametroByKeyQueryHandler> _logger;
        public GetParametroByKeyQueryHandler(IParametroRepository parametroRepository, ILogger<GetParametroByKeyQueryHandler> logger)
        {
            _parametroRepository = parametroRepository ?? throw new ArgumentNullException(nameof(parametroRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<Parametro?>> HandleAsync(GetParametroByKeyQuery query, CancellationToken cancellationToken = default)
        {
            if(query == null)
            {
                _logger.LogError("la consulta es null");
                return Result<Parametro?>.Failure("GetParametroByKeyQuery is null");
            }
            if(string.IsNullOrWhiteSpace(query.ParametroKey))
            {
                _logger.LogError("El ID del parametro es necesario");
                return Result<Parametro?>.Failure("ParametroKey us null");
            }
            try
            {
                var parametro = await _parametroRepository.GetByKeyAsync(query.ParametroKey, cancellationToken);
                if(parametro == null)
                {
                    _logger.LogWarning("No se encontro el parametro con ID: {ParametroKey}", query.ParametroKey);
                    return Result<Parametro?>.Failure($"Parametro con ID {query.ParametroKey} no existe");
                }
                return Result<Parametro?>.Success(parametro);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error al obtener el parametro con key: {ParametroKey}", query.ParametroKey);
                return Result<Parametro?>.Failure("Error retrieving parametro");
            }

        }
    }
}
