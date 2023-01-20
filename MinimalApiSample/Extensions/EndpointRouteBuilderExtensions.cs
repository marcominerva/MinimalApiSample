namespace MinimalApiSample.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static void MapEndpoints<T>(this IEndpointRouteBuilder endpoints) where T : IEndpointRouteHandler
        => T.MapEndpoints(endpoints);
}

public interface IEndpointRouteHandler
{
    static abstract void MapEndpoints(IEndpointRouteBuilder endpoints);
}
