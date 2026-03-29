namespace Accord.Web.Domain.Entities;

public class TemplateSet
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Version { get; set; } = default!;
    public string Description { get; set; } = default!;
    public ICollection<Template> Templates { get; set; } = [];
}
