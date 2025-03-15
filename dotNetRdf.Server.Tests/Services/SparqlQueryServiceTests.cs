using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using VDS.RDF;
using VDS.RDF.Query;
using VDS.RDF.Server.Services;

namespace dotNetRdf.Server.Tests.Services;

public class SparqlQueryServiceTests
{
    private readonly SparqlResultSet _emptyResultSet = new ();
    private readonly Mock<ISparqlQueryProcessor> _succesfulQueryProcessorMock;
    private readonly Mock<ISparqlQueryProcessor> _failedQueryProcessorMock;
    private readonly Mock<ISparqlQueryProcessor> _timedOutQueryProcessorMock;
    private readonly HttpContext _httpContext;

    public SparqlQueryServiceTests()
    {
        _succesfulQueryProcessorMock = new Mock<ISparqlQueryProcessor>();
        _succesfulQueryProcessorMock.Setup(x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()))
            .ReturnsAsync(_emptyResultSet);
        _failedQueryProcessorMock = new Mock<ISparqlQueryProcessor>();
        _failedQueryProcessorMock.Setup(x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()))
            .ThrowsAsync(new RdfQueryException("SPARQL QUERY ERROR"));
        _timedOutQueryProcessorMock = new Mock<ISparqlQueryProcessor>();
        _timedOutQueryProcessorMock.Setup(x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()))
            .ThrowsAsync(new RdfQueryTimeoutException("SPARQL QUERY TIMEOUT"));
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Headers.Append("Accept", new StringValues("application/sparql-results+xml"));
    }

    [Fact]
    public async Task ItPassesParsedSparqlQueryToTheQueryProcessor()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE { ?s ?p ?o }",
            new StringValues(),
            new StringValues(),
            _succesfulQueryProcessorMock.Object);

        // Query processor should be called once
        _succesfulQueryProcessorMock.Verify(
            x =>
                x.ProcessQueryAsync(
                    It.Is<SparqlQuery>(query =>
                        !query.DefaultGraphNames.Any() &&
                        !query.NamedGraphNames.Any()
                    )
                ),
            Times.Once
        );
        _httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ItRespondsWith400OnInvalidSparqlQuery()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o",
            new StringValues(),
            new StringValues(),
            _succesfulQueryProcessorMock.Object);
        // Query processor should not have been called
        _succesfulQueryProcessorMock.Verify(
            x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()),
            Times.Never
        );
        _httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ItPassesValidDefaultGraphNamesToQueryProcessor()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues(["http://example.com/g1", "http://example.com/g2"]),
            new StringValues(),
            _succesfulQueryProcessorMock.Object);
        
        _httpContext.Response.StatusCode.Should().Be(200);

        _succesfulQueryProcessorMock.Verify(
            x =>
                x.ProcessQueryAsync(
                    It.Is<SparqlQuery>(query =>
                        query.DefaultGraphNames.OfType<IUriNode>().Any(u=>u.Uri.AbsoluteUri.Equals("http://example.com/g1")) &&
                        query.DefaultGraphNames.OfType<IUriNode>().Any(u=>u.Uri.AbsoluteUri.Equals("http://example.com/g2")) &&
                        !query.NamedGraphNames.Any()
                    )
                ),
            Times.Once
        );

    }
    [Fact]
    public async Task ItRespondsWith40OnInvalidDefaultGraphName()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues("bad"),
            new StringValues(),
            _succesfulQueryProcessorMock.Object);
        // Query processor should not have been called
        _succesfulQueryProcessorMock.Verify(
            x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()),
            Times.Never
        );
        _httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ItPassesValidNamedGraphNamesToQueryProcessor()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues(),
            new StringValues(["http://example.com/g1", "http://example.com/g2"]),
            _succesfulQueryProcessorMock.Object);
        
        _httpContext.Response.StatusCode.Should().Be(200);

        _succesfulQueryProcessorMock.Verify(
            x =>
                x.ProcessQueryAsync(
                    It.Is<SparqlQuery>(query =>
                        query.NamedGraphNames.OfType<IUriNode>().Any(u=>u.Uri.AbsoluteUri.Equals("http://example.com/g1")) &&
                        query.NamedGraphNames.OfType<IUriNode>().Any(u=>u.Uri.AbsoluteUri.Equals("http://example.com/g2")) &&
                        !query.DefaultGraphNames.Any()
                    )
                ),
            Times.Once
        );
    }
    
    [Fact]
    public async Task ItRespondsWith40OnInvalidNamedGraphName()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues("http://example.org/"),
            new StringValues("bad"),
            _succesfulQueryProcessorMock.Object);
        // Query processor should not have been called
        _succesfulQueryProcessorMock.Verify(
            x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()),
            Times.Never
        );
        _httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ItRespondsWith504OnQueryTimeout()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues(),
            new StringValues(),
            _timedOutQueryProcessorMock.Object);

        _timedOutQueryProcessorMock.Verify(
            x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()),
            Times.Once
        );
        _httpContext.Response.StatusCode.Should().Be(504);
    }

    [Fact]
    public async Task ItRespondsWith500OnQueryException()
    {
        var sut = new SparqlQueryService(new RdfResponseWriter());
        await sut.ProcessQueryAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues(),
            new StringValues(),
            _failedQueryProcessorMock.Object);

        _failedQueryProcessorMock.Verify(
            x => x.ProcessQueryAsync(It.IsAny<SparqlQuery>()),
            Times.Once
        );
        _httpContext.Response.StatusCode.Should().Be(500);
    }

}