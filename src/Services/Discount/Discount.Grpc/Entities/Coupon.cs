using SharedKernel.Domain.Entities;

namespace Discount.Grpc.Entities;

public sealed class Coupon : Entity<int>
{

  public string ProductName { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public int Amount { get; set; }
}