using DataFlow.Core.Common;
using DataFlow.Core.Models;

namespace DataFlow.Core.Features.Commands
{
    // Comando para actualizar una columna existente
    public class UpdateConfigColumnCommand : ICommand<Result<ConfigColumn>>
    {
        public int Id { get; set; }
        public int IndexColumn { get; set; }
        public string? Name { get; set; }
        public string? NameDisplay { get; set; }
        public string? Description { get; set; }
        public int DataTypeId { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public int ColumnTypeId { get; set; }

    }
}
