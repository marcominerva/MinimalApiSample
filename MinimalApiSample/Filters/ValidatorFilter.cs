using FluentValidation;

namespace MinimalApiSample.Filters;

public class ValidatorFilter<T> : IRouteHandlerFilter where T : class
{
    private readonly IValidator<T> validator;

    public ValidatorFilter(IValidator<T> validator)
    {
        this.validator = validator;
    }

    public async ValueTask<object> InvokeAsync(RouteHandlerInvocationContext context, RouteHandlerFilterDelegate next)
    {
        var input = context.Parameters.FirstOrDefault(p => p.GetType() == typeof(T)) as T;
        if (input is null)
        {
            return Results.BadRequest();
        }

        var validationResult = await validator.ValidateAsync(input);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.GroupBy(e => e.PropertyName)
                .ToDictionary(k => k.Key, v => v.Select(e => e.ErrorMessage).ToArray());

            return Results.ValidationProblem(errors);
        }

        return await next(context);
    }
}
