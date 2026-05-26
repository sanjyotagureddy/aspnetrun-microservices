using AutoMapper;

using MediatR;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using Ordering.Application;
using Ordering.Application.Behaviors;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.DeleteCommand;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrdersList;
using Ordering.Application.Mappings;
using Ordering.Application.Models;
using Ordering.Domain.Entities;

using Xunit;

namespace Ordering.API.Test.Application;

public sealed class ApplicationCompositionAndModelsTests
{
    [Fact]
    public void ApplicationServiceRegistration_RegistersValidatorsAndPipelineBehaviors()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining<CheckoutOrderCommand>());

        services.AddApplicationServices();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<FluentValidation.IValidator<CheckoutOrderCommand>>());
        Assert.NotNull(provider.GetService<FluentValidation.IValidator<UpdateOrderCommand>>());

        var checkoutBehaviors = provider.GetServices<IPipelineBehavior<CheckoutOrderCommand, int>>().ToList();
        Assert.Contains(checkoutBehaviors, behavior => behavior.GetType().Name.Contains(nameof(ValidationBehavior<CheckoutOrderCommand, int>).Split('`')[0]));
        Assert.Contains(checkoutBehaviors, behavior => behavior.GetType().Name.Contains(nameof(UnhandledExceptionBehavior<CheckoutOrderCommand, int>).Split('`')[0]));
    }

    [Fact]
    public void MappingProfile_MapsOrderToQueryVm()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();

        IMapper mapper = config.CreateMapper();
        var order = new TestOrder(1)
        {
            UserName = "swn",
            TotalPrice = 99,
            EmailAddress = "user@example.com",
            FirstName = "first",
            LastName = "last"
        };

        OrdersVm vm = mapper.Map<OrdersVm>(order);

        Assert.Equal(order.Id, vm.Id);
        Assert.Equal(order.UserName, vm.UserName);
        Assert.Equal(order.TotalPrice, vm.TotalPrice);
        Assert.Equal(order.EmailAddress, vm.EmailAddress);
        Assert.Equal(order.FirstName, vm.FirstName);
        Assert.Equal(order.LastName, vm.LastName);
    }

    [Fact]
    public void CommandsAndModels_RoundTripProperties()
    {
        var checkout = new CheckoutOrderCommand
        {
            UserName = "swn",
            TotalPrice = 14,
            FirstName = "John",
            LastName = "Doe",
            EmailAddress = "john@doe.com",
            AddressLine = "street",
            Country = "US",
            State = "CA",
            ZipCode = "12345",
            CardName = "John Doe",
            CardNumber = "4242",
            Expiration = "12/30",
            CVV = "123",
            PaymentMethod = 1
        };

        var update = new UpdateOrderCommand
        {
            Id = 3,
            UserName = checkout.UserName,
            TotalPrice = checkout.TotalPrice,
            FirstName = checkout.FirstName,
            LastName = checkout.LastName,
            EmailAddress = checkout.EmailAddress,
            AddressLine = checkout.AddressLine,
            Country = checkout.Country,
            State = checkout.State,
            ZipCode = checkout.ZipCode,
            CardName = checkout.CardName,
            CardNumber = checkout.CardNumber,
            Expiration = checkout.Expiration,
            CVV = checkout.CVV,
            PaymentMethod = checkout.PaymentMethod
        };

        var vm = new OrdersVm
        {
            Id = 7,
            UserName = checkout.UserName,
            TotalPrice = checkout.TotalPrice,
            FirstName = checkout.FirstName,
            LastName = checkout.LastName,
            EmailAddress = checkout.EmailAddress,
            AddressLine = checkout.AddressLine,
            Country = checkout.Country,
            State = checkout.State,
            ZipCode = checkout.ZipCode,
            CardName = checkout.CardName,
            CardNumber = checkout.CardNumber,
            Expiration = checkout.Expiration,
            CVV = checkout.CVV,
            PaymentMethod = checkout.PaymentMethod
        };

        var email = new Email { To = "user@example.com", Subject = "subject", Body = "body" };
        var emailSettings = new EmailSettings { ApiKey = "key", FromAddress = "from@example.com", FromName = "ordering" };

        Assert.Equal("swn", checkout.UserName);
        Assert.Equal(14, checkout.TotalPrice);
        Assert.Equal(3, update.Id);
        Assert.Equal("john@doe.com", update.EmailAddress);
        Assert.Equal(7, vm.Id);
        Assert.Equal("John Doe", vm.CardName);
        Assert.Equal("user@example.com", email.To);
        Assert.Equal("subject", email.Subject);
        Assert.Equal("body", email.Body);
        Assert.Equal("key", emailSettings.ApiKey);
        Assert.Equal("from@example.com", emailSettings.FromAddress);
        Assert.Equal("ordering", emailSettings.FromName);
    }

    [Fact]
    public void QueryAndDeleteCommand_CanBeConstructed()
    {
        var query = new GetOrdersListQuery("swn");
        var command = new DeleteOrderCommand { Id = 9 };

        Assert.Equal("swn", query.UserName);
        Assert.Equal(9, command.Id);
    }

    [Fact]
    public void GetOrdersListQuery_ThrowsForNullUsername()
    {
        Assert.Throws<ArgumentNullException>(() => new GetOrdersListQuery(null));
    }

    private sealed class TestOrder : Order
    {
        public TestOrder(int id)
        {
            Id = id;
        }
    }
}
