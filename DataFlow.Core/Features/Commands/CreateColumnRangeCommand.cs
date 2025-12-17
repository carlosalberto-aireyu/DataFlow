
using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Commands
{
    public class CreateColumnRangeCommand : ICommand<Result<ColumnRange>>
    {
        public int ConfigColumnId { get; set; }
        public string? RFrom { get; set; }
        public string? RTo { get; set; }
        public string? DefaultValue { get; set; }

        public CreateColumnRangeCommand() { }

        public CreateColumnRangeCommand(int configColumnId, string? rFrom, string? rTo)
        {
            ConfigColumnId = configColumnId;
            RFrom = rFrom;
            RTo = rTo;
        }
    }
}