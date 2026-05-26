using System.Linq.Expressions;
using SharedKernel.Domain.Entities;

namespace Ordering.Application.Contracts.Persistence;

public interface IAsyncRepository<T, in TId>
  where T : Entity<TId>
  where TId : notnull
{
  Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

  Task<IReadOnlyList<T>> GetAsync(
    Expression<Func<T, bool>> predicate = null,
    Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null,
    IEnumerable<Expression<Func<T, object>>> includes = null,
    bool disableTracking = true,
    CancellationToken cancellationToken = default);

  Task<T> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

  Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

  Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

  Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}