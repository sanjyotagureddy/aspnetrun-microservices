using Cart.Api.Data;
using Cart.Api.Events;
using Cart.Api.Features.Cart;
using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Messaging;
using Discount.Grpc.Protos;
using FluentValidation;
using MediatR;
using Serilog;

using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using Cart.Api.Observability;

namespace Cart.Api;

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

        builder.Services.AddConfiguredCommonSharedKernelLogging(builder.Configuration, "cart-api");
        builder.Services.UseRequestLoggingMiddleware<CartRequestLoggingMiddleware>();

        // Add Redis Distributed Cache
        var redisConnectionString = builder.Configuration.GetConnectionString("cartdb")
                                     ?? builder.Configuration.GetConnectionString("Redis")
                                     ?? "localhost:6379";

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "Cart_";
        });

        // Add gRPC Discount Client
        builder.Services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>(options =>
        {
            var discountUrl = builder.Configuration["Services:Discount:BaseUrl"]
                              ?? builder.Configuration.GetConnectionString("discount-grpc")
                              ?? "http://discount-grpc";
            if (discountUrl.StartsWith("https+http://", StringComparison.OrdinalIgnoreCase))
            {
                discountUrl = discountUrl.Replace("https+http://", "http://", StringComparison.OrdinalIgnoreCase);
            }
            options.Address = new Uri(discountUrl, UriKind.Absolute);
        })
        .AddServiceDiscovery()
        .AddStandardResilienceHandler();

        // Add Core Infrastructure & MediatR
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddScoped<ICartRepository, CartRepository>();

        builder.Services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
        });

        builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.SharedKernel.Validation.ValidationBehavior<,>));

        builder.Services.AddMessaging(msgBuilder =>
        {
            msgBuilder.ApplyConfiguration(builder.Configuration, "cart-api");
            msgBuilder.RegisterDestination(dest =>
            {
                dest.DestinationName = BasketCheckoutIntegrationEvent.Topic;
                dest.OwnerService = "cart-api";
                dest.Contract = new MessageContractDescriptor("CartCheckoutEvent", "1.0", "application/json");
                dest.PartitioningStrategy = PartitioningStrategy.ByAggregateId;
                dest.PartitionKeySelector = "payload.userName";
            });
        });

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();
        app.UseAuthorization();

        app.MapCartEndpoints();

        app.Run();
    }
}
