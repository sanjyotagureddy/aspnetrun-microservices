using AutoMapper;
using Discount.Grpc.Entities;
using Discount.Grpc.Mapper;
using Discount.Grpc.Protos;
using Discount.Grpc.Repositories.Interfaces;
using Discount.Grpc.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Discount.Grpc.Test;

public class DiscountServiceTests
{
    private Mock<IDiscountRepository> _repository;
    private Mock<ILogger<DiscountService>> _logger;
    private IMapper _mapper;
    private DiscountService _discountService;
    private Coupon _coupon;

    [SetUp]
    public void Setup()
    {
        _logger = new Mock<ILogger<DiscountService>>();

        var mapperConfig = new MapperConfiguration(
            mc =>
            {
                mc.AddProfile(new DiscountProfile());
            });

        _mapper = mapperConfig.CreateMapper();
        _repository = new Mock<IDiscountRepository>();
        _discountService = new DiscountService(_repository.Object, _logger.Object, _mapper);

        _coupon = new Coupon()
        {
            Id = 1,
            Amount = 150,
            ProductName = "TempDataAttribute",
            Description = ""
        };
    }

    [TestCase("swn")]
    [TestCase("abc")]
    [Test]
    public void GetDiscount(string productName)
    {
        _repository.Setup(p => p.GetDiscount(productName)).ReturnsAsync(_coupon);
        var coupon = _discountService.GetDiscount(new GetDiscountRequest() { ProductName = productName }, null);
        if (coupon.Result != null)
            Assert.AreEqual(coupon.Result.Amount, 150);
        else
            Assert.Fail();
    }

    [TestCase("1")]
    [TestCase("2")]
    [Test]
    public async Task DeleteDiscount(string productName)
    {
        _repository.Setup(p => p.DeleteDiscount(productName)).ReturnsAsync(true);
        var coupon =
            await _discountService.DeleteDiscount(new DeleteDiscountRequest() { ProductName = productName }, null);

        Assert.AreEqual(coupon.Success, true);
    }

    [TestCase("1")]
    [TestCase("2")]
    [Test]
    public async Task DeleteDiscount_False(string productName)
    {
        _repository.Setup(p => p.DeleteDiscount(productName)).ReturnsAsync(false);
        var coupon =
            await _discountService.DeleteDiscount(new DeleteDiscountRequest() { ProductName = productName }, null);

        Assert.AreNotEqual(coupon.Success, true);
    }
}