using DataFlow.Core.Common;

namespace DataFlow.Core.Features.Commands
{
    public class DeleteColumnRangeCommand : ICommand<Result<bool>>
    {
        public int Id { get; set; }
        public DeleteColumnRangeCommand(int id)
        {
            Id = id;
        }
    }
}
