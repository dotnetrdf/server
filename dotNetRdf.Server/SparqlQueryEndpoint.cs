using System.Text;
using AngleSharp.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Writing;

namespace VDS.RDF.Server;

public class SparqlQueryEndpoint(string path, ISparqlQueryProcessor queryProcessor) : IServiceEndpoint
{
    public string Path { get; } = path;

    public void Register(WebApplication app)
    {
        app.MapGet(Path, async (ctx) =>
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
            await ProcessQueryAsync(ctx, query[0]!, defaultGraphUri, namedGraphUri);
        });

        app.MapPost(Path, async (ctx) =>
        {
            var mediaType = ctx.Request.GetTypedHeaders().ContentType?.MediaType.Value;
            string query;
            StringValues? defaultGraphUris = null, namedGraphUris = null;
            
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
            await ProcessQueryAsync(ctx, query, (StringValues)defaultGraphUris, (StringValues)namedGraphUris);
        });
    }

    private async Task ProcessQueryAsync(HttpContext ctx, string query, StringValues defaultGraphUri, StringValues namedGraphUri)
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

        if (namedGraphUri.Any())
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
                var mimeTypeDef =  ctx.GetAcceptableMediaType(it => it.CanWriteSparqlResults);
                if (!mimeTypeDef.HasValue)
                {
                    ctx.Response.StatusCode = 405;
                    return;
                }

                ctx.Response.StatusCode = 200;
                var resultsWriter = mimeTypeDef.Value.MimeTypeDefinition.GetSparqlResultsWriter();
                var acceptHeaderMatch = mimeTypeDef.Value.MatchedMediaType;
                ctx.Response.ContentType = acceptHeaderMatch.MediaType.Value;
                await using var textWriter =
                    new StreamWriter(ctx.Response.Body, acceptHeaderMatch.Encoding ?? Encoding.UTF8);
                resultsWriter.Save(resultSet, textWriter);
                textWriter.Close();
                return;
            }

            if (queryResult is IGraph resultGraph)
            {
                var mimeTypeDef = ctx.GetAcceptableMediaType(it => it.CanWriteRdf);
                if (!mimeTypeDef.HasValue)
                {
                    ctx.Response.StatusCode = 405;
                    return;
                }

                var writerMimeType = mimeTypeDef.Value.MimeTypeDefinition;
                var acceptHeaderMatch = mimeTypeDef.Value.MatchedMediaType;
                var resultsWriter = writerMimeType.GetRdfWriter();
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = acceptHeaderMatch.MediaType.Value;
                await using var textWriter =
                    new StreamWriter(ctx.Response.Body, acceptHeaderMatch.Encoding ?? Encoding.UTF8);
                resultsWriter.Save(resultGraph, textWriter);
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