using DataFlow.Core.Data;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


namespace DataFlow.Core.Repositories
{
    public class LookupRepository : ILookupRepository
    {
        private readonly AppDbContext _db;

        public LookupRepository(AppDbContext db)
        {
            _db = db;
        }

        public Task<bool> ColumnTypeExistsAsync(int id, CancellationToken ct)
            => _db.ColumnTypeLookups.AnyAsync(x => x.Id == id, ct);

        public Task<bool> DataTypeExistsAsync(int id, CancellationToken ct)
            => _db.DataTypeLookups.AnyAsync(x => x.Id == id, ct);

        public async Task<IReadOnlyList<ColumnTypeLookup>> GetAllColumnTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.ColumnTypeLookups
                .AsNoTracking()
                .OrderBy(ct => ct.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DataTypeLookup>> GetAllDataTypesAsync(CancellationToken cancellationToken = default)
        {
            return await _db.DataTypeLookups    
                .AsNoTracking()
                .OrderBy(dt => dt.Id)
                .ToListAsync(cancellationToken);
        }
    }
}
