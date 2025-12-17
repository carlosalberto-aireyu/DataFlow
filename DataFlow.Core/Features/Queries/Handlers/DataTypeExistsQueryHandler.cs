using DataFlow.Core.Common;
using DataFlow.Core.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries.Handlers
{
    internal class DataTypeExistsQueryHandler : IQueryHandler<DataTypeExistsQuery, Result<bool>>
    {
        private readonly ILookupRepository _lookupRepository;
        private readonly ILogger<DataTypeExistsQueryHandler> _logger;
        public DataTypeExistsQueryHandler(ILookupRepository lookupRepository, ILogger<DataTypeExistsQueryHandler> logger)
        {
            _lookupRepository = lookupRepository;
            _logger = logger;
        }

        public async Task<Result<bool>> HandleAsync(DataTypeExistsQuery query, CancellationToken cancellationToken = default)
        {
            if(query is null)
            {
                _logger.LogWarning("DataTypeExistsQuery es nulo");
                return Result<bool>.Failure("DataTypeExistsQuery no debe ser nulo .");
            }
            if(query.DataTypeId <= 0)
            {
                _logger.LogWarning("DataTypeId inválido: {DataTypeId}", query.DataTypeId);
                return Result<bool>.Failure("DataTypeId no es valido.");
            }

            try 
            {
                var exists = await _lookupRepository.DataTypeExistsAsync(query.DataTypeId, cancellationToken);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error error al consultar el DataTypeId {DataTypeId}", query.DataTypeId);
                return Result<bool>.Failure("Error error al consultar el DataTypeId.");
            }
        }
    }
}
