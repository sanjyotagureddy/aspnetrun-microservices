using Discount.Grpc.Entities;
using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Commands.CreateDiscount;

public sealed record CreateDiscountCommand(Coupon Coupon) : IRequest<Coupon>;