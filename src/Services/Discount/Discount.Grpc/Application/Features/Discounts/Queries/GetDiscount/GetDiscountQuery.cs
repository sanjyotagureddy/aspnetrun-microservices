using Discount.Grpc.Entities;
using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Queries.GetDiscount;

public sealed record GetDiscountQuery(string ProductName) : IRequest<Coupon?>;