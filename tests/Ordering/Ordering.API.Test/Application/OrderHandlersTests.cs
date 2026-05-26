using AutoMapper;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Exceptions;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.DeleteCommand;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrdersList;
using Ordering.Application.Mappings;
using Ordering.Domain.Entities;

using Xunit;

namespace Ordering.API.Test.Application;

public sealed class OrderHandlersTests
{
    private static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    [Fact]
    public async Task CheckoutOrderHandler_CreatesOrderAndSendsEmail()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Strict);
        IMapper mapper = CreateMapper();

        var command = new CheckoutOrderCommand
        {
            UserName = "swn",
            TotalPrice = 50,
            EmailAddress = "user@example.com"
        };

        repo.Setup(r => r.AddAsync(It.Is<Order>(order => order.UserName == "swn" && order.TotalPrice == 50)))
            .ReturnsAsync(new TestOrder(17) { UserName = "swn", TotalPrice = 50, EmailAddress = "user@example.com" });

        emailService.Setup(e => e.SendEmail(It.IsAny<Ordering.Application.Models.Email>()))
            .ReturnsAsync(true);

        var handler = new CheckoutOrderCommandHandler(repo.Object, mapper, emailService.Object, NullLogger<CheckoutOrderCommandHandler>.Instance);

        int createdId = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(17, createdId);
        repo.VerifyAll();
        emailService.VerifyAll();
    }

    [Fact]
    public async Task CheckoutOrderHandler_WhenEmailFails_StillReturnsOrderId()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        var emailService = new Mock<IEmailService>(MockBehavior.Strict);
        IMapper mapper = CreateMapper();

        var command = new CheckoutOrderCommand
        {
            UserName = "swn",
            TotalPrice = 50,
            EmailAddress = "user@example.com"
        };

        repo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync(new TestOrder(18) { UserName = "swn", TotalPrice = 50, EmailAddress = "user@example.com" });

        emailService.Setup(e => e.SendEmail(It.IsAny<Ordering.Application.Models.Email>()))
            .ThrowsAsync(new InvalidOperationException("smtp unavailable"));

        var logger = new Mock<ILogger<CheckoutOrderCommandHandler>>();
        var handler = new CheckoutOrderCommandHandler(repo.Object, mapper, emailService.Object, logger.Object);

        int createdId = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(18, createdId);
        repo.VerifyAll();
        emailService.VerifyAll();
    }

    [Fact]
    public async Task UpdateOrderHandler_UpdatesExistingOrder()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        IMapper mapper = CreateMapper();

        var existing = new TestOrder(3) { UserName = "old", TotalPrice = 10, EmailAddress = "old@example.com" };
        var command = new UpdateOrderCommand
        {
            Id = 3,
            UserName = "new-user",
            TotalPrice = 22,
            EmailAddress = "new@example.com"
        };

        repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(existing);
        repo.Setup(r => r.UpdateAsync(existing)).Returns(Task.CompletedTask);

        var handler = new UpdateOrderCommandHandler(repo.Object, mapper, NullLogger<UpdateOrderCommandHandler>.Instance);

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("new-user", existing.UserName);
        Assert.Equal(22, existing.TotalPrice);
        Assert.Equal("new@example.com", existing.EmailAddress);
        repo.VerifyAll();
    }

    [Fact]
    public async Task UpdateOrderHandler_WhenOrderMissing_ThrowsNotFound()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        IMapper mapper = CreateMapper();

        repo.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Order)null);

        var handler = new UpdateOrderCommandHandler(repo.Object, mapper, NullLogger<UpdateOrderCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new UpdateOrderCommand { Id = 404 }, CancellationToken.None));

        repo.VerifyAll();
    }

    [Fact]
    public async Task DeleteOrderHandler_DeletesWhenFound()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);

        var existing = new TestOrder(5) { UserName = "swn" };

        repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(existing);
        repo.Setup(r => r.DeleteAsync(existing)).Returns(Task.CompletedTask);

        var handler = new DeleteOrderCommandHandler(repo.Object, NullLogger<DeleteOrderCommandHandler>.Instance);

        await handler.Handle(new DeleteOrderCommand { Id = 5 }, CancellationToken.None);

        repo.VerifyAll();
    }

    [Fact]
    public async Task DeleteOrderHandler_WhenOrderMissing_ThrowsNotFound()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        repo.Setup(r => r.GetByIdAsync(100)).ReturnsAsync((Order)null);

        var handler = new DeleteOrderCommandHandler(repo.Object, NullLogger<DeleteOrderCommandHandler>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new DeleteOrderCommand { Id = 100 }, CancellationToken.None));

        repo.VerifyAll();
    }

    [Fact]
    public async Task GetOrdersListHandler_ReturnsMappedOrders()
    {
        var repo = new Mock<IOrderRepository>(MockBehavior.Strict);
        IMapper mapper = CreateMapper();

        repo.Setup(r => r.GetOrdersByUserName("swn"))
            .ReturnsAsync(new[] { new TestOrder(9) { UserName = "swn", TotalPrice = 34m } });

        var handler = new GetOrdersListQueryHandler(repo.Object, mapper);

        List<OrdersVm> result = await handler.Handle(new GetOrdersListQuery("swn"), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(9, result[0].Id);
        Assert.Equal("swn", result[0].UserName);
        Assert.Equal(34, result[0].TotalPrice);
        repo.VerifyAll();
    }

    private sealed class TestOrder : Order
    {
        public TestOrder(int id)
        {
            Id = id;
        }
    }
}
