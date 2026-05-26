using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Models;
using Ordering.Infrastructure.Mail;
using Ordering.Infrastructure.Repositories;

namespace Ordering.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
      IConfiguration configuration)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
        services.AddDbContext<OrderContext>(options =>
      options.UseSqlServer(configuration.GetConnectionString("OrderingConnectionString")));

        services.AddScoped(typeof(IAsyncRepository<,>), typeof(RepositoryBase<,>));
        services.AddScoped<IOrderRepository, OrderRepository>();

        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddTransient<ISendGridClientWrapper>(_ => new SendGridClientWrapper(configuration.GetSection("EmailSettings").Get<EmailSettings>().ApiKey));
        services.AddTransient<IEmailService, EmailService>();

        return services;
    }
}
