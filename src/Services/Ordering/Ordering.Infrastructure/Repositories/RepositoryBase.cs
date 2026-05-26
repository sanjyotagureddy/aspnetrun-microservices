using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Ordering.Application.Contracts.Persistence;
using Ordering.Infrastructure.Persistence;
using SharedKernel.Domain.Entities;

namespace Ordering.Infrastructure.Repositories;

public class RepositoryBase<T, TId>(OrderContext dbContext) : IAsyncRepository<T, TId>
  where T : Entity<TId>
  where TId : notnull
{
  protected readonly OrderContext DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

  public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
  {
    return await DbContext.Set<T>().ToListAsync(cancellationToken);
  }

  public async Task<IReadOnlyList<T>> GetAsync(
    Expression<Func<T, bool>> predicate = null,
    Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
    IEnumerable<Expression<Func<T, object>>> includes = null,
    bool disableTracking = true,
    CancellationToken cancellationToken = default)
  {
    IQueryable<T> query = DbContext.Set<T>();
    if (disableTracking) query = query.AsNoTracking();

    if (predicate != null) query = query.Where(predicate);

    if (includes != null) query = includes.Aggregate(query, (current, include) => current.Include(include));

    if (orderBy != null)
      return await orderBy(query).ToListAsync(cancellationToken);

    return await query.ToListAsync(cancellationToken);
  }

  public virtual async Task<T> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
  {
    return await DbContext.Set<T>().FindAsync(new object[] { id }, cancellationToken);
  }

  public async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
  {
    await DbContext.Set<T>().AddAsync(entity, cancellationToken);
    await DbContext.SaveChangesAsync(cancellationToken);
    return entity;
  }

  public async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
  {
    DbContext.Entry(entity).State = EntityState.Modified;
    await DbContext.SaveChangesAsync(cancellationToken);
  }

  public async Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
  {
    DbContext.Set<T>().Remove(entity);
    await DbContext.SaveChangesAsync(cancellationToken);
  }
}