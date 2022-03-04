using FluentValidation;
using MinimalApiSample.Models;

namespace MinimalApiSample.Validations;

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Name).NotEmpty().MaximumLength(50);
    }
}