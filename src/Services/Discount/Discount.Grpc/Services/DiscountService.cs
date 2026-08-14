using Discount.Grpc.Data;
using Discount.Grpc.Domain;
using Discount.Grpc.Protos;
using Grpc.Core;

namespace Discount.Grpc.Services;

public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    private readonly IDiscountRepository _repository;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(IDiscountRepository repository, ILogger<DiscountService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var coupon = await _repository.GetDiscountAsync(request.ProductName, context.CancellationToken);

        _logger.LogInformation("Discount retrieved for ProductName: {ProductName}, Amount: {Amount}",
            coupon.ProductName, coupon.Amount);

        return MapToCouponModel(coupon);
    }

    public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
    {
        var coupon = MapToCoupon(request.Coupon);

        var success = await _repository.CreateDiscountAsync(coupon, context.CancellationToken);
        if (!success)
        {
            _logger.LogWarning("Failed to create discount for ProductName: {ProductName}", coupon.ProductName);
            throw new RpcException(new Status(StatusCode.Internal, $"Failed to create discount for {coupon.ProductName}"));
        }

        _logger.LogInformation("Discount created successfully for ProductName: {ProductName}", coupon.ProductName);

        var createdCoupon = await _repository.GetDiscountAsync(coupon.ProductName, context.CancellationToken);
        return MapToCouponModel(createdCoupon);
    }

    public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
    {
        var coupon = MapToCoupon(request.Coupon);

        var success = await _repository.UpdateDiscountAsync(coupon, context.CancellationToken);
        if (!success)
        {
            _logger.LogWarning("Failed to update discount for ProductName: {ProductName}", coupon.ProductName);
            throw new RpcException(new Status(StatusCode.NotFound, $"Discount for {coupon.ProductName} not found or update failed"));
        }

        _logger.LogInformation("Discount updated successfully for ProductName: {ProductName}", coupon.ProductName);

        var updatedCoupon = await _repository.GetDiscountAsync(coupon.ProductName, context.CancellationToken);
        return MapToCouponModel(updatedCoupon);
    }

    public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
    {
        var deleted = await _repository.DeleteDiscountAsync(request.ProductName, context.CancellationToken);

        _logger.LogInformation("Discount deletion for ProductName: {ProductName}, Success: {Success}",
            request.ProductName, deleted);

        return new DeleteDiscountResponse { Success = deleted };
    }

    private static CouponModel MapToCouponModel(Coupon coupon)
    {
        return new CouponModel
        {
            Id = coupon.Id,
            ProductName = coupon.ProductName,
            Description = coupon.Description ?? string.Empty,
            Amount = coupon.Amount
        };
    }

    private static Coupon MapToCoupon(CouponModel model)
    {
        return new Coupon
        {
            Id = model.Id,
            ProductName = model.ProductName,
            Description = model.Description,
            Amount = model.Amount
        };
    }
}
