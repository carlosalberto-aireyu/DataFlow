using System.ComponentModel.DataAnnotations;

namespace DataFlow.Core.Models
{
    public class ConfigTemplate : BaseEntity
    {
        public string? Description { get; set; }

        public ICollection<ConfigColumn> ConfigColumns { get; set; } = new List<ConfigColumn>();
        public ICollection<HistProcess> HistProcess { get; set; } = new List<HistProcess>();
    }
}
