using DataFlow.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries
{

    public class ColumnTypeExistsQuery : IQuery<Result<bool>>
    {
        public int ColumnTypeId { get; }
        public ColumnTypeExistsQuery(int columnTypeId)
        {
            ColumnTypeId = columnTypeId;
        }
    }
}
