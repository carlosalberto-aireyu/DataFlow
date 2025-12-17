using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Queries
{
    public class GetConfigTemplateByIdQuery : IQuery<Result<ConfigTemplate>>
    {
        public int Id { get; set; }

        public GetConfigTemplateByIdQuery(int id) => Id = id;
    }
}
