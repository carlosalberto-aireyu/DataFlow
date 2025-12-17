using DataFlow.Core.Common;
using DataFlow.Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Commands
{
    public class CreateParametroCommand : ICommand<Result<Parametro>>
    {
        [Required]
        public string ParametroKey { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string ParametroValue { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
