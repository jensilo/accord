using Accord.Web.Domain.Entities;
using Accord.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Accord.Web.Services;

public class RequirementService(AppDbContext db) : IRequirementService
{
    public async Task SaveAsync(Guid userId, Guid templateId, string content)
    {
        db.Requirements.Add(new Requirement
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TemplateId = templateId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    public async Task DeleteAllAsync(Guid userId)
    {
        await db.Requirements.Where(r => r.UserId == userId).ExecuteDeleteAsync();
    }

    public async Task<IReadOnlyList<Requirement>> GetByUserAsync(Guid userId) =>
        await db.Requirements
            .Include(r => r.Template)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(150)
            .ToListAsync();
}
