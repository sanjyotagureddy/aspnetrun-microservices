using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging;
using Common.SharedKernel.Observability.Context;
using Common.SharedKernel.Web;
using FluentValidation;
using MediatR;
using Npgsql;
using Order.Api.Data;
using Order.Api.Events;
using Order.Api.Features.Orders;
using Order.Api.Messaging;
using Order.Api.Observability;
using Serilog;

namespace Order.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Host.UseSerilog(
            (context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(),
            writeToProviders: true);

        builder.Services.AddConfiguredCommonSharedKernelLogging(builder.Configuration, "order-api");
        builder.Services.UseRequestLoggingMiddleware<OrderRequestLoggingMiddleware>();

        var connectionString = builder.Configuration.GetConnectionString("ordersdb")
                               ?? builder.Configuration.GetConnectionString("orders")
                               ?? throw new ConfigurationException("Connection string 'ordersdb' or 'orders' missing.");

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        builder.Services.AddHostedService<OrderDbInitializer>();
        builder.Services.AddScoped<IOrderRepository, OrderRepository>();

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });

        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.SharedKernel.Validation.ValidationBehavior<,>));

        builder.Services.AddMessaging(msgBuilder =>
        {
            msgBuilder.ApplyConfiguration(builder.Configuration, "order-api");
            msgBuilder.RegisterDestination(dest =>
            {
                dest.DestinationName = OrderCreatedIntegrationEvent.Topic;
                dest.OwnerService = "order-api";
                dest.Contract = new MessageContractDescriptor("OrderLifecycleEvent", "1.0", "application/json");
                dest.PartitioningStrategy = PartitioningStrategy.ByAggregateId;
                dest.PartitionKeySelector = "payload.orderId";
            });
        });

        builder.Services.AddSingleton<BasketCheckoutIntegrationEventHandler>();
        builder.Services.AddHostedService<BasketCheckoutConsumer>();

        builder.Services.AddAuthorization();
        builder.Services.AddSwaggerSupport(builder.Configuration, "Order API");
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        app.MapDefaultEndpoints();
        app.UseSwaggerSupport("Order API");
        app.UseForwardedHeaders();
        app.UseAppCallContextMiddleware<OrderAppCallContextMiddlewareBase>();
        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment()) app.UseHsts();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapOrderEndpoints();

        app.Run();
    }
}
