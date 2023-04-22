using System.Net;
using AutoMapper;
using Basket.API.Controllers;
using Basket.API.Entities;
using Basket.API.GrpcServices;
using Basket.API.Mappings;
using Basket.API.Repositories.Interfaces;
using Discount.Grpc.Protos;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Basket.API.Test;

public class BasketControllerTest
{
  private BasketController _controller;
  private Mock<DiscountGrpcService> _grpcService;
  private IMapper _mapper;
  private Mock<DiscountProtoService.DiscountProtoServiceClient> _mock;
  private Mock<IPublishEndpoint> _publishEndpoint;
  private Mock<IBasketRepository> _repository;
  private ShoppingCart _shoppingCart;

  [SetUp]
  public void Setup()
  {
    _mock = new Mock<DiscountProtoService.DiscountProtoServiceClient>();
    _repository = new Mock<IBasketRepository>();
    _grpcService = new Mock<DiscountGrpcService>(_mock.Object);
    _publishEndpoint = new Mock<IPublishEndpoint>();
    var mapperConfig = new MapperConfiguration(
      mc => { mc.AddProfile(new BasketProfile()); });

    _mapper = mapperConfig.CreateMapper();
    _controller = new BasketController(_repository.Object, _grpcService.Object, _publishEndpoint.Object, _mapper);

    _shoppingCart = new ShoppingCart
    {
      UserName = "swn"
    };
  }

  [TestCase("swn")]
  [Test]
  public void GetBasket(string userName)
  {
    _repository.Setup(p => p.GetBasket(userName)).ReturnsAsync(_shoppingCart);
    var basket = _controller.GetBasket(userName);
    if (basket.Result.Result is OkObjectResult okResult)
      Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
    else
      Assert.Fail();
  }

  [TestCase("abc")]
  [TestCase("xyz")]
  [Test]
  public void GetBasket_NotFound(string userName)
  {
    _repository.Setup(p => p.GetBasket(userName)).ReturnsAsync(new ShoppingCart(userName));
    var basket = _controller.GetBasket(userName);
    if (basket.Result.Result is OkObjectResult okResult)
      Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
    else
      Assert.Fail();
  }
}