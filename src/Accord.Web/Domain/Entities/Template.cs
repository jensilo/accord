namespace Accord.Web.Domain.Entities;

public class Template
{
    public Guid Id { get; set; }
    public Guid TemplateSetId { get; set; }
    public string Type { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Config { get; set; } = default!;
    public TemplateSet TemplateSet { get; set; } = default!;
}
