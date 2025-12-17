using DataFlow.Core.Common;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DataFlow.Core.Features.Queries.Handlers
{
    public class GetHistProcessByConfigTemplateIdQueryHandler : IQueryHandler<GetHistProcessByConfigTemplateIdQuery, Result<IReadOnlyList<HistProcess>>>
    {
        private readonly IConfigTemplateRepository _configTemplateRepository;
        private readonly IHistProcessRepository _histProcessRepository;
        private readonly ILogger<GetHistProcessByConfigTemplateIdQueryHandler> _logger;
        public GetHistProcessByConfigTemplateIdQueryHandler(
            IConfigTemplateRepository configTemplateRepository,
            IHistProcessRepository histProcessRepository,
            ILogger<GetHistProcessByConfigTemplateIdQueryHandler> logger)
        {
            _configTemplateRepository = configTemplateRepository ?? throw new ArgumentNullException(nameof(configTemplateRepository));
            _histProcessRepository = histProcessRepository ?? throw new ArgumentNullException(nameof(histProcessRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<IReadOnlyList<HistProcess>>> HandleAsync(GetHistProcessByConfigTemplateIdQuery query, CancellationToken cancellationToken = default)
        {
            if (query == null)
            {
                _logger.LogError("GetHistProcessByConfigTemplateIdQuery es nulo");
                return Result<IReadOnlyList<HistProcess>>.Failure("El query no puede ser nulo");
            }
            if (query.ConfigTemplateId <= 0)
            {
                _logger.LogError("El Id de la plantilla proporcionado no es valido: {ConfigTemplateId}", query.ConfigTemplateId);
                return Result<IReadOnlyList<HistProcess>>.Failure("El ID de la Plantilla no es valido");
            }
            try
            {
                var plantilla = await _configTemplateRepository.GetByIdAsync(query.ConfigTemplateId, cancellationToken);
                if (plantilla == null)
                {
                    _logger.LogError("No se encontro la plantilla con Id: {ConfigTemplateId}", query.ConfigTemplateId);
                    return Result<IReadOnlyList<HistProcess>>.Failure("No se encontro la plantilla solicitada");
                }

                var histProcesses = await _histProcessRepository.GetByConfigTemplateIdAsync(query.ConfigTemplateId, cancellationToken);
                if (histProcesses == null)
                {
                    _logger.LogError("No se encontraron procesos historicos para la plantilla con Id: {ConfigTemplateId}", query.ConfigTemplateId);
                    return Result<IReadOnlyList<HistProcess>>.Failure($"Error al obtener los procesos historicos para la plantilla con Id: {query.ConfigTemplateId}");
                }
                return Result<IReadOnlyList<HistProcess>>.Success(histProcesses);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener los procesos historicos para la plantilla con GetHistProcessByConfigTemplateIdQuery: {ConfigTemplateId}", query.ConfigTemplateId);
                return Result<IReadOnlyList<HistProcess>>.Failure("Ocurrio un error inesperado al procesar la solicitud: GetHistProcessByConfigTemplateIdQuery");
            }
        }
    }
}
