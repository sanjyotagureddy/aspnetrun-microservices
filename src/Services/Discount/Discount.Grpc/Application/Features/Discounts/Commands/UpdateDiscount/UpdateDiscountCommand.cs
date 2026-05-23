using Discount.Grpc.Entities;
using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Commands.UpdateDiscount;

public sealed record UpdateDiscountCommand(Coupon Coupon) : IRequest<Coupon>;