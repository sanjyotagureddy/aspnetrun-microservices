using Cart.Api.Domain;

namespace Cart.Api.Data;

public interface ICartRepository
{
    Task<ShoppingCart> GetCartAsync(string userName, CancellationToken cancellationToken = default);
    Task<ShoppingCart> UpdateCartAsync(ShoppingCart cart, CancellationToken cancellationToken = default);
    Task<bool> DeleteCartAsync(string userName, CancellationToken cancellationToken = default);
}
