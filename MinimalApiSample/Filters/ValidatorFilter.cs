using FluentValidation;

namespace MinimalApiSample.Filters;

public class ValidatorFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> validator;

    public ValidatorFilter(IValidator<T> validator)
    {
        this.validator = validator;
    }

    public async ValueTask<object> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (context.Arguments.FirstOrDefault(a => a.GetType() == typeof(T)) is T input)
        {
            //if (!MiniValidator.TryValidate(input, out var errors))
            //{
            //    return Results.ValidationProblem(errors);
            //}

            var validationResult = await validator.ValidateAsync(input);
            if (!validationResult.IsValid)
            {
                return TypedResults.ValidationProblem(validationResult.ToDictionary());
            }

            return await next(context);
        }

        return TypedResults.BadRequest();
    }
}
