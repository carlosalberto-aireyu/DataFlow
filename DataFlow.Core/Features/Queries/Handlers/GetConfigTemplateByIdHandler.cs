
using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;


namespace DataFlow.Core.Features.Queries.Handlers
{
    public class GetConfigTemplateByIdQueryHandler : IQueryHandler<GetConfigTemplateByIdQuery, Result<ConfigTemplate>>
    {
        private readonly IConfigTemplateRepository _repo;
        private readonly ILogger<GetConfigTemplateByIdQueryHandler> _logger;
        public GetConfigTemplateByIdQueryHandler(IConfigTemplateRepository repo,
            ILogger<GetConfigTemplateByIdQueryHandler> logger
            )
        {
            _repo = repo;
            _logger = logger;
        }

        public async Task<Result<ConfigTemplate>> HandleAsync(GetConfigTemplateByIdQuery query, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (query == null)
            {
                _logger.LogError("La consulta {QueryName} es nula.", nameof(query));
                return Result<ConfigTemplate>.Failure("La consulta no puede ser nula.");
            }

            try
            {
                var template = await _repo.GetWithDetailAsync(query.Id, cancellationToken);
                if (template is null)
                {
                    _logger.LogWarning("No se encontró la plantilla de configuración con Id {Id}", query.Id);
                    return Result<ConfigTemplate>.Failure("Plantilla de configuración no encontrada.");

                }
                cancellationToken.ThrowIfCancellationRequested();
                return Result<ConfigTemplate>.Success(template);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la plantilla de configuración con Id {Id}", query.Id);
                return Result<ConfigTemplate>.Failure("Error al obtener la plantilla de configuración.");
            }
        }
    }
}
