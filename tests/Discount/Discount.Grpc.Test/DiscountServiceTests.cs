using AutoMapper;
using Discount.Grpc.Application.Features.Discounts.Commands.CreateDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.DeleteDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.UpdateDiscount;
using Discount.Grpc.Application.Features.Discounts.Queries.GetDiscount;
using Discount.Grpc.Entities;
using Discount.Grpc.Mapper;
using Discount.Grpc.Protos;
using Discount.Grpc.Services;
using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Discount.Grpc.Test;

public class DiscountServiceTests
{
    private static IMapper CreateMapper()
    {
        var mapperConfiguration = new MapperConfiguration(
            configuration => configuration.AddProfile(new DiscountProfile()),
            NullLoggerFactory.Instance);
        return mapperConfiguration.CreateMapper();
    }

    [Fact]
    public async Task GetDiscount_ReturnsMappedCoupon()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<DiscountService>>();
        var mapper = CreateMapper();
        var coupon = new Coupon { Id = 1, ProductName = "IPhone X", Description = "IPhone Discount", Amount = 150 };

        mediator.Setup(mediator => mediator.Send(
                It.Is<GetDiscountQuery>(query => query.ProductName == "IPhone X"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var service = new DiscountService(mediator.Object, logger.Object, mapper);

        var result = await service.GetDiscount(new GetDiscountRequest { ProductName = "IPhone X" }, null!);

        Assert.Equal(coupon.ProductName, result.ProductName);
        Assert.Equal(coupon.Description, result.Discription);
        Assert.Equal(coupon.Amount, result.Amount);
        mediator.Verify(mediator => mediator.Send(
                It.Is<GetDiscountQuery>(query => query.ProductName == "IPhone X"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void GetDiscount_ThrowsNotFoundWhenCouponMissing()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<DiscountService>>();
        var mapper = CreateMapper();

        mediator.Setup(mediator => mediator.Send(It.IsAny<GetDiscountQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Coupon?)null);

        var service = new DiscountService(mediator.Object, logger.Object, mapper);

        var exception = await Assert.ThrowsAsync<RpcException>(() =>
            service.GetDiscount(new GetDiscountRequest { ProductName = "Missing" }, null!));

        Assert.Equal(StatusCode.NotFound, exception?.Result?.StatusCode);
    }

    [Fact]
    public async Task CreateDiscount_MapsRequestAndReturnsCreatedCoupon()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<DiscountService>>();
        var mapper = CreateMapper();
        var coupon = new Coupon { Id = 3, ProductName = "Pixel", Description = "Phone Discount", Amount = 50 };

        mediator.Setup(mediator => mediator.Send(
                It.Is<CreateDiscountCommand>(command =>
                    command.Coupon.ProductName == coupon.ProductName &&
                    command.Coupon.Description == coupon.Description &&
                    command.Coupon.Amount == coupon.Amount),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var service = new DiscountService(mediator.Object, logger.Object, mapper);

        var result = await service.CreateDiscount(new CreateDiscountRequest
        {
            Coupon = new CouponModel
            {
                ProductName = coupon.ProductName,
                Discription = coupon.Description,
                Amount = coupon.Amount
            }
        }, null!);

        Assert.Equal(coupon.ProductName, result.ProductName);
        Assert.Equal(coupon.Description, result.Discription);
        Assert.Equal(coupon.Amount, result.Amount);
    }

    [Fact]
    public async Task UpdateDiscount_MapsRequestAndReturnsUpdatedCoupon()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<DiscountService>>();
        var mapper = CreateMapper();
        var coupon = new Coupon { Id = 3, ProductName = "Pixel", Description = "Updated Discount", Amount = 75 };

        mediator.Setup(mediator => mediator.Send(
                It.Is<UpdateDiscountCommand>(command =>
                    command.Coupon.ProductName == coupon.ProductName &&
                    command.Coupon.Description == coupon.Description &&
                    command.Coupon.Amount == coupon.Amount),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);

        var service = new DiscountService(mediator.Object, logger.Object, mapper);

        var result = await service.UpdateDiscount(new UpdateDiscountRequest
        {
            Coupon = new CouponModel
            {
                Id = coupon.Id,
                ProductName = coupon.ProductName,
                Discription = coupon.Description,
                Amount = coupon.Amount
            }
        }, null!);

        Assert.Equal(coupon.ProductName, result.ProductName);
        Assert.Equal(coupon.Description, result.Discription);
        Assert.Equal(coupon.Amount, result.Amount);
    }

    [Fact]
    public async Task DeleteDiscount_ReturnsMediatorResult()
    {
        var mediator = new Mock<IMediator>();
        var logger = new Mock<ILogger<DiscountService>>();
        var mapper = CreateMapper();

        mediator.Setup(mediator => mediator.Send(
                It.Is<DeleteDiscountCommand>(command => command.ProductName == "Pixel"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new DiscountService(mediator.Object, logger.Object, mapper);

        var result = await service.DeleteDiscount(new DeleteDiscountRequest { ProductName = "Pixel" }, null!);

        Assert.True(result.Success);
    }
}
