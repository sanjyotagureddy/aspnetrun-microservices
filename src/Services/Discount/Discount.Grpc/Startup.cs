using Discount.Grpc.Mapper;
using Discount.Grpc.Repositories;
using Discount.Grpc.Repositories.Interfaces;
using Discount.Grpc.Services;
using MediatR;
using SharedKernel;
using SharedKernel.Middleware;

namespace Discount.Grpc;

public class Startup
{
  // This method gets called by the runtime. Use this method to add services to the container.
  // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddLogging();
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
    services.AddAutoMapper(cfg => { }, typeof(DiscountProfile));
    services.AddSingleton<IDiscountConnectionFactory, DiscountConnectionFactory>();
    services.AddScoped<ICouponRepository, CouponRepository>();
    services.AddSingleton<IDiscountDatabaseInitializer, DiscountDatabaseInitializer>();
    services.AddGrpc();
    services.AddHealthChecks();
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

    app.UseMiddleware<RequestContextMiddleware>();
    // Global exception handler for the service (returns SharedKernel.Errors.Error payload)
    app.UseGlobalExceptionHandler(Constants.ServiceCodes.Discount);
        app.UseRouting();


    app.UseEndpoints(endpoints =>
    {
      endpoints.MapGrpcService<DiscountService>();
      endpoints.MapHealthChecks("/health");
      endpoints.MapGet("/", WriteDefaultResponseAsync);
    });
  }

  internal static Task WriteDefaultResponseAsync(HttpContext context)
  {
    return context.Response.WriteAsync(
      "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
  }
}
