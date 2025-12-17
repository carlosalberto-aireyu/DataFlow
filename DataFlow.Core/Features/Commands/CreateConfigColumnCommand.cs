using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Commands
{

    public class CreateConfigColumnCommand : ICommand<Result<ConfigColumn>>
    {
        public int IndexColumn { get; set; }
        public int ConfigTemplateId { get; set; }
        public string? Name { get; set; }
        public string? NameDisplay { get; set; }
        public string? Description { get; set; }
        public int DataTypeId { get; set; }
        public string? DefaultValue { get; set; }
        public int ColumnTypeId { get; set; }
    }
}
