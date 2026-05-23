using AutoMapper;
using Discount.Grpc.Entities;
using Discount.Grpc.Protos;

namespace Discount.Grpc.Mapper;

public sealed class DiscountProfile : Profile
{
  public DiscountProfile()
  {
    CreateMap<Coupon, CouponModel>()
      .ForMember(destination => destination.Discription,
        options => options.MapFrom(source => source.Description));

    CreateMap<CouponModel, Coupon>()
      .ForMember(destination => destination.Description,
        options => options.MapFrom(source => source.Discription));
  }
}