using DataFlow.Core.Common;
using DataFlow.Core.Data;
using DataFlow.Core.Models;
using DataFlow.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries.Handlers
{
    public class GetColumnTypesQueryHandler : IQueryHandler<GetColumnTypesQuery, Result<IReadOnlyList<ColumnTypeLookup>>>
    {
        private readonly ILookupRepository _lookupRepository;
        public GetColumnTypesQueryHandler(ILookupRepository lookupRepository)
        {
            _lookupRepository = lookupRepository ?? throw new ArgumentNullException(nameof(lookupRepository));
        }

        public async Task<Result<IReadOnlyList<ColumnTypeLookup>>> HandleAsync(GetColumnTypesQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _lookupRepository.GetAllColumnTypesAsync(cancellationToken);
                return Result<IReadOnlyList<ColumnTypeLookup>>.Success(items);

            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<ColumnTypeLookup>>.Failure(
                    $"Error al recuperar ColumnTypeLookups: {ex.Message}");
            }
        }
    }
}
