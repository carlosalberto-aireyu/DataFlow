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
    public class GetConfigColumnByIdQueryHandler : IQueryHandler<GetConfigColumnByIdQuery, Result<ConfigColumn>>
    {
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly ILogger<GetConfigColumnsByTemplateIdQueryHandler> _logger;

        public GetConfigColumnByIdQueryHandler(
            IConfigColumnRepository configColumnRepository, 
            ILogger<GetConfigColumnsByTemplateIdQueryHandler> logger)
        {
            _configColumnRepository = configColumnRepository;
            _logger = logger;
        }


        public async Task<Result<ConfigColumn>> HandleAsync(GetConfigColumnByIdQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                _logger.LogError("GetConfigColumnByIdQuery es null");
                return Result<ConfigColumn>.Failure("La consulta no puede ser nula");
            }
            try
            {
                var configColumn = await _configColumnRepository.GetWithDetailAsync(query.Id, cancellationToken);
                if (configColumn == null)
                {
                    _logger.LogWarning("No se encontró la columna de configuración con Id {Id}", query.Id);
                    return Result<ConfigColumn>.Failure($"No se encontró la columna de configuración con Id {query.Id}");
                }
                
                
                return Result<ConfigColumn>.Success(configColumn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la columna de configuración con Id {Id}", query.Id);
                return Result<ConfigColumn>.Failure("Ocurrió un error al procesar la consulta");

            }

            
        }
    }
}
