namespace MinimalApiSample.Routing;

public interface IEndpointRouteHandler
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints);
}
