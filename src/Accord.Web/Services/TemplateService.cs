using System.Text.Json;
using Accord.Web.Domain.Entities;
using Accord.Web.Domain.Parser;
using Accord.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accord.Web.Services;

public class TemplateService(AppDbContext db) : ITemplateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<TemplateSet>> GetTemplateSets()
    {
        var all = await db.TemplateSets
            .Include(s => s.Templates.OrderBy(t => t.Name))
            .ToListAsync();
        return all
            .GroupBy(s => new { s.Family, s.Name })
            .Select(g => g.OrderByDescending(s => s.Version).First())
            .OrderBy(s => s.Family).ThenBy(s => s.Name)
            .ToList();
    }

    public Task<List<Template>> GetTemplates(Guid templateSetId) =>
        db.Templates.Where(t => t.TemplateSetId == templateSetId).OrderBy(t => t.Name).ToListAsync();

    public Task<Template?> GetTemplate(Guid id) =>
        db.Templates.Include(t => t.TemplateSet).FirstOrDefaultAsync(t => t.Id == id);

    public TemplateConfig ParseConfig(Template template) =>
        JsonSerializer.Deserialize<TemplateConfig>(template.Config, JsonOptions)!;
}
