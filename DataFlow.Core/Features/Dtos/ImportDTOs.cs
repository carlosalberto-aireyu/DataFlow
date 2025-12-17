using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataFlow.Core.Features.Dtos
{
    public record ImportRootDto(DateTime? ExportedAt, List<TemplateDto>? Templates);

    public record TemplateDto
    {
        public int Id { get; init; }
        public string? Description { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public List<ConfigColumnDto>? ConfigColumns { get; init; }
    }

    public record ConfigColumnDto
    {
        public int Id { get; init; }
        public int ConfigTemplateId { get; init; }
        public int IndexColumn { get; init; }
        public string? Name { get; init; }
        public string? NameDisplay { get; init; }
        public string? Description { get; init; }
        public int DataTypeId { get; init; }
        public int ColumnTypeId { get; init; }
        public string? DefaultValue { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
        public List<RangeDto>? Ranges { get; init; }
    }

    public record RangeDto
    {
        public int Id { get; init; }
        public int ConfigColumnId { get; init; }
        public string? RFrom { get; init; }
        public string? RTo { get; init; }
        public string? DefaultValue { get; init; }
        public DateTime? CreatedAt { get; init; }
        public DateTime? UpdatedAt { get; init; }
    }

}
