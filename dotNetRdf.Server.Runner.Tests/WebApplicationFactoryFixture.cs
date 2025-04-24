using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace dotNetRdf.Server.Runner.Tests;

public class WebApplicationFactoryFixture<TEntryPoint>: WebApplicationFactory<TEntryPoint> 
    where TEntryPoint : class
{
    public string HostUrl { get; set; } = "https://localhost:5001"; // we can use any free port

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseUrls(HostUrl);
    }

    /*
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var dummyHost = builder.Build();

        builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

        var host = builder.Build();
        host.Start();

        return dummyHost;
    }
    */
}