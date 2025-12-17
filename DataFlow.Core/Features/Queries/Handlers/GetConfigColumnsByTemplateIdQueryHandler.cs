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
    public class GetConfigColumnsByTemplateIdQueryHandler : IQueryHandler<GetConfigColumnsByTemplateIdQuery, Result<IReadOnlyList<ConfigColumn>>>
    {
        private readonly IConfigColumnRepository _configColumnRepository;
        private readonly IConfigTemplateRepository _configTemplateRepository;
        private readonly ILogger<GetConfigColumnsByTemplateIdQueryHandler> _logger;

        public GetConfigColumnsByTemplateIdQueryHandler(
            IConfigColumnRepository configColumnRepository,
            IConfigTemplateRepository configTemplateRepository,
            ILogger<GetConfigColumnsByTemplateIdQueryHandler> logger
            )
        {
            _configColumnRepository = configColumnRepository;
            _configTemplateRepository = configTemplateRepository;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<ConfigColumn>>> HandleAsync(GetConfigColumnsByTemplateIdQuery query, CancellationToken cancellationToken = default)
        {
            if(query == null)
            {
                _logger.LogError("GetConfigColumnsByTemplateIdQuery is null");
                return Result<IReadOnlyList<ConfigColumn>>.Failure("La consulta no puede ser nula");
            }

            var column = await _configTemplateRepository
                .GetByIdAsync(query.ConfigTemplateId);
            if(column is null)
            {
                _logger.LogError("ConfigTemplate with Id {ConfigTemplateId} not found", query.ConfigTemplateId);
                return Result<IReadOnlyList<ConfigColumn>>.Failure($"No se encontró la plantilla de configuración con Id {query.ConfigTemplateId}");
            }

            try
            {
                var configColumns = await _configColumnRepository
                    .GetByConfigTemplateIdAsync(query.ConfigTemplateId, cancellationToken);
                return Result<IReadOnlyList<ConfigColumn>>.Success(configColumns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar ConfigColumns por ConfigTemplateId {ConfigTemplateId}", query.ConfigTemplateId);
                return Result<IReadOnlyList<ConfigColumn>>.Failure("Error al obtener las columnas de configuración");

            }
        }
    }
}
