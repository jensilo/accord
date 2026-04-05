using Accord.Web.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Accord.Web.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    public DbSet<User> Users => Set<User>();
    public DbSet<MagicLinkToken> MagicLinkTokens => Set<MagicLinkToken>();
    public DbSet<TemplateSet> TemplateSets => Set<TemplateSet>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<Requirement> Requirements => Set<Requirement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(320);
        });

        modelBuilder.Entity<MagicLinkToken>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.Token).IsUnique();
            e.Property(t => t.Token).HasMaxLength(64);
            e.HasOne(t => t.User).WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TemplateSet>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasIndex(s => new { s.Family, s.Name, s.Version }).IsUnique();
            e.Property(s => s.Family).HasMaxLength(50);
            e.Property(s => s.Name).HasMaxLength(200);
            e.Property(s => s.Version).HasMaxLength(50);
            e.Property(s => s.Description).HasMaxLength(2000);
        });

        modelBuilder.Entity<Template>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => new { t.TemplateSetId, t.Type, t.Name, t.Version }).IsUnique();
            e.Property(t => t.Type).HasMaxLength(50);
            e.Property(t => t.Name).HasMaxLength(200);
            e.Property(t => t.Version).HasMaxLength(50);
            e.HasOne(t => t.TemplateSet).WithMany(s => s.Templates).HasForeignKey(t => t.TemplateSetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Requirement>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Content).HasMaxLength(10000);
            e.HasOne(r => r.User).WithMany().HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Template).WithMany().HasForeignKey(r => r.TemplateId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
