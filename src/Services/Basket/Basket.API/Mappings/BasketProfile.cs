using AutoMapper;

using Basket.API.Entities;

using Shared.Messaging.Events;

namespace Basket.API.Mappings;

public class BasketProfile : Profile
{
  public BasketProfile()
  {
    CreateMap<BasketCheckout, BasketCheckoutEvent>().ReverseMap();
  }
}