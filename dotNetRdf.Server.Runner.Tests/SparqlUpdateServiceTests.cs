using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;

namespace dotNetRdf.Server.Runner.Tests;

[Collection("Web Application")]
public class SparqlUpdateServiceTests
{
    private readonly WebApplicationFactory<Program> _factory;
    public SparqlUpdateServiceTests(WebApplicationFactoryFixture<Program> factory)
    {
        _factory = factory;
        factory.HostUrl = "http://localhost:5001";
    }
    
    [Fact]
    public async Task TestSimpleUpdate()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/update");
        request.Content = new StringContent(
            "INSERT DATA { <http://example.org/s1> <http://example.org/p1> <http://example.org/o1> . }",
            Encoding.UTF8,
            "application/sparql-update"
            );
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task TestSimpleFormUpdate()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/update");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            {"update", "INSERT DATA { <http://example.org/s1> <http://example.org/p1> <http://example.org/o1> . }"}
        });
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }
}