namespace Accord.Web.Services;

public interface IRequirementService
{
    Task SaveAsync(Guid userId, Guid templateId, string content);
    Task<IReadOnlyList<Accord.Web.Domain.Entities.Requirement>> GetByUserAsync(Guid userId);
    Task DeleteAllAsync(Guid userId);
}
