using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProductRequests.Domain.Offers;
using ProductRequests.Domain.ProductRequests;
using ProductRequests.Domain.Users;

namespace ProductRequests.Infrastructure.Persistence;

public sealed class ProductRequestsDbContext(DbContextOptions<ProductRequestsDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<ProductRequest> ProductRequests => Set<ProductRequest>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<OfferHistory> OfferHistories => Set<OfferHistory>();

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ChangeTracker.DetectChanges();
        await EnsureConcurrencyTokensAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductRequestsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    private async Task EnsureConcurrencyTokensAsync(CancellationToken cancellationToken)
    {
        foreach (EntityEntry<ProductRequest> entry in ChangeTracker.Entries<ProductRequest>()
                     .Where(item => item.State == EntityState.Modified))
        {
            Guid original = entry.Property(item => item.Version).OriginalValue;
            Guid? persisted = await ProductRequests.AsNoTracking()
                .Where(item => item.Id == entry.Entity.Id)
                .Select(item => (Guid?)item.Version)
                .SingleOrDefaultAsync(cancellationToken);
            if (persisted != original)
            {
                throw new DbUpdateConcurrencyException("Product request was modified concurrently.");
            }
        }

        foreach (EntityEntry<Offer> entry in ChangeTracker.Entries<Offer>()
                     .Where(item => item.State == EntityState.Modified))
        {
            Guid original = entry.Property(item => item.Version).OriginalValue;
            Guid? persisted = await Offers.AsNoTracking()
                .Where(item => item.Id == entry.Entity.Id)
                .Select(item => (Guid?)item.Version)
                .SingleOrDefaultAsync(cancellationToken);
            if (persisted != original)
            {
                throw new DbUpdateConcurrencyException("Offer was modified concurrently.");
            }
        }
    }
}
