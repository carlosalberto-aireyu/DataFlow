using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataFlow.Core.Models
{
    public class HistProcess : BaseEntity
    {
        [ForeignKey(nameof(ConfigTemplate))]
        public int ConfigTemplateId { get; set; }
        public ConfigTemplate? ConfigTemplate { get; set; }
        public string? DataProcess { get; set; }
        public string? FileSource { get; set; }
        public string? FileTarget { get; set; }
        public string? FinalStatus { get; set; }
    }
}
