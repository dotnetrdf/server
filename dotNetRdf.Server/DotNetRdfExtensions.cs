using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using VDS.RDF.Configuration;
using VDS.RDF.Query;

namespace VDS.RDF.Server;

public static class DotNetRdfExtensions
{
    /// <summary>
    /// Initialise a set of endpoints for the specified WebApplication from the RDF configuration file referenced
    /// by the app setting at `DotNetRdf.Configuration` (defaulting to a file named `configuration.ttl` at the
    /// ContentRoot of the WebApplication).
    /// </summary>
    /// <param name="app"></param>
    public static void UseDotNetRdf(this WebApplication app)
    {
        // Use the core configuration loader with the server configuration extensions
        ConfigurationLoader.AddObjectFactory(new ServerConfiguration());

        var configPath = app.Configuration.GetSection("DotNetRdf").GetRequiredSection("Configuration").Value ??
                         "configuration.ttl";
        var configFileInfo = app.Environment.ContentRootFileProvider.GetFileInfo(configPath);
        if (!configFileInfo.Exists)
        {
            return;
        }
        
        var configGraph = new Graph();
        configGraph.LoadFromFile(configFileInfo.PhysicalPath);

        INode rdfType =
            configGraph.GetUriNode(configGraph.UriFactory.Create("http://www.w3.org/1999/02/22-rdf-syntax-ns#type"));
        INode httpHandler =
            configGraph.GetUriNode(configGraph.UriFactory.Create(ConfigurationLoader.ConfigurationNamespace + "HttpHandler"));
        if (rdfType != null && httpHandler != null)
        {
            foreach (var httpHandlerNode in configGraph.GetTriplesWithPredicateObject(rdfType, httpHandler)
                         .Select(t => t.Subject))
            {
                MapServiceEndpoint(app, configGraph, httpHandlerNode);
            }
        }
    }

    /// <summary>
    /// Register a SPARQL query endpoint with a WebApplication
    /// </summary>
    /// <param name="app">The application to register the new endpoint with.</param>
    /// <param name="path">The path in the app where the endpoint will be installed.</param>
    /// <param name="sparqlQueryProcessor">The query processor used to handle incoming requests.</param>
    public static void MapSparqlQueryEndpoint(this WebApplication app, string path, ISparqlQueryProcessor sparqlQueryProcessor)
    {
        var endpoint = new SparqlQueryEndpoint(path, sparqlQueryProcessor);
        endpoint.Register(app);
    }

    private static void MapServiceEndpoint(WebApplication app, Graph configGraph, INode serviceEndpointNode)
    {
        if (ConfigurationLoader.LoadObject(configGraph, serviceEndpointNode) is IServiceEndpoint serviceEndpoint)
        {
            serviceEndpoint.Register(app);
        };
    }
}