using Catalog.API.Domain.Entities;
using FluentValidation;

namespace Catalog.API.Application.Features.Products.Validators;

internal sealed class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(product => product.Name).NotEmpty().WithMessage("Name is required").MaximumLength(200);
        RuleFor(product => product.Category).NotEmpty().WithMessage("Category is required").MaximumLength(100);
        RuleFor(product => product.Summary).MaximumLength(1000);
        RuleFor(product => product.Description).MaximumLength(2000);
        RuleFor(product => product.ImageFile).MaximumLength(200);
        RuleFor(product => product.Price).GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative");
    }
}