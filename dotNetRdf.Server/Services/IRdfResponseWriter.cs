using Microsoft.AspNetCore.Http;
using VDS.RDF.Query;

namespace VDS.RDF.Server.Services;

public interface IRdfResponseWriter
{
    public Task WriteSparqlResultSetAsync( HttpContext ctx, SparqlResultSet resultSet);
    public Task WriteGraphAsync(HttpContext ctx, IGraph graph);
}