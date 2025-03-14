using Microsoft.AspNetCore.Builder;

namespace VDS.RDF.Server;

public interface IServiceEndpoint
{
    string Path { get; }
    void Register(WebApplication app);
}