using DataFlow.Core.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands
{
    public class DeleteParametroCommand : ICommand<Result<bool>>
    {
        [Required]
        public string ParametroKey { get; set; } = string.Empty;
    }
}
