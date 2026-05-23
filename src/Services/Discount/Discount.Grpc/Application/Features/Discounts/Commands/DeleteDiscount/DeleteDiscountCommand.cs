using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Commands.DeleteDiscount;

public sealed record DeleteDiscountCommand(string ProductName) : IRequest<bool>;