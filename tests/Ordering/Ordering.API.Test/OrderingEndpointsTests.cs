using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using SharedKernel.Exceptions;
using Microsoft.AspNetCore.Routing;

using Moq;

using Ordering.API.Endpoints;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.DeleteCommand;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrdersList;

using Xunit;

namespace Ordering.API.Test;

public sealed class OrderingEndpointsTests
{
    [Fact]
    public void MapEndpoints_RegistersRoutes()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();
        IEndpointRouteBuilder routeBuilder = app;

        var endpoint = new OrderingEndpoints();
        endpoint.MapEndpoints(app);

        int endpointCount = routeBuilder.DataSources.SelectMany(source => source.Endpoints).Count();
        Assert.True(endpointCount >= 4);
    }

    [Fact]
    public async Task GetOrdersByUserName_ReturnsOkWithOrders()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var expected = new List<OrdersVm> { new() { UserName = "swn", TotalPrice = 5 } };

        mediator.Setup(m => m.Send(It.Is<GetOrdersListQuery>(query => query.UserName == "swn"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        Ok<IEnumerable<OrdersVm>> result = await OrderingEndpoints.GetOrdersByUserName(mediator.Object, "swn", CancellationToken.None);

        Assert.Single(result.Value);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task CheckoutOrder_WhenIdempotencyKeyMissing_ThrowsValidationException()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var context = new DefaultHttpContext();
        var command = new CheckoutOrderCommand { UserName = "swn", CardNumber = "1234" };
        await Assert.ThrowsAsync<ValidationException>(async () => await OrderingEndpoints.CheckoutOrder(mediator.Object, context, command, CancellationToken.None));
        mediator.Verify(m => m.Send(It.IsAny<CheckoutOrderCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutOrder_WhenIdempotencyKeyPresent_ReturnsOrderId()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var context = new DefaultHttpContext();
        context.Request.Headers.Append("Idempotency-Key", "abc-123");

        var command = new CheckoutOrderCommand { UserName = "swn", CardNumber = "1234" };

        mediator.Setup(m => m.Send(It.Is<CheckoutOrderCommand>(value => value == command), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        Ok<int> ok = await OrderingEndpoints.CheckoutOrder(mediator.Object, context, command, CancellationToken.None);
        Assert.Equal(42, ok.Value);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task UpdateOrder_ReturnsNoContent()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var command = new UpdateOrderCommand { Id = 7, UserName = "swn" };

        mediator.Setup(m => m.Send(It.Is<UpdateOrderCommand>(value => value == command), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        NoContent result = await OrderingEndpoints.UpdateOrder(mediator.Object, command, CancellationToken.None);

        Assert.NotNull(result);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task DeleteOrder_ReturnsNoContent()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        mediator.Setup(m => m.Send(It.Is<DeleteOrderCommand>(command => command.Id == 11), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        NoContent result = await OrderingEndpoints.DeleteOrder(mediator.Object, 11, CancellationToken.None);

        Assert.NotNull(result);
        mediator.VerifyAll();
    }
}
