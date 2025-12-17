using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Queries
{
    public class GetHistProcessByConfigTemplateIdQuery : IQuery<Result<IReadOnlyList<HistProcess>>>
    {
        public int ConfigTemplateId { get; set; }
        public GetHistProcessByConfigTemplateIdQuery(int configTemplateId)
        {
            ConfigTemplateId = configTemplateId;
        }
    }
}
