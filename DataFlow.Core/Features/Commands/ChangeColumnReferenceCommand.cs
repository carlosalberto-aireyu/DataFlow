using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands
{
    public class ChangeColumnReferenceCommand : ICommand<Result<ConfigColumn>>
    {
        public int ConfigColumnId { get; }
        public string NewColumnLetter { get; }

        public ChangeColumnReferenceCommand(int configColumnId, string newColumnLetter)
        {
            ConfigColumnId = configColumnId;
            NewColumnLetter = newColumnLetter;
        }
    }
}
