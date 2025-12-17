using DataFlow.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries
{
    public class DataTypeExistsQuery : IQuery<Result<bool>>
    {
        public int DataTypeId { get; }
        public DataTypeExistsQuery(int dataTypeId)
        {
            DataTypeId = dataTypeId;
        }
    }
}
