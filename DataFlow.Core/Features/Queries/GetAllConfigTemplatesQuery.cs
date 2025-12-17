using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Queries
{
    public class GetAllConfigTemplatesQuery : IQuery<Result<IReadOnlyList<ConfigTemplate>>>
    {
        public GetAllConfigTemplatesQuery() { }
    }
}
