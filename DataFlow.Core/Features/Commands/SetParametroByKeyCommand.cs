using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands
{
    public class SetParametroByKeyCommand : ICommand<Result<Parametro>>
    {
        public string ParametroKey { get; }
        public string ParametroValue { get; }
        public SetParametroByKeyCommand(string parametroKey, string parametroValue)
        {
            ParametroKey = parametroKey ?? throw new ArgumentNullException(nameof(parametroKey));
            ParametroValue = parametroValue ?? throw new ArgumentNullException(nameof(parametroValue));
        }

    }
}
