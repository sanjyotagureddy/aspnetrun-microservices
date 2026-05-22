using Catalog.API.Application.Features.Products.Commands.CreateProduct;
using Catalog.API.Application.Features.Products.Commands.UpdateProduct;
using Catalog.API.Application.Features.Products.Validators;
using Catalog.API.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Catalog.API.Test.Application;

public class ValidatorsTests
{
    [Fact]
    public void CreateProductValidator_AcceptsValidCommand()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand("Name", "Category", "Summary", "Description", "image.png", 1m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateProductValidator_RejectsInvalidCommand()
    {
        var validator = new CreateProductCommandValidator();
        var command = new CreateProductCommand(string.Empty, string.Empty, new string('a', 1001), new string('b', 2001), new string('c', 201), -1m);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Name is required"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Category is required"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Price must be non-negative"));
    }

    [Fact]
    public void ProductValidator_AcceptsValidProduct()
    {
        var validator = new ProductValidator();
        var product = new Product { Name = "Name", Category = "Category", Summary = "Summary", Description = "Description", ImageFile = "image.png", Price = 1m };

        var result = validator.Validate(product);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ProductValidator_RejectsInvalidProduct()
    {
        var validator = new ProductValidator();
        var product = new Product { Name = string.Empty, Category = string.Empty, Summary = new string('a', 1001), Description = new string('b', 2001), ImageFile = new string('c', 201), Price = -1m };

        var result = validator.Validate(product);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Name is required"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Category is required"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Price must be non-negative"));
    }

    [Fact]
    public void UpdateProductValidator_RejectsNullProduct()
    {
        var validator = new UpdateProductCommandValidator();

        var result = validator.Validate(new UpdateProductCommand(null!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Product is required"));
    }

    [Fact]
    public void UpdateProductValidator_ValidatesNestedProduct()
    {
        var validator = new UpdateProductCommandValidator();
        var product = new Product { Name = string.Empty, Category = string.Empty, Price = -1m };

        var result = validator.Validate(new UpdateProductCommand(product));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Name is required"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Category is required"));
        result.Errors.Should().Contain(error => error.ErrorMessage.Contains("Price must be non-negative"));
    }
}