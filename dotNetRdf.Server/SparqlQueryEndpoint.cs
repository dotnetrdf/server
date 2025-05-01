using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using VDS.RDF.Query;
using VDS.RDF.Server.Services;

namespace VDS.RDF.Server;

/**
 * <summary>
 * A <see cref="IServiceEndpoint"/> that implements the SPARQL Protocol query operation.
 * </summary>
 * <para>
 * The endpoint supports both GET and POST operations in accordance with section 2.1 of
 * SPARQL 1.1 Protocol. (https://www.w3.org/TR/sparql11-protocol/)
 * </para>
 */
public class SparqlQueryEndpoint(string path, ISparqlQueryProcessor queryProcessor) : IServiceEndpoint
{
    /// <summary>
    /// Get the server path that this endpoint responds on
    /// </summary>
    public string Path { get; } = path;

    public void Register(IEndpointRouteBuilder routeBuilder)
    {
        var sparqlService = routeBuilder.ServiceProvider.GetRequiredService<ISparqlQueryService>();
        routeBuilder.MapGet(Path, async (ctx) =>
        {
            var query = ctx.Request.Query["query"];
            var defaultGraphUri = ctx.Request.Query["default-graph-uri"];
            var namedGraphUri = ctx.Request.Query["named-graph-uri"];
            if (query.Count != 1)
            {
                ctx.Response.StatusCode = 400;
                return;
            }

            ctx.EnableSynchronousIO();
            await sparqlService.ProcessQueryAsync(ctx, query[0]!, defaultGraphUri, namedGraphUri, queryProcessor);
        });

        routeBuilder.MapPost(Path, async (ctx) =>
        {
            var mediaType = ctx.Request.GetTypedHeaders().ContentType?.MediaType.Value?.ToLowerInvariant();
            string query;
            StringValues? defaultGraphUris, namedGraphUris;
            
            switch (mediaType)
            {
                case "application/x-www-form-urlencoded":
                    // Get args from form body
                    var form = await ctx.Request.ReadFormAsync();
                    query = form["query"].FirstOrDefault() ?? String.Empty;
                    defaultGraphUris = form["default-graph-uri"];
                    namedGraphUris = form["named-graph-uri"];
                break;
                case "application/sparql-query":
                    // Get query from body and other args from query params
                    using (StreamReader reader = new StreamReader(ctx.Request.Body))
                    {
                        query = await reader.ReadToEndAsync();
                    }
                    defaultGraphUris = ctx.Request.Query["default-graph-uri"];
                    namedGraphUris = ctx.Request.Query["named-graph-uri"];
                break;
                default:
                    ctx.Response.StatusCode = 400;
                    return;
            }

            if (string.IsNullOrEmpty(query))
            {
                ctx.Response.StatusCode = 400;
                return;
            }
            ctx.EnableSynchronousIO();
            await sparqlService.ProcessQueryAsync(ctx, query, (StringValues)defaultGraphUris, (StringValues)namedGraphUris, queryProcessor);
        });
    }

}