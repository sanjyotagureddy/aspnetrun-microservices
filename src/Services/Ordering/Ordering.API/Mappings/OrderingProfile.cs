using AutoMapper;

using Ordering.Application.Features.Orders.Commands.CheckoutOrder;

using Shared.Messaging.Events;

namespace Ordering.API.Mappings;

public class OrderingProfile : Profile
{
  public OrderingProfile()
  {
    CreateMap<CheckoutOrderCommand, BasketCheckoutEvent>().ReverseMap();
  }
}