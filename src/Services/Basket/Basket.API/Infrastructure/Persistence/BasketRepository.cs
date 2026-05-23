using Basket.API.Application.Contracts.Persistence;
using Basket.API.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Basket.API.Infrastructure.Persistence;

internal sealed class BasketRepository(IDistributedCache redisCache) : IBasketRepository
{
  private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

  private readonly IDistributedCache _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));

  public async Task<ShoppingCart> GetBasketAsync(string userName, CancellationToken cancellationToken = default)
  {
    string basket = await _redisCache.GetStringAsync(userName, cancellationToken);
    return string.IsNullOrWhiteSpace(basket)
      ? null
      : JsonSerializer.Deserialize<ShoppingCart>(basket, SerializerOptions);
  }

  public async Task<ShoppingCart> UpdateBasketAsync(ShoppingCart basket, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(basket);
    await _redisCache.SetStringAsync(basket.Id, JsonSerializer.Serialize(basket, SerializerOptions), cancellationToken);
    return await GetBasketAsync(basket.Id, cancellationToken) ?? basket;
  }

  public Task DeleteBasketAsync(string userName, CancellationToken cancellationToken = default)
  {
    return _redisCache.RemoveAsync(userName, cancellationToken);
  }
}