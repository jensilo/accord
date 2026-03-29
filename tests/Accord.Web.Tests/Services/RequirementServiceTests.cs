using Accord.Web.Domain.Entities;
using Accord.Web.Infrastructure.Data;
using Accord.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Accord.Web.Tests.Services;

public class RequirementServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task<(Guid templateSetId, Guid templateId)> SeedTemplateAsync(AppDbContext db)
    {
        var setId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        db.TemplateSets.Add(new TemplateSet
        {
            Id = setId, Name = "Test Set", Version = "1.0", Description = "Test"
        });
        db.Templates.Add(new Template
        {
            Id = templateId, TemplateSetId = setId, Type = "ebt",
            Name = "Test Template", Version = "1.0", Config = "{}"
        });
        await db.SaveChangesAsync();
        return (setId, templateId);
    }

    [Fact]
    public async Task SaveAsync_CreatesRequirementWithCorrectFields()
    {
        var db = CreateDb();
        var service = new RequirementService(db);

        var userId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        await service.SaveAsync(userId, templateId, "The system must do X.");

        var req = db.Requirements.Single();
        req.UserId.Should().Be(userId);
        req.TemplateId.Should().Be(templateId);
        req.Content.Should().Be("The system must do X.");
        req.CreatedAt.Should().BeOnOrAfter(before);
        req.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyRequirementsForSpecifiedUser()
    {
        var db = CreateDb();
        var service = new RequirementService(db);
        var (_, templateId) = await SeedTemplateAsync(db);

        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();

        await service.SaveAsync(user1, templateId, "Req A");
        await service.SaveAsync(user2, templateId, "Req B");
        await service.SaveAsync(user1, templateId, "Req C");

        var results = await service.GetByUserAsync(user1);

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(r => r.UserId.Should().Be(user1));
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsRequirementsOrderedByCreatedAtDescending()
    {
        var db = CreateDb();
        var service = new RequirementService(db);
        var (_, templateId) = await SeedTemplateAsync(db);

        var userId = Guid.NewGuid();

        // Add with explicit timestamps to ensure order is deterministic
        db.Requirements.AddRange(
            new Requirement
            {
                Id = Guid.NewGuid(), UserId = userId, TemplateId = templateId,
                Content = "Oldest", CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new Requirement
            {
                Id = Guid.NewGuid(), UserId = userId, TemplateId = templateId,
                Content = "Middle", CreatedAt = DateTime.UtcNow.AddMinutes(-5)
            },
            new Requirement
            {
                Id = Guid.NewGuid(), UserId = userId, TemplateId = templateId,
                Content = "Newest", CreatedAt = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var results = await service.GetByUserAsync(userId);

        results.Should().HaveCount(3);
        results[0].Content.Should().Be("Newest");
        results[1].Content.Should().Be("Middle");
        results[2].Content.Should().Be("Oldest");
    }

    [Fact]
    public async Task GetByUserAsync_NoRequirements_ReturnsEmptyList()
    {
        var db = CreateDb();
        var service = new RequirementService(db);

        var results = await service.GetByUserAsync(Guid.NewGuid());

        results.Should().BeEmpty();
    }
}
