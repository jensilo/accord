namespace Accord.Web.Domain.Entities;

public class Requirement
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TemplateId { get; set; }
    public string Content { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public User User { get; set; } = default!;
    public Template Template { get; set; } = default!;
}
