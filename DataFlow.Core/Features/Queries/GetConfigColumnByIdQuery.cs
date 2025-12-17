using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries
{
    public class GetConfigColumnByIdQuery : IQuery<Result<ConfigColumn>>
    {
        public int Id { get; set; }
        public GetConfigColumnByIdQuery(int id) => Id = id;
    }
}
