using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands
{
    public class CopyConfigColumnCommand:ICommand<Result<ConfigColumn>>
    {
        public int Id { get; set; }
        public CopyConfigColumnCommand(int id)
        {
            Id = id;
        }
    }
}
