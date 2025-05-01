using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Update;
using VDS.RDF.Update.Commands;

namespace VDS.RDF.Server.Services;

public class SparqlUpdateService : ISparqlUpdateService
{
    public async Task ProcessUpdateAsync(HttpContext ctx, string update, StringValues usingGraphUri, StringValues usingNamedGraphUri,
        ISparqlUpdateProcessor updateProcessor)
    {
        if (String.IsNullOrEmpty(update))
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await ctx.Response.WriteAsync("No SPARQL Update provided");
            return;
        }

        SparqlUpdateCommandSet cmdSet;
        try
        {
            SparqlUpdateParser parser = new SparqlUpdateParser(SparqlQuerySyntax.Sparql_Star_1_1);
            cmdSet = parser.ParseFromString(update);
        }
        catch (RdfException ex)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await ctx.Response.WriteAsync("Invalid SPARQL Update provided:\n" + ex.Message);
            return;
        }

        try
        {
            var userDefaultGraphs = usingGraphUri.Where(g => !string.IsNullOrEmpty(g)).ToList();
            var userNamedGraphs = usingNamedGraphUri.Where(g => !string.IsNullOrEmpty(g)).ToList();
            if (userDefaultGraphs.Any() || userNamedGraphs.Any())
            {
                foreach (var cmd in cmdSet.Commands)
                {
                    if (cmd is BaseModificationCommand modifyCmd)
                    {
                        if (modifyCmd.WithGraphName != null || modifyCmd.UsingUris.Any() ||
                            modifyCmd.UsingNamedUris.Any())
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            await ctx.Response.WriteAsync(
                                "A command in your update request contains a WITH/USING/USING NAMED clause but you have also specified one/both of the using-graph-uri or using-named-graph-uri parameters which is not permitted by the SPARQL Protocol");
                            return;
                        }

                        try
                        {
                            userDefaultGraphs.ForEach(g => modifyCmd.AddUsingUri(new Uri(g!)));
                            userNamedGraphs.ForEach(g => modifyCmd.AddUsingNamedUri(new Uri(g!)));
                        }
                        catch (UriFormatException)
                        {
                            ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                            await ctx.Response.WriteAsync(
                                "Invalid graph name in using-graph-uri or using-named-graph-uri parameter");
                            return;
                        }
                    }
                }
            }

            updateProcessor.ProcessCommandSet(cmdSet);
            updateProcessor.Flush();
            ctx.Response.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (RdfQueryTimeoutException timeout)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
            await ctx.Response.WriteAsync("SPARQL Update timed out");
        }
        catch (RdfException exception)
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await ctx.Response.WriteAsync("Error processing SPARQL Update:\n" + exception.Message);
        }
    }
}