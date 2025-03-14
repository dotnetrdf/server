using Microsoft.AspNetCore.Builder;

namespace VDS.RDF.Server;

public interface IServiceEndpoint
{
    void Register(WebApplication app);
}