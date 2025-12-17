using DataFlow.Core.Data;
using DataFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Repositories
{
    public class EFParametroRepository : EFRepository<Parametro>, IParametroRepository
    {

        public EFParametroRepository(AppDbContext context) : base(context)
        {}

        public async Task<Parametro?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            return await DbSet.FirstOrDefaultAsync(p => p.ParametroKey == key, cancellationToken);
        }
    }
}
