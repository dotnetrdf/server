using VDS.RDF.Server;
using VDS.RDF.Configuration;
using VDS.RDF.Query.Pull.Configuration;

ConfigurationLoader.AddObjectFactory(new PullQueryProcessorConfigurationFactory());

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDotNetRdfServices();

var app = builder.Build();

app.MapDotNetRdfEndpoints();

Console.WriteLine(app.Urls.FirstOrDefault());

app.Run();