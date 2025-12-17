using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Commands
{
    public class UpdateColumnRangeCommand : ICommand<Result<ColumnRange>>
    {
        public int Id { get; set; }
        public string? RFrom { get; set; }
        public string? RTo { get; set; }
        public string ? DefaultValue { get; set; }
        public UpdateColumnRangeCommand() { }

        public UpdateColumnRangeCommand(int id, string? rFrom, string? rTo)
        {
            Id = id;
            RFrom = rFrom;
            RTo = rTo;
        }
    }
}
