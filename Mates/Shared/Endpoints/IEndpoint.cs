namespace Mates.Shared.Endpoints;

public interface IEndpoint
{
    void RegisterEndpoint(IEndpointRouteBuilder app);
}