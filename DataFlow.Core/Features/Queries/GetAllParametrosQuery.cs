using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries
{
    public class GetAllParametrosQuery : IQuery<Result<IReadOnlyList<Parametro>>>
    {
        public GetAllParametrosQuery() { }

    }
}
