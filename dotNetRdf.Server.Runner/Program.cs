using VDS.RDF.Server;
using VDS.RDF.Configuration;
using VDS.RDF.Query.Pull.Configuration;

ConfigurationLoader.AddObjectFactory(new PullQueryProcessorConfigurationFactory());

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseDotNetRdf();

Console.WriteLine(app.Urls.FirstOrDefault());

app.Run();