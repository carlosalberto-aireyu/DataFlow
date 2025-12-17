using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Queries
{
    public class GetParametroByKeyQuery : IQuery<Result<Parametro?>>
    {
        public string ParametroKey { get; set; }

        public GetParametroByKeyQuery(string parametroKey) => ParametroKey = parametroKey;
    }
}
