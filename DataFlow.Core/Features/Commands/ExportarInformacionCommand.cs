using DataFlow.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands
{
    public class ExportarInformacionCommand : ICommand<Result<bool>>
    {
        public int ParametroId { get; set; }
        public ExportarInformacionCommand(int parametroId)
        {
            ParametroId = parametroId;
        }
    }
}
