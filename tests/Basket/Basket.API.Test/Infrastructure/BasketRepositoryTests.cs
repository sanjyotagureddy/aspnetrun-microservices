using Basket.API.Domain.Entities;
using Basket.API.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace Basket.API.Test.Infrastructure;

public sealed class BasketRepositoryTests
{
    [Fact]
    public async Task GetBasketAsync_ReturnsNullWhenCacheIsEmpty()
    {
        var cache = new FakeDistributedCache();
        var repository = new BasketRepository(cache);

        ShoppingCart basket = await repository.GetBasketAsync("alice", CancellationToken.None);

        Assert.Null(basket);
    }

    [Fact]
    public async Task UpdateBasketAsync_WritesAndReadsBasket()
    {
        var cache = new FakeDistributedCache();
        var repository = new BasketRepository(cache);
        var basket = new ShoppingCart("alice")
        {
            Items =
            [
                new ShoppingCartItem { ProductName = "book", Quantity = 1, Price = 20m }
            ]
        };

        ShoppingCart updated = await repository.UpdateBasketAsync(basket, CancellationToken.None);

        Assert.Equal("alice", updated.Id);
        Assert.Single(updated.Items);
        Assert.Equal(basket.Id, cache.LastKey);
        Assert.NotNull(cache.StoredValue);
    }

    [Fact]
    public async Task DeleteBasketAsync_RemovesKey()
    {
        var cache = new FakeDistributedCache();
        var repository = new BasketRepository(cache);

        await repository.DeleteBasketAsync("alice", CancellationToken.None);

        Assert.Equal("alice", cache.RemovedKey);
    }

    private sealed class FakeDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public string LastKey { get; private set; } = string.Empty;

        public string RemovedKey { get; private set; } = string.Empty;

        public string StoredValue { get; private set; } = string.Empty;

        public byte[] Get(string key)
        {
            return _values.TryGetValue(key, out byte[] value) ? value : Array.Empty<byte>();
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _values[key] = value;
            LastKey = key;
            StoredValue = System.Text.Encoding.UTF8.GetString(value);
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _values.Remove(key);
            RemovedKey = key;
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }
    }
}