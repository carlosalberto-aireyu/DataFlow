using DataFlow.Core.Common;

namespace DataFlow.Core.Features.Commands
{
    public class DeleteConfigColumnCommand : ICommand<Result<bool>>
    {
        public int Id { get; set; }
        public DeleteConfigColumnCommand(int id)
        {
            Id = id;
        }

    }
}
