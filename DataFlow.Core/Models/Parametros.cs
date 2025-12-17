using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Models
{
    public class Parametro : BaseEntity
    {
        public string ParametroKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ParametroValue { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

}
