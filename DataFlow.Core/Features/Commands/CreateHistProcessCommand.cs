
using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Commands
{
    public class CreateHistProcessCommand : ICommand<Result<HistProcess>>
    {
        public int ConfigTemplateId { get; set; }
        public string? DataProcess { get; set; }

        public string? FileSource { get; set; }

        public string? FileTarget { get; set; }

        public string? FinalStatus { get; set; }

        public CreateHistProcessCommand() { }

        public CreateHistProcessCommand(int configTemplateId, string? dataProcess, string? fileSource, string? fileTarget, string? finalStatus)
        {
            ConfigTemplateId = configTemplateId;
            DataProcess = dataProcess;
            FileSource = fileSource;
            FileTarget = fileTarget;
            FinalStatus = finalStatus;
        }
    }
}
