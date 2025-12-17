
using DataFlow.Core.Common;
namespace DataFlow.Core.Features.Commands
{
    public class DeleteHistProcessCommand : ICommand<Result<bool>>
    {
        public int Id { get; set; }

        public DeleteHistProcessCommand() { }
        public DeleteHistProcessCommand(int id)
        {
            Id = id;
        }
    }
}
