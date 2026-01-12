using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Features.Queries.Handlers
{
    public class GetAllConfigTemplatesQueryHandler : IQueryHandler<GetAllConfigTemplatesQuery, Result<IReadOnlyList<ConfigTemplate>>>
    {
        private readonly IConfigTemplateRepository _configTemplateRepository;
        private readonly ILogger<GetAllConfigTemplatesQueryHandler> _logger;

        public GetAllConfigTemplatesQueryHandler(IConfigTemplateRepository configTemplateRepository, ILogger<GetAllConfigTemplatesQueryHandler> logger)
        {
            _configTemplateRepository = configTemplateRepository;
            _logger = logger;
        }

        public async Task<Result<IReadOnlyList<ConfigTemplate>>> HandleAsync(GetAllConfigTemplatesQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Obteniendo todas las plantillas de configuración.");
                var templates = await _configTemplateRepository.GetAllAsync(cancellationToken);
                if (templates is null || templates.Count <= 0)
                {
                    _logger.LogWarning("No se encontraron plantillas de configuración.");
                    return Result<IReadOnlyList<ConfigTemplate>>.Success(templates ?? new List<ConfigTemplate>());
                }
                
                var sorted = templates.OrderByDescending(t => t.CreatedAt).ToList();
                

                _logger.LogInformation("Se obtuvieron {Count} plantillas", templates.Count);
                return Result<IReadOnlyList<ConfigTemplate>>.Success(sorted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todas las plantillas");
                return Result<IReadOnlyList<ConfigTemplate>>.Failure($"Error al cargar plantillas: {ex.Message}");
            }
        }
    }
}
