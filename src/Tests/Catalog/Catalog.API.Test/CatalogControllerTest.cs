using System.Collections.Generic;
using System.Net;
using Catalog.API.Controllers;
using Catalog.API.Entities;
using Catalog.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
namespace Catalog.API.Test
{
    public class CatalogControllerTest
    {
        private CatalogController _catalogController;
        private Mock<IProductRepository> _repository;
        private Mock<ILogger<CatalogController>> _logger;
        private List<Product> _products;

        [SetUp]
        public void Setup()
        {
            _products = new List<Product>()
                    {
                        new Product()
                        {
                            Id = "602d2149e773f2a3990b47f5",
                            Name = "IPhone X",
                            Summary = "This phone is the company's biggest change to its flagship smartphone in years. It includes a borderless.",
                            Description = "Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus.",
                            ImageFile = "product-1.png",
                            Price = 950.00M,
                            Category = "Smart Phone"
                        },
                        new Product()
                        {
                            Id = "602d2149e773f2a3990b47f6",
                            Name = "Samsung 10",
                            Summary = "This phone is the company's biggest change to its flagship smartphone in years. It includes a borderless.",
                            Description = "Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus.",
                            ImageFile = "product-2.png",
                            Price = 840.00M,
                            Category = "Smart Phone"
                        },
                        new Product()
                        {
                            Id = "602d2149e773f2a3990b47f7",
                            Name = "Huawei Plus",
                            Summary = "This phone is the company's biggest change to its flagship smartphone in years. It includes a borderless.",
                            Description = "Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus.",
                            ImageFile = "product-3.png",
                            Price = 650.00M,
                            Category = "White Appliances"
                        },
                        new Product()
                        {
                            Id = "602d2149e773f2a3990b47f8",
                            Name = "Xiaomi Mi 9",
                            Summary = "This phone is the company's biggest change to its flagship smartphone in years. It includes a borderless.",
                            Description = "Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus.",
                            ImageFile = "product-4.png",
                            Price = 470.00M,
                            Category = "White Appliances"
                        },
                        new Product()
                        {
                            Id = "602d2149e773f2a3990b47f9",
                            Name = "HTC U11+ Plus",
                            Summary = "This phone is the company's biggest change to its flagship smartphone in years. It includes a borderless.",
                            Description = "Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus.",
                            ImageFile = "product-5.png",
                            Price = 380.00M,
                            Category = "Smart Phone"
                        },
                        new Product()
                        {
                            Id = "602d2149e773f2a3990b47fa",
                            Name = "LG G7 ThinQ",
                            Summary = "This phone is the company's biggest change to its flagship smartphone in years. It includes a borderless.",
                            Description = "Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus. Lorem ipsum dolor sit amet, consectetur adipisicing elit. Ut, tenetur natus doloremque laborum quos iste ipsum rerum obcaecati impedit odit illo dolorum ab tempora nihil dicta earum fugiat. Temporibus, voluptatibus.",
                            ImageFile = "product-6.png",
                            Price = 240.00M,
                            Category = "Home Kitchen"
                        }
                    };

            _repository = new Mock<IProductRepository>();
            _repository.Setup(p => p.GetProducts()).ReturnsAsync(_products);
            _logger = new Mock<ILogger<CatalogController>>();

            _catalogController = new CatalogController(_repository.Object, _logger.Object);
        }

        [Test]
        public void GetProducts()
        {

            var products = _catalogController.GetProducts();
            if (products.Result.Result is OkObjectResult okResult)
                Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
            else
                Assert.Fail();
        }
    }
}
