using Microsoft.EntityFrameworkCore;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Repositories;
using Ordering.Domain.Entities;
using Xunit;

namespace Ordering.API.Test.Repositories;

public sealed class OrderRepositoryTests
{
    private static OrderContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<OrderContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new OrderContext(options);
    }

    [Fact]
    public async Task RepositoryBase_Add_GetById_GetAll_Update_Delete_Works()
    {
        using var context = CreateContext("repo_test_db1");
        var repo = new RepositoryBase<Order, int>(context);

        var order = new Order
        {
            UserName = "alice",
            TotalPrice = 10m,
            FirstName = "Alice",
            LastName = "Smith",
            EmailAddress = "alice@example.com",
            AddressLine = "123 Lane",
            Country = "USA"
        };
        var added = await repo.AddAsync(order);

        Assert.Equal(order, added);
        Assert.NotEqual(0, added.Id);

        var fetched = await repo.GetByIdAsync(added.Id);
        Assert.Equal(added.Id, fetched.Id);

        var all = await repo.GetAllAsync();
        Assert.Contains(all, o => o.Id == added.Id);

        added.TotalPrice = 20m;
        await repo.UpdateAsync(added);

        var updated = await repo.GetByIdAsync(added.Id);
        Assert.Equal(20m, updated.TotalPrice);

        await repo.DeleteAsync(added);
        var afterDelete = await repo.GetAllAsync();
        Assert.DoesNotContain(afterDelete, o => o.Id == added.Id);
    }

    [Fact]
    public async Task OrderRepository_GetOrdersByUserName_ReturnsOnlyUserOrders()
    {
        using var context = CreateContext("repo_test_db2");
        var repo = new OrderRepository(context);

        var o1 = new Order { UserName = "bob", TotalPrice = 5m, FirstName = "B", LastName = "One", EmailAddress = "b1@example.com", AddressLine = "Addr1", Country = "USA" };
        var o2 = new Order { UserName = "bob", TotalPrice = 7m, FirstName = "B", LastName = "Two", EmailAddress = "b2@example.com", AddressLine = "Addr2", Country = "USA" };
        var o3 = new Order { UserName = "carol", TotalPrice = 3m, FirstName = "C", LastName = "Three", EmailAddress = "c@example.com", AddressLine = "Addr3", Country = "USA" };

        await repo.AddAsync(o1);
        await repo.AddAsync(o2);
        await repo.AddAsync(o3);

        var bobs = await repo.GetOrdersByUserName("bob");
        Assert.Equal(2, bobs.Count());
        Assert.All(bobs, o => Assert.Equal("bob", o.UserName));
    }
}
