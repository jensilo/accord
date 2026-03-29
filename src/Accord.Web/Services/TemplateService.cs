using System.Text.Json;
using Accord.Web.Domain.Entities;
using Accord.Web.Domain.Parser;
using Accord.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accord.Web.Services;

public class TemplateService(AppDbContext db) : ITemplateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public Task<List<TemplateSet>> GetTemplateSets() =>
        db.TemplateSets
            .Include(s => s.Templates.OrderBy(t => t.Name))
            .OrderBy(s => s.Name)
            .ToListAsync();

    public Task<List<Template>> GetTemplates(Guid templateSetId) =>
        db.Templates.Where(t => t.TemplateSetId == templateSetId).OrderBy(t => t.Name).ToListAsync();

    public Task<Template?> GetTemplate(Guid id) =>
        db.Templates.Include(t => t.TemplateSet).FirstOrDefaultAsync(t => t.Id == id);

    public TemplateConfig ParseConfig(Template template) =>
        JsonSerializer.Deserialize<TemplateConfig>(template.Config, JsonOptions)!;
}
