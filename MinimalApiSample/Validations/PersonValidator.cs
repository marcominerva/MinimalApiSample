using FluentValidation;
using MinimalApiSample.Models;

namespace MinimalApiSample.Validations;

public class PersonValidator : AbstractValidator<Person>
{
    public PersonValidator()
    {
        RuleFor(p => p.FirstName).NotEmpty().MaximumLength(30);
        RuleFor(p => p.LastName).NotEmpty().MaximumLength(30);
        RuleFor(p => p.City).MaximumLength(50);
    }
}
