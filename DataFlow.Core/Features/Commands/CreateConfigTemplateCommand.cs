using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Commands
{
    public class CreateConfigTemplateCommand : ICommand<Result<ConfigTemplate>>
    {
        public string? Description { get; set; }
        public IList<ConfigColumn>? Columns { get; set; }
    }
}
