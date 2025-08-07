namespace WebApplication1.Shared.Endpoints;

public interface IEndpoint
{
    void RegisterEndpoint(IEndpointRouteBuilder app);
}