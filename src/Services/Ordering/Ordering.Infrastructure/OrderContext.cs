using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Ordering.Domain.Entities;

using SharedKernel.Domain.Entities;

namespace Ordering.Infrastructure;

public class OrderContext(DbContextOptions options) : DbContext(options)
{
  public DbSet<Order> Orders { get; set; }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfiguration(new Persistence.Configurations.OrderEntityTypeConfiguration());
    base.OnModelCreating(modelBuilder);
  }

  public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    foreach (EntityEntry<AuditableEntity<int>> entry in ChangeTracker.Entries<AuditableEntity<int>>())
      switch (entry.State)
      {
        case EntityState.Added:
          entry.Entity.SetCreated("swn");
          break;

        case EntityState.Modified:
          entry.Entity.SetLastModified("swn");
          break;
      }

    return base.SaveChangesAsync(cancellationToken);
  }
}
