using System.Net;
using Discount.API.Controllers;
using Discount.API.Entities;
using Discount.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Discount.API.Test;

public class DiscountControllerTest
{
  private readonly DiscountController _controller;
  private readonly Coupon _coupon;
  private readonly Coupon _couponNoDiscount;
  private readonly Mock<IDiscountRepository> _repository;

  public DiscountControllerTest()
  {
    _repository = new Mock<IDiscountRepository>();
    _controller = new DiscountController(_repository.Object);

    _coupon = new Coupon
    {
      Id = 1,
      Amount = 150,
      ProductName = "TempDataAttribute",
      Description = ""
    };

    _couponNoDiscount = new Coupon
    {
      Id = 0,
      Amount = 0,
      ProductName = "No discount",
      Description = "No discount"
    };
  }

  [Theory]
  [InlineData("1")]
  [InlineData("2")]
  public async Task GetDiscount(string productName)
  {
    _repository.Setup(p => p.GetDiscount(productName)).ReturnsAsync(_coupon);
    var coupon = await _controller.GetDiscount(productName);

    var okResult = Assert.IsType<OkObjectResult>(coupon.Result);
    Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
  }

  [Theory]
  [InlineData("5e")]
  [InlineData("62")]
  public async Task GetDiscount_NoDiscount(string productName)
  {
    _repository.Setup(p => p.GetDiscount(productName)).ReturnsAsync(_couponNoDiscount);
    var coupon = await _controller.GetDiscount(productName);

    var okResult = Assert.IsType<OkObjectResult>(coupon.Result);
    Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
  }

  [Theory]
  [InlineData("1")]
  [InlineData("2")]
  public async Task DeleteDiscount(string productName)
  {
    _repository.Setup(p => p.DeleteDiscount(productName)).ReturnsAsync(true);
    var coupon = await _controller.DeleteDiscount(productName);

    var okResult = Assert.IsType<OkObjectResult>(coupon.Result);
    Assert.Equal((int)HttpStatusCode.OK, okResult.StatusCode);
  }

  [Fact]
  public async Task CreateDiscount()
  {
    _repository.Setup(p => p.CreateDiscount(It.IsAny<Coupon>())).ReturnsAsync(true);
    var coupon = await _controller.CreateDiscount(NewCoupon());

    var createdResult = Assert.IsType<CreatedAtRouteResult>(coupon.Result);
    Assert.Equal((int)HttpStatusCode.Created, createdResult.StatusCode);
  }

  private static Coupon NewCoupon()
  {
    return new()
    {
      Id = 5,
      Amount = 10,
      Description = "",
      ProductName = "AND"
    };
  }
}