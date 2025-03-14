using Microsoft.AspNetCore.Http;
using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Server.Services;

namespace dotNetRdf.Server.Tests.Services;

public class RdfResponseWriterTests
{
    private readonly SparqlResultSet _resultSet;
    private readonly Graph _graph;
    public RdfResponseWriterTests()
    {
        _graph = new Graph();
        _graph.Assert(new Triple(
            _graph.CreateUriNode(new Uri("http://example.org/s")),
            _graph.CreateUriNode(new Uri("http://example.org/p")),
            _graph.CreateUriNode(new Uri("http://example.org/o"))));
        _resultSet = new SparqlResultSet(new SparqlResult[]
        {
            new ([
                new KeyValuePair<string, INode>("x", _graph.CreateUriNode(new Uri("http://example.org/x")))
            ])
        });
    }

    [Fact]
    public async Task WriteSparqlResultSetAsync_SelectsPreferredMediaType()
    {
        var responseBody = new MemoryStream();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Accept"] = "application/svg+xml;q=1.0,application/sparql-results+xml;q=0.9,application/sparql-results+json;q=0.8";
        ctx.Response.Body = responseBody;
        var sut = new RdfResponseWriter();
        var parser = new SparqlXmlParser();

        await sut.WriteSparqlResultSetAsync(ctx, _resultSet);
        
        Assert.Equal(200, ctx.Response.StatusCode);
        Assert.Equal("application/sparql-results+xml", ctx.Response.Headers["Content-Type"]);
        using var reader = new StreamReader(new MemoryStream(responseBody.GetBuffer()));
        var parsedResults = new SparqlResultSet();
        parser.Load(parsedResults, reader);
        Assert.Equal(_resultSet, parsedResults);
    }

    [Fact]
    public async Task WriteSparqlResultSetAsync_ReturnsNotAcceptable()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Accept"] = "application/svg+xml";
        var sut = new RdfResponseWriter();
        await sut.WriteSparqlResultSetAsync(ctx, _resultSet);
        Assert.Equal(405, ctx.Response.StatusCode);
    }

    [Fact]
    public async Task WriteGraphAsync_SelectsPreferredMediaType()
    {
        var responseBody = new MemoryStream();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Accept"] = "application/svg+xml;q=1.0,application/rdf+xml;q=0.8,text/turtle;q=0.9";
        ctx.Response.Body = responseBody;
        var sut = new RdfResponseWriter();
        
        await sut.WriteGraphAsync(ctx, _graph);
        
        Assert.Equal(200, ctx.Response.StatusCode);
        Assert.Equal("text/turtle", ctx.Response.Headers["Content-Type"]);
    }
    
    [Fact]
    public async Task WriteGraphAsync_ReturnsNotAcceptable()
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Accept"] = "application/svg+xml";
        var sut = new RdfResponseWriter();
        await sut.WriteGraphAsync(ctx, _graph);
        Assert.Equal(405, ctx.Response.StatusCode);
    }

}