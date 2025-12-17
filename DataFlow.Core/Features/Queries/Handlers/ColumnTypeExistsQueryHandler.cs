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
    public class ColumnTypeExistsQueryHandler : IQueryHandler<ColumnTypeExistsQuery, Result<bool>>
    {
        private readonly ILookupRepository _lookupRepository;
        private readonly ILogger<ColumnTypeExistsQueryHandler> _logger;
        public ColumnTypeExistsQueryHandler(ILookupRepository lookupRepository, ILogger<ColumnTypeExistsQueryHandler> logger)
        {
            _lookupRepository = lookupRepository ?? throw new ArgumentNullException(nameof(lookupRepository));
            _logger = logger;
        }

        public async Task<Result<bool>> HandleAsync(ColumnTypeExistsQuery query, CancellationToken cancellationToken = default)
        {
            if(query == null)
            {
                _logger.LogError("El query no puede ser nulo.");
                return Result<bool>.Failure("El query no puede ser nulo.");
            }
            if(query.ColumnTypeId <= 0)
            {
                _logger.LogError("El Id del tipo de columna no es valido.");
                return Result<bool>.Failure("El Id del tipo de columna no es valido.");
            }


            try 
            {
                var exists = await _lookupRepository.ColumnTypeExistsAsync(query.ColumnTypeId, cancellationToken);
                return Result<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ha ocurido un error al verificar si existe el tipo de columna.");
                return Result<bool>.Failure($"Ha ocurido un error al verificar si existe el tipo de columna: {ex.Message}");
            }
        }
    }
}
