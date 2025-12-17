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
    public class GetHistProcessByIdQueryHandler : IQueryHandler<GetHistProcessByIdQuery, Result<HistProcess>>
    {
        private readonly IHistProcessRepository _repository;

        private readonly ILogger _logger;
        public GetHistProcessByIdQueryHandler(IHistProcessRepository repository, ILogger logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<HistProcess>> HandleAsync(GetHistProcessByIdQuery query, CancellationToken cancellationToken = default)
        {
            if(query == null)
            {
                _logger.LogWarning("La consulta es nula");
                return Result<HistProcess>.Failure("La consulta no puede ser nula");
            }
            try
            {
                var found = await _repository.GetByIdAsync(query.Id, cancellationToken);
                if (found == null)
                {
                    _logger.LogWarning($"No existe el Id:{query.Id}");
                    return Result<HistProcess>.Failure($"No existe el Id:{query.Id}");
                }

                return Result<HistProcess>.Success(found);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,"Ha ocurrido un error tratando de obtener el registro GetHistProcessByIdQuery");
                return Result<HistProcess>.Failure("Ha ocurrido un error tratando de obtener el registro GetHistProcessByIdQuery");
            }
        }
    }
}
