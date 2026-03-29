using Accord.Web.Domain.Entities;
using Accord.Web.Infrastructure.Data;
using Accord.Web.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Accord.Web.Tests.Services;

file sealed class FakeMailService : IMailService
{
    public Task SendMagicLink(string email, string link) => Task.CompletedTask;
}

public class AuthServiceTests
{
    private static (AppDbContext db, AuthService service) Create(int ttlMinutes = 15)
    {
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var service = new AuthService(db, Options.Create(new AuthOptions
        {
            MagicLinkTtlMinutes = ttlMinutes
        }));

        return (db, service);
    }

    // ---------------------------------------------------------------------------
    // FindOrCreateUser
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task FindOrCreateUser_NewEmail_CreatesUser()
    {
        var (db, service) = Create();

        var user = await service.FindOrCreateUser("Alice@Example.com");

        user.Should().NotBeNull();
        user.Email.Should().Be("alice@example.com");
        db.Users.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindOrCreateUser_ExistingEmail_ReturnsExistingUser()
    {
        var (db, service) = Create();

        var first = await service.FindOrCreateUser("bob@example.com");
        var second = await service.FindOrCreateUser("BOB@EXAMPLE.COM");

        second.Id.Should().Be(first.Id);
        db.Users.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindOrCreateUser_NewUser_HasUtcCreatedAt()
    {
        var (_, service) = Create();
        var before = DateTime.UtcNow;

        var user = await service.FindOrCreateUser("carol@example.com");

        user.CreatedAt.Should().BeOnOrAfter(before);
        user.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    // ---------------------------------------------------------------------------
    // CreateMagicLinkToken
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task CreateMagicLinkToken_CreatesTokenForUser()
    {
        var (db, service) = Create();
        var user = await service.FindOrCreateUser("dave@example.com");

        var token = await service.CreateMagicLinkToken(user.Id);

        token.Should().NotBeNullOrEmpty();
        db.MagicLinkTokens.Should().ContainSingle(t => t.Token == token && t.UserId == user.Id);
    }

    [Fact]
    public async Task CreateMagicLinkToken_TokenExpiresAfterTtl()
    {
        var (db, service) = Create(ttlMinutes: 30);
        var user = await service.FindOrCreateUser("eve@example.com");
        var before = DateTime.UtcNow;

        var token = await service.CreateMagicLinkToken(user.Id);

        var link = db.MagicLinkTokens.Single(t => t.Token == token);
        link.ExpiresAt.Should().BeCloseTo(before.AddMinutes(30), TimeSpan.FromSeconds(5));
    }

    // ---------------------------------------------------------------------------
    // ValidateAndConsumeToken
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task ValidateAndConsumeToken_ValidToken_ReturnsUser()
    {
        var (_, service) = Create();
        var user = await service.FindOrCreateUser("frank@example.com");
        var token = await service.CreateMagicLinkToken(user.Id);

        var result = await service.ValidateAndConsumeToken(token);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task ValidateAndConsumeToken_ValidToken_MarksTokenAsUsed()
    {
        var (db, service) = Create();
        var user = await service.FindOrCreateUser("grace@example.com");
        var token = await service.CreateMagicLinkToken(user.Id);

        await service.ValidateAndConsumeToken(token);

        db.MagicLinkTokens.Single(t => t.Token == token).Used.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAndConsumeToken_UsedToken_ReturnsNull()
    {
        var (_, service) = Create();
        var user = await service.FindOrCreateUser("heidi@example.com");
        var token = await service.CreateMagicLinkToken(user.Id);

        await service.ValidateAndConsumeToken(token); // consume once
        var result = await service.ValidateAndConsumeToken(token); // second attempt

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndConsumeToken_ExpiredToken_ReturnsNull()
    {
        var (db, service) = Create(ttlMinutes: 0);
        var user = await service.FindOrCreateUser("ivan@example.com");
        var token = await service.CreateMagicLinkToken(user.Id);

        // Force expiry by backdating in the database
        var link = db.MagicLinkTokens.Single(t => t.Token == token);
        link.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var result = await service.ValidateAndConsumeToken(token);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAndConsumeToken_NonExistentToken_ReturnsNull()
    {
        var (_, service) = Create();

        var result = await service.ValidateAndConsumeToken("0000000000000000000000000000000000000000000000000000000000000000");

        result.Should().BeNull();
    }
}
