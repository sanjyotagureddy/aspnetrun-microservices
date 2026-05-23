namespace Basket.API.Application.Contracts.Infrastructure;

public interface IDiscountService
{
  Task<decimal> GetDiscountAsync(string productName, CancellationToken cancellationToken = default);
}