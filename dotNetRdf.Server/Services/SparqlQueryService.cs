using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace VDS.RDF.Server.Services;

public class SparqlQueryService(IRdfResponseWriter responseWriter) : ISparqlQueryService
{

    public async Task ProcessQueryAsync(HttpContext ctx, string query, StringValues defaultGraphUri, StringValues namedGraphUri, ISparqlQueryProcessor queryProcessor)
    {
        SparqlQuery parsedQuery;
        try
        {
            var parser = new SparqlQueryParser();
            parsedQuery = parser.ParseFromString(query);
        }
        catch (RdfException)
        {
            ctx.Response.StatusCode = 400;
            return;
        }

        if (defaultGraphUri.Any())
        {
            parsedQuery.ClearDefaultGraphs();
            foreach (var defaultGraphName in defaultGraphUri)
            {
                if (defaultGraphName != null)
                {
                    parsedQuery.AddDefaultGraph(new UriNode(new Uri(defaultGraphName)));
                }
            }
        }

        if (namedGraphUri.Count != 0)
        {
            parsedQuery.ClearNamedGraphs();
            foreach (var namedGraphName in namedGraphUri)
            {
                if (namedGraphName != null)
                {
                    parsedQuery.AddNamedGraph(new UriNode(new Uri(namedGraphName)));
                }
            }
        }

        try
        {
            var queryResult = await queryProcessor.ProcessQueryAsync(parsedQuery);
            if (queryResult is SparqlResultSet resultSet)
            {
                await responseWriter.WriteSparqlResultSetAsync(ctx, resultSet);
                return;
            }

            if (queryResult is IGraph resultGraph)
            {
                await responseWriter.WriteGraphAsync(ctx, resultGraph);
                return;
            }

            // Unexpected type of query result
            ctx.Response.StatusCode = 500;
        }
        catch (RdfQueryTimeoutException)
        {
            ctx.Response.StatusCode = 504;
        }
        catch (RdfException)
        {
            // Unexpected error processing query
            ctx.Response.StatusCode = 500;
        }

    }
}