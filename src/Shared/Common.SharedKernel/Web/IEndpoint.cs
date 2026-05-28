using Microsoft.AspNetCore.Routing;

namespace Common.SharedKernel.Web;

public interface IEndpoint
{
    void MapEndpoints(IEndpointRouteBuilder app);
}
