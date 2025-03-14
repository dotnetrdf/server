using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VDS.RDF.Configuration;
using VDS.RDF.Server.Services;

namespace VDS.RDF.Server;

public static class DotNetRdfExtensions
{
    public static void AddDotNetRdfServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IRdfResponseWriter, RdfResponseWriter>();
        serviceCollection.AddSingleton<ISparqlQueryService, SparqlQueryService>();
    }

    /// <summary>
    /// Initialise a set of endpoints for the specified WebApplication from the RDF configuration file referenced
    /// by the app setting at `DotNetRdf.Configuration` (defaulting to a file named `configuration.ttl` at the
    /// ContentRoot of the WebApplication).
    /// </summary>
    /// <param name="app"></param>
    public static void MapDotNetRdfEndpoints(this WebApplication app)
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
    /// Load a service endpoint from the application configuration file and register it with the web application
    /// </summary>
    /// <param name="routeBuilder">The route builder to register the new endpoint with</param>
    /// <param name="configGraph">The RDF graph that contains the application configuration</param>
    /// <param name="serviceEndpointNode">The RDF node in <paramref name="configGraph"/> that defines the endpoint to be registered.</param>
    private static void MapServiceEndpoint(IEndpointRouteBuilder routeBuilder, Graph configGraph, INode serviceEndpointNode)
    {
        if (ConfigurationLoader.LoadObject(configGraph, serviceEndpointNode) is IServiceEndpoint serviceEndpoint)
        {
            serviceEndpoint.Register(routeBuilder);
        }
    }
}