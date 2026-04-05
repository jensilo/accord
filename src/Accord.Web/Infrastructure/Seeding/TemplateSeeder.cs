using System.Text.Json;
using Accord.Web.Domain.Entities;
using Accord.Web.Domain.Parser;
using Accord.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Accord.Web.Infrastructure.Seeding;

public class TemplateOptions
{
    public string DefinitionsPath { get; set; } = "docs/templates";
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

        // Migrate legacy TemplateSets that were seeded before the Family field existed
        var legacySets = await db.TemplateSets.Where(s => s.Family == "").ToListAsync();
        if (legacySets.Count > 0)
        {
            foreach (var s in legacySets) s.Family = "paris";
            await db.SaveChangesAsync();
            logger.LogInformation("Migrated {Count} legacy template set(s) to family 'paris'", legacySets.Count);
        }

        foreach (var familyDir in Directory.GetDirectories(basePath).OrderBy(d => d))
        {
            var family = Path.GetFileName(familyDir);
            foreach (var versionDir in Directory.GetDirectories(familyDir).OrderBy(d => d))
                await SeedVersionAsync(family, versionDir);
        }
    }

    private async Task SeedVersionAsync(string family, string dir)
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
            var set = await db.TemplateSets.FirstOrDefaultAsync(s => s.Family == family && s.Name == setName && s.Version == version);

            if (set == null)
            {
                set = new TemplateSet
                {
                    Id = Guid.NewGuid(),
                    Family = family,
                    Name = setName,
                    Version = version,
                    Description = $"{family}/{setName} templates v{version}"
                };
                db.TemplateSets.Add(set);
                await db.SaveChangesAsync();
                logger.LogInformation("Created template set {Family}/{Name} v{Version}", family, setName, version);
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

            // Remove stale templates whose version no longer matches the seeded version
            var seededVersions = templates.Select(t => t.config.Version).ToHashSet();
            var stale = await db.Templates
                .Where(t => t.TemplateSetId == set.Id && !seededVersions.Contains(t.Version))
                .ToListAsync();
            if (stale.Count > 0)
            {
                db.Templates.RemoveRange(stale);
                await db.SaveChangesAsync();
                logger.LogInformation("Removed {Count} stale template(s) from set {Name} v{Version}", stale.Count, setName, version);
            }
        }
    }
}
