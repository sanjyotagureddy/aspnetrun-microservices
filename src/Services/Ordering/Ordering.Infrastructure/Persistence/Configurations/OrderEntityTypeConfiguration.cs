using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Entities;

namespace Ordering.Infrastructure.Persistence.Configurations;

[ExcludeFromCodeCoverage]
public sealed class OrderEntityTypeConfiguration : IEntityTypeConfiguration<Order>
{
  public void Configure(EntityTypeBuilder<Order> builder)
  {
    builder.ToTable("Orders");

    builder.HasKey(order => order.Id);

    builder.Property(order => order.UserName).IsRequired().HasMaxLength(50);
    builder.Property(order => order.TotalPrice).HasColumnType("decimal(18,2)");

    builder.Property(order => order.FirstName).IsRequired().HasMaxLength(50);
    builder.Property(order => order.LastName).IsRequired().HasMaxLength(50);
    builder.Property(order => order.EmailAddress).IsRequired().HasMaxLength(100);
    builder.Property(order => order.AddressLine).IsRequired().HasMaxLength(150);
    builder.Property(order => order.Country).IsRequired().HasMaxLength(50);
    builder.Property(order => order.State).HasMaxLength(50);
    builder.Property(order => order.ZipCode).HasMaxLength(20);

    builder.Property(order => order.CardName).HasMaxLength(50);
    builder.Property(order => order.CardNumber).HasMaxLength(25);
    builder.Property(order => order.Expiration).HasMaxLength(10);
    builder.Property(order => order.CVV).HasMaxLength(3);
  }
}
