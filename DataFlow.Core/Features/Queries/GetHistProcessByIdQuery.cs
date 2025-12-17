using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries
{
    public class GetHistProcessByIdQuery : IQuery<Result<HistProcess>>
    {
        public int Id { get; set; }
        public GetHistProcessByIdQuery(int id)
        {
            Id = id;
        }
    }
}
