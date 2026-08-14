using Discount.Grpc.Data;
using Discount.Grpc.Domain;
using Discount.Grpc.Protos;
using Discount.Grpc.Services;
using FluentAssertions;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Discount.Grpc.Tests;

public class DiscountServiceTests
{
    private readonly IDiscountRepository _repository = Substitute.For<IDiscountRepository>();
    private readonly ILogger<DiscountService> _logger = Substitute.For<ILogger<DiscountService>>();
    private readonly DiscountService _service;

    public DiscountServiceTests()
    {
        _service = new DiscountService(_repository, _logger);
    }

    [Fact]
    public async Task GetDiscount_ShouldReturnCouponModel_WhenDiscountExists()
    {
        // Arrange
        var coupon = new Coupon { Id = 1, ProductName = "IPhone X", Description = "IPhone Discount", Amount = 150 };
        _repository.GetDiscountAsync("IPhone X", Arg.Any<CancellationToken>())
            .Returns(coupon);

        var request = new GetDiscountRequest { ProductName = "IPhone X" };
        var context = Substitute.For<ServerCallContext>();

        // Act
        var result = await _service.GetDiscount(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.ProductName.Should().Be("IPhone X");
        result.Amount.Should().Be(150);
        result.Description.Should().Be("IPhone Discount");
    }

    [Fact]
    public async Task CreateDiscount_ShouldReturnCouponModel_WhenSuccessful()
    {
        // Arrange
        var coupon = new Coupon { Id = 2, ProductName = "Samsung S10", Description = "Samsung Discount", Amount = 100 };
        _repository.CreateDiscountAsync(Arg.Any<Coupon>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repository.GetDiscountAsync("Samsung S10", Arg.Any<CancellationToken>())
            .Returns(coupon);

        var request = new CreateDiscountRequest
        {
            Coupon = new CouponModel { ProductName = "Samsung S10", Description = "Samsung Discount", Amount = 100 }
        };
        var context = Substitute.For<ServerCallContext>();

        // Act
        var result = await _service.CreateDiscount(request, context);

        // Assert
        result.Should().NotBeNull();
        result.ProductName.Should().Be("Samsung S10");
        result.Amount.Should().Be(100);
    }

    [Fact]
    public async Task CreateDiscount_ShouldThrowRpcException_WhenCreationFailed()
    {
        // Arrange
        _repository.CreateDiscountAsync(Arg.Any<Coupon>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new CreateDiscountRequest
        {
            Coupon = new CouponModel { ProductName = "FailProduct", Description = "Desc", Amount = 50 }
        };
        var context = Substitute.For<ServerCallContext>();

        // Act
        Func<Task> act = async () => await _service.CreateDiscount(request, context);

        // Assert
        await act.Should().ThrowAsync<RpcException>()
            .WithMessage("*Failed to create discount*");
    }

    [Fact]
    public async Task UpdateDiscount_ShouldReturnCouponModel_WhenSuccessful()
    {
        // Arrange
        var coupon = new Coupon { Id = 1, ProductName = "IPhone X", Description = "Updated", Amount = 200 };
        _repository.UpdateDiscountAsync(Arg.Any<Coupon>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repository.GetDiscountAsync("IPhone X", Arg.Any<CancellationToken>())
            .Returns(coupon);

        var request = new UpdateDiscountRequest
        {
            Coupon = new CouponModel { Id = 1, ProductName = "IPhone X", Description = "Updated", Amount = 200 }
        };
        var context = Substitute.For<ServerCallContext>();

        // Act
        var result = await _service.UpdateDiscount(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Amount.Should().Be(200);
        result.Description.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateDiscount_ShouldThrowRpcException_WhenUpdateFailed()
    {
        // Arrange
        _repository.UpdateDiscountAsync(Arg.Any<Coupon>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var request = new UpdateDiscountRequest
        {
            Coupon = new CouponModel { Id = 99, ProductName = "NonExistent", Description = "Desc", Amount = 10 }
        };
        var context = Substitute.For<ServerCallContext>();

        // Act
        Func<Task> act = async () => await _service.UpdateDiscount(request, context);

        // Assert
        await act.Should().ThrowAsync<RpcException>();
    }

    [Fact]
    public async Task DeleteDiscount_ShouldReturnSuccessResponse()
    {
        // Arrange
        _repository.DeleteDiscountAsync("IPhone X", Arg.Any<CancellationToken>())
            .Returns(true);

        var request = new DeleteDiscountRequest { ProductName = "IPhone X" };
        var context = Substitute.For<ServerCallContext>();

        // Act
        var result = await _service.DeleteDiscount(request, context);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }
}
