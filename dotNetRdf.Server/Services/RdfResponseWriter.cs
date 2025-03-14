using System.Text;
using Microsoft.AspNetCore.Http;
using VDS.RDF.Query;

namespace VDS.RDF.Server.Services;

public class RdfResponseWriter: IRdfResponseWriter
{
    public async Task WriteSparqlResultSetAsync(HttpContext ctx, SparqlResultSet resultSet)
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
    }

    public async Task WriteGraphAsync(HttpContext ctx, IGraph graph)
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
        resultsWriter.Save(graph, textWriter);
    }
}