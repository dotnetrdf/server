using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VDS.RDF.Query;

namespace VDS.RDF.Server.Services;

public interface ISparqlQueryService
{
    Task ProcessQueryAsync(HttpContext ctx, string query, StringValues defaultGraphUri, StringValues namedGraphUri, ISparqlQueryProcessor queryProcessor);
}