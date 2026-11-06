using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Evently.Api;

public class AppDb(DbContextOptions<AppDb> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<EventIcon> EventIcons => Set<EventIcon>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<IdentityRole>().HasData(AppRoles.All.Select(r => new IdentityRole
        {
            Id = $"role-{r.ToLowerInvariant()}", Name = r, NormalizedName = r.ToUpperInvariant(), ConcurrencyStamp = $"role-{r.ToLowerInvariant()}",
        }));
        b.Entity<AppUser>(e =>
        {
            e.Property(u => u.FirstName).HasMaxLength(100);
            e.Property(u => u.LastName).HasMaxLength(100);
        });
        b.Entity<Category>(e =>
        {
            e.HasIndex(c => c.Name).IsUnique();
            e.Property(c => c.Name).HasMaxLength(100);
        });
        b.Entity<Country>(e =>
        {
            e.HasKey(c => c.Code);
            e.Property(c => c.Code).HasMaxLength(2);
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasData(SeedData.Countries.Select(c => new Country { Code = c.Code, Name = c.Name }));
        });
        b.Entity<Currency>(e =>
        {
            e.HasKey(c => c.Code);
            e.Property(c => c.Code).HasMaxLength(3);
            e.Property(c => c.Name).HasMaxLength(100);
            e.HasData(SeedData.Currencies.Select(c => new Currency { Code = c.Code, Name = c.Name }));
        });
        b.Entity<Topic>(e =>
        {
            e.HasIndex(t => t.Name).IsUnique();
            e.Property(t => t.Name).HasMaxLength(100);
        });
        b.Entity<IdempotencyKey>(e =>
        {
            e.HasKey(k => k.Key);
            e.Property(k => k.Key).HasMaxLength(64);
            e.HasOne(k => k.Event).WithMany().HasForeignKey(k => k.EventId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<EventIcon>().HasKey(i => i.EventId);
        b.Entity<Comment>(e =>
        {
            e.Property(c => c.Author).HasMaxLength(256);
            e.Property(c => c.Body).HasMaxLength(2000);
            e.HasIndex(c => new { c.EventId, c.CreatedAt });
            e.HasOne<Event>().WithMany().HasForeignKey(c => c.EventId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Event>(e =>
        {
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Topics).WithMany();
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.TicketCost).HasPrecision(12, 2);
            e.HasOne<EventIcon>().WithOne().HasForeignKey<EventIcon>(i => i.EventId).OnDelete(DeleteBehavior.Cascade);
            e.OwnsOne(x => x.Location, l => l.Property(p => p.Country).HasMaxLength(2));
        });
    }
}
