using MediatR;

namespace Ordering.Application.Features.Orders.Commands.DeleteCommand;

public class DeleteOrderCommand : IRequest
{
    public int Id { get; set; }
}