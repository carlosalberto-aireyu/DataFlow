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
    public class GetDataTypesQueryHandler : IQueryHandler<GetDataTypesQuery, Result<IReadOnlyList<DataTypeLookup>>>
    {
        private readonly ILookupRepository _lookupRepository;
        public GetDataTypesQueryHandler(ILookupRepository lookupRepository)
        {
            _lookupRepository = lookupRepository ?? throw new ArgumentNullException(nameof(lookupRepository));
        }

        public async Task<Result<IReadOnlyList<DataTypeLookup>>> HandleAsync(GetDataTypesQuery query, CancellationToken cancellationToken = default)
        {
            try
            {
                var items = await _lookupRepository.GetAllDataTypesAsync(cancellationToken);
                return Result<IReadOnlyList<DataTypeLookup>>.Success(items);

            }
            catch (Exception ex)
            {
                return Result<IReadOnlyList<DataTypeLookup>>.Failure(
                    $"Error al recuperar DataTypeLookup: {ex.Message}");
            }
        }
    }
}
