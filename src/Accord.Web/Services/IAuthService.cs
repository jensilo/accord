using Accord.Web.Domain.Entities;

namespace Accord.Web.Services;

public interface IAuthService
{
    Task<User> FindOrCreateUser(string email);
    Task<string> CreateMagicLinkToken(Guid userId);
    Task<User?> ValidateAndConsumeToken(string token);
}
