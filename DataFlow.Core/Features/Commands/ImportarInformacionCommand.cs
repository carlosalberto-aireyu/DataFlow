using DataFlow.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands
{
    public class ImportarInformacionCommand : ICommand<Result<bool>>
    {
        public string FilePath { get; }
        public bool OverwriteExisting { get; set; } = false;
        public ImportarInformacionCommand(string filePath, bool overwriteExisting = true)
        {
            FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
            OverwriteExisting = overwriteExisting;
        }
    }
}
