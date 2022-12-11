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
        var input = context.Arguments.FirstOrDefault(a => a.GetType() == typeof(T)) as T;
        if (input is null)
        {
            return TypedResults.BadRequest();
        }

        var validationResult = await validator.ValidateAsync(input);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}
