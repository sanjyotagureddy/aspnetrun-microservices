namespace Products.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddProductsApi(builder.Configuration);
        builder.Services.AddAuthorization();
        builder.Services.AddSwaggerSupport(builder.Configuration, "Products API");

        var app = builder.Build();

        app.MapDefaultEndpoints();
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerSupport("Products API");
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapDiscoveredEndpoints();
        app.Run();
    }
}
