using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Commands
{
    public class UpdateConfigTemplateCommand : ICommand<Result<ConfigTemplate>>
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public IList<ConfigColumn>? Columns { get; set; }

    }
}
