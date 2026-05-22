using FluentValidation;
using UpdateProductCommand = Catalog.API.Application.Features.Products.Commands.UpdateProduct.UpdateProductCommand;

namespace Catalog.API.Application.Features.Products.Validators;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(command => command.Product)
            .NotNull().WithMessage("Product is required")
            .DependentRules(() =>
            {
                RuleFor(command => command.Product)
                    .SetValidator(new ProductValidator());
            });
    }
}