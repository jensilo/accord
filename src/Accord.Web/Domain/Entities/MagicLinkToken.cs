namespace Accord.Web.Domain.Entities;

public class MagicLinkToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public User User { get; set; } = default!;
}
