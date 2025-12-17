using DataFlow.Core.Common;

namespace DataFlow.Core.Features.Commands
{
    public class DeleteConfigTemplateCommand : ICommand<Result<bool>>
    {
        public int Id { get; set; }
        public DeleteConfigTemplateCommand(int id)
        {
            Id = id;
        }
    }
}
