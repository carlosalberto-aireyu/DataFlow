using DataFlow.Core.Models;
using Microsoft.Extensions.Logging;
using DataFlow.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataFlow.Core.Common;

namespace DataFlow.Core.Features.Queries.Handlers
{
    public class GetAllParametrosQueryHandler : IQueryHandler<GetAllParametrosQuery, Result<IReadOnlyList<Parametro>>>
    {
        private readonly IParametroRepository _parametrosRepository;
        private readonly ILogger<GetAllParametrosQueryHandler> _logger;
        public GetAllParametrosQueryHandler(IParametroRepository parametrosRepository, ILogger<GetAllParametrosQueryHandler> logger)
        {
            _parametrosRepository = parametrosRepository ?? throw new ArgumentNullException(nameof(parametrosRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        public async Task<Result<IReadOnlyList<Parametro>>> HandleAsync(GetAllParametrosQuery query, CancellationToken cancellationToken = default)
        {
            try 
            {
                _logger.LogInformation("Recuperando todos los Parametro");
                var parametro = await _parametrosRepository.GetAllAsync(cancellationToken);

                return Result<IReadOnlyList<Parametro>>.Success(parametro);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al recuperar los Parametro GetAllParametrosQuery");
                return Result<IReadOnlyList<Parametro>>.Failure("Error al recuperar todos los Parametro.");
            }
        }
    }
}
