using AutoMapper;

using Microsoft.Extensions.Logging.Abstractions;

using Ordering.API.Mappings;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;

using Shared.Messaging.Events;

using Xunit;

namespace Ordering.API.Test;

public sealed class OrderingProfileTests
{
    [Fact]
    public void MappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<OrderingProfile>(), NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void BasketCheckoutEvent_MapsToCheckoutOrderCommand()
    {
        var configuration = new MapperConfiguration(config => config.AddProfile<OrderingProfile>(), NullLoggerFactory.Instance);
        IMapper mapper = configuration.CreateMapper();

        var source = new BasketCheckoutEvent
        {
            UserName = "swn",
            TotalPrice = 42,
            CardNumber = "1111",
            CVV = "123"
        };

        CheckoutOrderCommand command = mapper.Map<CheckoutOrderCommand>(source);

        Assert.Equal(source.UserName, command.UserName);
        Assert.Equal(source.TotalPrice, command.TotalPrice);
        Assert.Equal(source.CardNumber, command.CardNumber);
        Assert.Equal(source.CVV, command.CVV);
    }
}