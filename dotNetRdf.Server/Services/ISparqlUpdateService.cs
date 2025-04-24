using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VDS.RDF.Update;

namespace VDS.RDF.Server.Services;

public interface ISparqlUpdateService
{
    Task ProcessUpdateAsync(HttpContext ctx, string update, StringValues usingGraphUri, StringValues usingNamedGraphUri, ISparqlUpdateProcessor updateProcessor);
}