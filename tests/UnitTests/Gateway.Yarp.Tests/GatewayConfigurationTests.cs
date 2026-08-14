using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Gateway.Yarp.Tests;

public class GatewayConfigurationTests
{
    [Fact]
    public void GatewayConfiguration_ShouldContainAllRequiredRoutesAndClusters()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var reverseProxySection = configuration.GetSection("ReverseProxy");

        // Act & Assert
        reverseProxySection.Exists().Should().BeTrue();

        var routes = reverseProxySection.GetSection("Routes");
        routes.GetSection("products-route").Exists().Should().BeTrue();
        routes.GetSection("inventory-route").Exists().Should().BeTrue();
        routes.GetSection("cart-route").Exists().Should().BeTrue();
        routes.GetSection("orders-route").Exists().Should().BeTrue();

        var clusters = reverseProxySection.GetSection("Clusters");
        clusters.GetSection("products-cluster").Exists().Should().BeTrue();
        clusters.GetSection("inventory-cluster").Exists().Should().BeTrue();
        clusters.GetSection("cart-cluster").Exists().Should().BeTrue();
        clusters.GetSection("orders-cluster").Exists().Should().BeTrue();
    }

    [Fact]
    public void GatewayConfiguration_ShouldMapExpectedPathPatterns()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        // Act
        var productsPath = configuration["ReverseProxy:Routes:products-route:Match:Path"];
        var inventoryPath = configuration["ReverseProxy:Routes:inventory-route:Match:Path"];
        var cartPath = configuration["ReverseProxy:Routes:cart-route:Match:Path"];
        var ordersPath = configuration["ReverseProxy:Routes:orders-route:Match:Path"];

        // Assert
        productsPath.Should().Be("/api/v1/products/{**catch-all}");
        inventoryPath.Should().Be("/api/v1/inventory/{**catch-all}");
        cartPath.Should().Be("/api/v1/cart/{**catch-all}");
        ordersPath.Should().Be("/api/v1/orders/{**catch-all}");
    }
}
