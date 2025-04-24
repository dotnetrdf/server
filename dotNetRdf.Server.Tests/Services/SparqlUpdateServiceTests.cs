using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using VDS.RDF.Query;
using VDS.RDF.Server.Services;
using VDS.RDF.Update;
using VDS.RDF.Update.Commands;

namespace dotNetRdf.Server.Tests.Services;

public class SparqlUpdateServiceTests
{
    private readonly Mock<ISparqlUpdateProcessor> _successfulUpdateProcessorMock;
    private readonly Mock<ISparqlUpdateProcessor> _failedUpdateProcessorMock;
    private readonly Mock<ISparqlUpdateProcessor> _timedOutUpdateProcessorMock;
    private readonly HttpContext _httpContext;

    public SparqlUpdateServiceTests()
    {
        _successfulUpdateProcessorMock = new Mock<ISparqlUpdateProcessor>();
        _successfulUpdateProcessorMock.Setup(x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()));
        _failedUpdateProcessorMock = new Mock<ISparqlUpdateProcessor>();
        _failedUpdateProcessorMock.Setup(x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()))
            .Throws(new RdfQueryException("SPARQL UPDATE ERROR"));
        _timedOutUpdateProcessorMock = new Mock<ISparqlUpdateProcessor>();
        _timedOutUpdateProcessorMock.Setup(x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()))
            .Throws(new RdfQueryTimeoutException("SPARQL UPDATE TIMEOUT"));
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task ItPassesParsedSparqlUpdateToTheUpdateProcessor()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(_httpContext,
            "INSERT DATA { <http://example.org/s> <http://example.org/p> <http://example.org/o> . }",
            new StringValues(),
            new StringValues(),
            _successfulUpdateProcessorMock.Object);

        // Query processor should be called once
        _successfulUpdateProcessorMock.Verify(
            x =>
                x.ProcessCommandSet(
                    It.Is<SparqlUpdateCommandSet>(it =>
                        it.CommandCount == 1
                    )
                ),
            Times.Once
        );
        _httpContext.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ItRespondsWith400OnInvalidSparqlUpdate()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues(),
            new StringValues(),
            _successfulUpdateProcessorMock.Object);
        // Query processor should not have been called
        _successfulUpdateProcessorMock.Verify(
            x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()),
            Times.Never
        );
        _httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ItUpdatesParsedCommandsWithUsingGraphUris()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(
            _httpContext,
            "INSERT { ?s <http://example.org/q> ?o . } WHERE { ?s <http://example.org/p> ?o }",
            new StringValues(["http://example.org/g1", "http://example.org/g2"]),
            new StringValues(),
            _successfulUpdateProcessorMock.Object
        );
        // Query processor should be called with one command in the command set containing the two named graphs in the USING clause
        _successfulUpdateProcessorMock.Verify(
            x =>
                x.ProcessCommandSet(
                    It.Is<SparqlUpdateCommandSet>(it =>
                        it.CommandCount == 1 &&
                        it.Commands.OfType<BaseModificationCommand>().All(cmd =>
                            cmd.UsingUris.Contains(new Uri("http://example.org/g1")) &&
                            cmd.UsingUris.Contains(new Uri("http://example.org/g2")))
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ItUpdatesParsedCommandsWithUsingNamedGraphUris()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(
            _httpContext,
            "INSERT { ?s <http://example.org/q> ?o . } WHERE { ?s <http://example.org/p> ?o }",
            new StringValues(),
            new StringValues(["http://example.org/g1", "http://example.org/g2"]),
            _successfulUpdateProcessorMock.Object
        );
        // Query processor should be called with one command in the command set containing the two named graphs in the USING clause
        _successfulUpdateProcessorMock.Verify(
            x =>
                x.ProcessCommandSet(
                    It.Is<SparqlUpdateCommandSet>(it =>
                        it.CommandCount == 1 &&
                        it.Commands.OfType<BaseModificationCommand>().All(cmd =>
                            cmd.UsingNamedUris.Contains(new Uri("http://example.org/g1")) &&
                            cmd.UsingNamedUris.Contains(new Uri("http://example.org/g2")))
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task ItRespondsWith40OnInvalidDefaultGraphName()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(_httpContext,
            "SELECT * WHERE {?s ?p ?o }",
            new StringValues("bad"),
            new StringValues(),
            _successfulUpdateProcessorMock.Object);
        // Query processor should not have been called
        _successfulUpdateProcessorMock.Verify(
            x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()),
            Times.Never
        );
        _httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ItRespondsWith40OnInvalidNamedGraphName()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(_httpContext,
            "INSERT { ?s <http://example.org/q> ?o . } WHERE { ?s <http://example.org/p> ?o }",
            new StringValues(),
            new StringValues("bad"),
            _successfulUpdateProcessorMock.Object);
        // Query processor should not have been called
        _successfulUpdateProcessorMock.Verify(
            x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()),
            Times.Never
        );
        _httpContext.Response.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task ItRespondsWith504OnQueryTimeout()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(_httpContext,
            "INSERT { ?s <http://example.org/q> ?o . } WHERE { ?s <http://example.org/p> ?o }",
            new StringValues(),
            new StringValues(),
            _timedOutUpdateProcessorMock.Object);

        _timedOutUpdateProcessorMock.Verify(
            x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()),
            Times.Once
        );
        _httpContext.Response.StatusCode.Should().Be(504);
    }
    
    [Fact]
    public async Task ItRespondsWith500OnQueryException()
    {
        var sut = new SparqlUpdateService();
        await sut.ProcessUpdateAsync(_httpContext,
            "INSERT { ?s <http://example.org/q> ?o . } WHERE { ?s <http://example.org/p> ?o }",
            new StringValues(),
            new StringValues(),
            _failedUpdateProcessorMock.Object);

        _failedUpdateProcessorMock.Verify(
            x => x.ProcessCommandSet(It.IsAny<SparqlUpdateCommandSet>()),
            Times.Once
        );
        _httpContext.Response.StatusCode.Should().Be(500);
    }
}