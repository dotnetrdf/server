using System.Net.Http.Headers;
using FluentAssertions;

namespace dotNetRdf.Server.Runner.Tests;

[Collection("Web Application")]
public class SparqlQueryIntegrationTests
{
    private readonly HttpClient _client;

    public SparqlQueryIntegrationTests(WebApplicationFactoryFixture<Program> factory)
    {
        factory.HostUrl = "http://localhost:5001";
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task TestSimpleGetQuery()
    {
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/sparql-results+xml", 0.9)
            );
        var response = await _client.GetAsync("/query?query=SELECT * WHERE { ?s ?p ?o } LIMIT 10");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/sparql-results+xml");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestNotAcceptableResponse()
    {
        _client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/svg+xml", 0.9)
            );
        var response = await _client.GetAsync("/query?query=SELECT * WHERE { ?s ?p ?o } LIMIT 10");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotAcceptable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/plain");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("No acceptable media type found for SPARQL results.");
    }
}