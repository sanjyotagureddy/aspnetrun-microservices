using System.Net;
using Discount.API.Controllers;
using Discount.API.Entities;
using Discount.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Discount.API.Test
{
    public class DiscountControllerTest
    {
        private Mock<IDiscountRepository> _repository;
        private DiscountController _controller;
        private Coupon _coupon, _couponNoDiscount;
        [SetUp]
        public void Setup()
        {
            _repository = new Mock<IDiscountRepository>();
            _controller = new DiscountController(_repository.Object);

            _coupon = new Coupon()
            {
                Id = 1,
                Amount = 150,
                ProductName = "TempDataAttribute",
                Description = ""
            };

            _couponNoDiscount = new Coupon()
            {
                Id = 0,
                Amount = 0,
                ProductName = "No discount",
                Description = "No discount"
            };

        }

        [TestCase("1")]
        [TestCase("2")]
        [Test]
        public void GetDiscount(string productName)
        {
            _repository.Setup(p => p.GetDiscount(productName)).ReturnsAsync(_coupon);
            var coupon = _controller.GetDiscount(productName);
            if (coupon.Result.Result is OkObjectResult okResult)
                Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
            else
                Assert.Fail();
        }

        [TestCase("5e")]
        [TestCase("62")]
        [Test]
        public void GetDiscount_NoDiscount(string productName)
        {
            _repository.Setup(p => p.GetDiscount(productName)).ReturnsAsync(_couponNoDiscount);
            var coupon = _controller.GetDiscount(productName);
            if (coupon.Result.Result is OkObjectResult okResult)
            {
                Assert.AreEqual((int) HttpStatusCode.OK, okResult.StatusCode);
            }
            else
                Assert.Fail();
        }
    }
}