using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using VDS.RDF.Server.Services;
using VDS.RDF.Update;

namespace VDS.RDF.Server;

public class SparqlUpdateEndpoint(string path, ISparqlUpdateProcessor updateProcessor):IServiceEndpoint
{
    public string Path { get; } = path;
    public void Register(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost(Path, async (ctx) =>
        {
            string update;
            StringValues? usingGraphUris, usingNamedGraphUris;
            if (ctx.Request.HasFormContentType)
            {
                var form = await ctx.Request.ReadFormAsync();
                update = form["query"].FirstOrDefault() ?? String.Empty;
                usingGraphUris = form["using-graph-uri"];
                usingNamedGraphUris = form["using-named-graph-uri"];
            }
            else if (ctx.Request.ContentType == "application/sparql-update")
            {
                update = await new StreamReader(ctx.Request.Body).ReadToEndAsync();
                usingGraphUris = ctx.Request.Query["using-graph-uri"];
                usingNamedGraphUris = ctx.Request.Query["using-named-graph-uri"];
            }
            else
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await ctx.Response.WriteAsync("Invalid request Content-Type");
                return;
            }

            var updateService = new SparqlUpdateService();
            await updateService.ProcessUpdateAsync(ctx, update, (StringValues)usingGraphUris,
                (StringValues)usingNamedGraphUris, updateProcessor);
        });
    }
}