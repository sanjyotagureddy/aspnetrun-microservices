using Microsoft.EntityFrameworkCore;
using Ordering.Domain.Entities;
using SharedKernel.Domain.Entities;

namespace Ordering.Infrastructure.Persistence;

public class OrderContext(DbContextOptions options) : DbContext(options)
{
  public DbSet<Order> Orders { get; set; }

  public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    foreach (var entry in ChangeTracker.Entries<AuditableEntity<int>>())
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