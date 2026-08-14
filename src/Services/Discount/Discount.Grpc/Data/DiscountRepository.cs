using Dapper;
using Discount.Grpc.Domain;
using Microsoft.Data.Sqlite;

namespace Discount.Grpc.Data;

public class DiscountRepository : IDiscountRepository
{
    private readonly string _connectionString;

    public DiscountRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetValue<string>("DatabaseSettings:ConnectionString")
                            ?? "Data Source=discountdb.db";
    }

    public async Task<Coupon> GetDiscountAsync(string productName, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        
        var commandDefinition = new CommandDefinition(
            "SELECT Id, ProductName, Description, Amount FROM Coupons WHERE LOWER(ProductName) = LOWER(@ProductName)",
            new { ProductName = productName },
            cancellationToken: cancellationToken);

        var coupon = await connection.QueryFirstOrDefaultAsync<Coupon>(commandDefinition);

        if (coupon is null)
        {
            return new Coupon
            {
                ProductName = productName,
                Amount = 0,
                Description = "No Discount"
            };
        }

        return coupon;
    }

    public async Task<bool> CreateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var commandDefinition = new CommandDefinition(
            "INSERT INTO Coupons (ProductName, Description, Amount) VALUES (@ProductName, @Description, @Amount)",
            new { coupon.ProductName, coupon.Description, coupon.Amount },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(commandDefinition);
        return affected > 0;
    }

    public async Task<bool> UpdateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var commandDefinition = new CommandDefinition(
            "UPDATE Coupons SET ProductName=@ProductName, Description=@Description, Amount=@Amount WHERE Id=@Id",
            new { coupon.ProductName, coupon.Description, coupon.Amount, coupon.Id },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(commandDefinition);
        return affected > 0;
    }

    public async Task<bool> DeleteDiscountAsync(string productName, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);

        var commandDefinition = new CommandDefinition(
            "DELETE FROM Coupons WHERE LOWER(ProductName) = LOWER(@ProductName)",
            new { ProductName = productName },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(commandDefinition);
        return affected > 0;
    }
}
