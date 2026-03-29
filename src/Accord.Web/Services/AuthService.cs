using System.Security.Cryptography;
using Accord.Web.Domain.Entities;
using Accord.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Accord.Web.Services;

public class AuthOptions
{
    public string CookieName { get; set; } = "accord.session";
    public int MagicLinkTtlMinutes { get; set; } = 15;
}

public class AuthService(AppDbContext db, IOptions<AuthOptions> options) : IAuthService
{
    public async Task<User> FindOrCreateUser(string email)
    {
        var normalised = email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalised);
        if (user != null) return user;

        user = new User { Id = Guid.NewGuid(), Email = normalised, CreatedAt = DateTime.UtcNow };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<string> CreateMagicLinkToken(Guid userId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        db.MagicLinkTokens.Add(new MagicLinkToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(options.Value.MagicLinkTtlMinutes),
            Used = false
        });

        await db.SaveChangesAsync();
        return token;
    }

    public async Task<User?> ValidateAndConsumeToken(string token)
    {
        var link = await db.MagicLinkTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token && !t.Used && t.ExpiresAt > DateTime.UtcNow);

        if (link == null) return null;

        link.Used = true;
        await db.SaveChangesAsync();
        return link.User;
    }
}
