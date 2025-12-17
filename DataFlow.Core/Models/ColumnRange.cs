using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Core.Models
{
    public class ColumnRange : BaseEntity
    {
        [ForeignKey(nameof(ConfigColumn))]
        public int ConfigColumnId { get; set; }
        public ConfigColumn? ConfigColumn { get; set; }
        public string? DefaultValue { get; set; } = string.Empty;
        public string? RFrom { get; set; }
        public string? RTo { get; set; }
    }
}
