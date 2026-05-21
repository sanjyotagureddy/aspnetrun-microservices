using Microsoft.AspNetCore.Routing;

namespace SharedKernel.Web;

public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
