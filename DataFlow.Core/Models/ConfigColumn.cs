using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Core.Models
{
    public class ConfigColumn: BaseEntity
    {

        public int IndexColumn { get; set; }

        [ForeignKey(nameof(ConfigTemplate))]
        public int ConfigTemplateId { get; set; }
        public ConfigTemplate? ConfigTemplate { get; set; }
        public string? Name { get; set; }
        public string? NameDisplay { get; set; }    
        public string? Description { get; set; }
        public int DataTypeId { get; set; }
        public DataTypeLookup? DataType { get; set; }
        public string? DefaultValue { get; set; }
        public int ColumnTypeId { get; set; }
        public ColumnTypeLookup? ColumnType { get; set; }

        public ICollection<ColumnRange> Ranges { get; set; } = new List<ColumnRange>();
    }
}
