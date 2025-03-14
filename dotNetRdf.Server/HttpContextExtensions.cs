using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace VDS.RDF.Server;

public static class HttpContextExtensions
{
    public static void EnableSynchronousIO(this HttpContext httpContext)
    {
        var bodyControlFeature = httpContext.Features.Get<IHttpBodyControlFeature>();
        if (bodyControlFeature != null)
        {
            bodyControlFeature.AllowSynchronousIO = true;
        }

    }

    public static MediaTypeDef? GetAcceptableMediaType(this HttpContext httpContext, Func<MimeTypeDefinition, bool> filter)
    {
        foreach (var acceptHeader in httpContext.Request.GetTypedHeaders().Accept
                     .OrderByDescending(it => it.Quality ?? 0.0d))
        {
            if (acceptHeader.MediaType.Value != null)
            {
                foreach (var mimeTypeDefinition in MimeTypesHelper.GetDefinitions(acceptHeader.MediaType.Value))
                {
                    if (filter(mimeTypeDefinition))
                    {
                        return new MediaTypeDef(mimeTypeDefinition, acceptHeader);
                    }
                }
            }

        }

        return null;
    }
    
}

public readonly struct MediaTypeDef(MimeTypeDefinition mimeTypeDefinition, MediaTypeHeaderValue mediaType)
{
    public MimeTypeDefinition MimeTypeDefinition { get; } = mimeTypeDefinition;
    public MediaTypeHeaderValue MatchedMediaType { get; } = mediaType;
}