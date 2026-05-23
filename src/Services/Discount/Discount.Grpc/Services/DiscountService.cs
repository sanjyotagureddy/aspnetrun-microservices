using AutoMapper;
using Discount.Grpc.Application.Features.Discounts.Commands.CreateDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.DeleteDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.UpdateDiscount;
using Discount.Grpc.Application.Features.Discounts.Queries.GetDiscount;
using Discount.Grpc.Entities;
using Discount.Grpc.Protos;
using Grpc.Core;
using MediatR;

namespace Discount.Grpc.Services;

public class DiscountService(IMediator mediator, ILogger<DiscountService> logger, IMapper mapper)
  : DiscountProtoService.DiscountProtoServiceBase
{
  private readonly ILogger<DiscountService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

  public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    var coupon = await _mediator.Send(new GetDiscountQuery(request.ProductName), cancellationToken);
    if (coupon == null)
    {
      _logger.LogError($"Discount with ProductName= {request.ProductName} Not found.");
      throw new RpcException(new Status(StatusCode.NotFound,
        $"Discount with ProductName= {request.ProductName} Not found."));
    }

    _logger.LogInformation("Discount is retrieved for ProductName : {productName}, Amount : {amount}",
      coupon.ProductName, coupon.Amount);
    var couponModel = _mapper.Map<CouponModel>(coupon);
    return couponModel;
  }

  public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    var coupon = _mapper.Map<Coupon>(request.Coupon);

    coupon = await _mediator.Send(new CreateDiscountCommand(coupon), cancellationToken);
    _logger.LogInformation("Discount is successfully created. ProductName : {ProductName}", coupon.ProductName);

    var couponModel = _mapper.Map<CouponModel>(coupon);
    return couponModel;
  }

  public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    var coupon = _mapper.Map<Coupon>(request.Coupon);

    coupon = await _mediator.Send(new UpdateDiscountCommand(coupon), cancellationToken);
    _logger.LogInformation("Discount is successfully updated. ProductName : {ProductName}", coupon.ProductName);

    var couponModel = _mapper.Map<CouponModel>(coupon);
    return couponModel;
  }

  public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request,
    ServerCallContext context)
  {
    var cancellationToken = context?.CancellationToken ?? CancellationToken.None;
    var deleted = await _mediator.Send(new DeleteDiscountCommand(request.ProductName), cancellationToken);
    var response = new DeleteDiscountResponse
    {
      Success = deleted
    };

    return response;
  }
}