using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace VDS.RDF.Server;

public interface IServiceEndpoint
{
    void Register(IEndpointRouteBuilder routeBuilder);
}