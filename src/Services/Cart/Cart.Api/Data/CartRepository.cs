using System.Text.Json;
using Cart.Api.Domain;
using Microsoft.Extensions.Caching.Distributed;

namespace Cart.Api.Data;

public class CartRepository : ICartRepository
{
    private readonly IDistributedCache _redisCache;

    public CartRepository(IDistributedCache redisCache)
    {
        _redisCache = redisCache ?? throw new ArgumentNullException(nameof(redisCache));
    }

    public async Task<ShoppingCart> GetCartAsync(string userName, CancellationToken cancellationToken = default)
    {
        var cartJson = await _redisCache.GetStringAsync(userName, cancellationToken);
        if (string.IsNullOrEmpty(cartJson))
        {
            return new ShoppingCart(userName);
        }

        return JsonSerializer.Deserialize<ShoppingCart>(cartJson) ?? new ShoppingCart(userName);
    }

    public async Task<ShoppingCart> UpdateCartAsync(ShoppingCart cart, CancellationToken cancellationToken = default)
    {
        var cartJson = JsonSerializer.Serialize(cart);
        await _redisCache.SetStringAsync(cart.UserName, cartJson, cancellationToken);

        return await GetCartAsync(cart.UserName, cancellationToken);
    }

    public async Task<bool> DeleteCartAsync(string userName, CancellationToken cancellationToken = default)
    {
        await _redisCache.RemoveAsync(userName, cancellationToken);
        return true;
    }
}
