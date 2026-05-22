using CreateProductCommand = Catalog.API.Application.Features.Products.Commands.CreateProduct.CreateProductCommand;
using FluentValidation;

namespace Catalog.API.Application.Features.Products.Validators;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Name).NotEmpty().WithMessage("Name is required").MaximumLength(200);
        RuleFor(command => command.Category).NotEmpty().WithMessage("Category is required").MaximumLength(100);
        RuleFor(command => command.Summary).MaximumLength(1000);
        RuleFor(command => command.Description).MaximumLength(2000);
        RuleFor(command => command.ImageFile).MaximumLength(200);
        RuleFor(command => command.Price).GreaterThanOrEqualTo(0).WithMessage("Price must be non-negative");
    }
}