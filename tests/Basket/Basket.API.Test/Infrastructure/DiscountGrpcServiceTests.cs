using Basket.API.Infrastructure.Services;
using Grpc.Core;
using Discount.Grpc.Protos;
using Moq;
using Xunit;

namespace Basket.API.Test.Infrastructure;

public sealed class DiscountGrpcServiceTests
{
    [Fact]
    public async Task GetDiscountAsync_ForwardsRequestAndReturnsAmount()
    {
        var client = new Mock<DiscountProtoService.DiscountProtoServiceClient>();
        var service = new DiscountGrpcService(client.Object);
        var coupon = new CouponModel { ProductName = "book", Amount = 3 };

        client.Setup(c => c.GetDiscountAsync(
                It.Is<GetDiscountRequest>(request => request.ProductName == "book"),
                null,
                null,
                It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(coupon));

        decimal discount = await service.GetDiscountAsync("book", CancellationToken.None);

        Assert.Equal(3m, discount);
        client.VerifyAll();
    }

    private static AsyncUnaryCall<CouponModel> CreateAsyncUnaryCall(CouponModel response)
    {
        return new AsyncUnaryCall<CouponModel>(
            Task.FromResult(response),
            Task.FromResult(new Metadata()),
            () => Status.DefaultSuccess,
            () => new Metadata(),
            () => { });
    }
}