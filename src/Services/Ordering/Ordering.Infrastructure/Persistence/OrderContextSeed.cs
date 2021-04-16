using Microsoft.Extensions.Logging;
using Ordering.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Persistence
{
    public class OrderContextSeed
    {
        public static async Task SeedAsync(OrderContext orderContext, ILogger<OrderContextSeed> logger)
        {
            if (!orderContext.Orders.Any())
            {
                await orderContext.Orders.AddRangeAsync(GetPreconfiguredOrders());
                await orderContext.SaveChangesAsync();

                logger.LogInformation("Seed database associated with context {DbContextName}", typeof(OrderContext).Name);
            }
        }

        private static IEnumerable<Order> GetPreconfiguredOrders()
        {
            return new List<Order>
            {
                new() { UserName = "swn", FirstName = "Sanjyot", LastName = "Agureddy", EmailAddress = "sanjyot.agureddy@hotmail.com", AddressLine = "Pune", Country = "India", TotalPrice = 350 }
            };
        }
    }
}