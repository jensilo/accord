using System.Text.Json;
using Accord.Web.Domain.Entities;
using Accord.Web.Domain.Parser;
using Accord.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Accord.Web.Infrastructure.Seeding;

public class TemplateOptions
{
    public string DefinitionsPath { get; set; } = "docs/templates/paris";
}

public class TemplateSeeder(AppDbContext db, IOptions<TemplateOptions> options, IHostEnvironment env, ILogger<TemplateSeeder> logger)
{
    public async Task SeedAsync()
    {
        var basePath = Path.IsPathRooted(options.Value.DefinitionsPath)
            ? options.Value.DefinitionsPath
            : Path.Combine(env.ContentRootPath, options.Value.DefinitionsPath);

        if (!Directory.Exists(basePath))
        {
            logger.LogWarning("Template definitions path {Path} does not exist; skipping seeding", basePath);
            return;
        }

        foreach (var versionDir in Directory.GetDirectories(basePath).OrderBy(d => d))
            await SeedVersionAsync(versionDir);
    }

    private async Task SeedVersionAsync(string dir)
    {
        var dirName = Path.GetFileName(dir);
        var version = dirName.StartsWith('v') ? dirName[1..] : dirName;

        var byType = new Dictionary<string, List<(TemplateConfig config, string rawJson)>>();

        foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f))
        {
            var json = await File.ReadAllTextAsync(file);
            var config = JsonSerializer.Deserialize<TemplateConfig>(json);
            if (config == null) continue;

            if (!byType.TryGetValue(config.Type, out var list))
                byType[config.Type] = list = [];
            list.Add((config, json));
        }

        foreach (var (type, templates) in byType)
        {
            var setName = type.ToUpperInvariant();
            var set = await db.TemplateSets.FirstOrDefaultAsync(s => s.Name == setName && s.Version == version);

            if (set == null)
            {
                set = new TemplateSet
                {
                    Id = Guid.NewGuid(),
                    Name = setName,
                    Version = version,
                    Description = $"PARIS {setName} templates v{version}"
                };
                db.TemplateSets.Add(set);
                await db.SaveChangesAsync();
                logger.LogInformation("Created template set {Name} v{Version}", setName, version);
            }

            foreach (var (config, rawJson) in templates)
            {
                var exists = await db.Templates.AnyAsync(t =>
                    t.TemplateSetId == set.Id &&
                    t.Type == config.Type &&
                    t.Name == config.Name &&
                    t.Version == config.Version);

                if (exists) continue;

                db.Templates.Add(new Template
                {
                    Id = Guid.NewGuid(),
                    TemplateSetId = set.Id,
                    Type = config.Type,
                    Name = config.Name,
                    Version = config.Version,
                    Config = rawJson
                });
                logger.LogInformation("Seeded template {Name}", config.Name);
            }

            await db.SaveChangesAsync();
        }
    }
}
