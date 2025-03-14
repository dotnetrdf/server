using VDS.RDF.Configuration;
using VDS.RDF.Query;

namespace VDS.RDF.Server;

public class ServerConfiguration : IObjectFactory
{
    private const string SparqlQueryEndpoint = "VDS.RDF.Server.SparqlQueryEndpoint",
        PathProperty = ConfigurationLoader.ConfigurationNamespace + "path",
        ProcessorProperty = ConfigurationLoader.ConfigurationNamespace + "processor";
    public bool TryLoadObject(IGraph g, INode objNode, Type targetType, out object? obj)
    {
        switch (targetType.FullName)
        {
            case SparqlQueryEndpoint:
                return TryLoadSparqlQueryEndpoint(g, objNode, out obj);
            default:
                obj = null;
                return false;
        }
    }

    public bool CanLoadObject(Type t)
    {
        switch (t.FullName)
        {
            case SparqlQueryEndpoint:
                return true;
            default:
                return false;
        }
    }

    private bool TryLoadSparqlQueryEndpoint(IGraph g, INode objNode, out object? obj)
    {
        if (objNode is IUriNode endpointNode && endpointNode.Uri.Scheme.Equals("dotnetrdf"))
        {
            var path = endpointNode.Uri.AbsolutePath;
            var processorProperty = g.GetUriNode(g.UriFactory.Create(ConfigurationLoader.PropertyQueryProcessor));
            if (processorProperty != null)
            {
                var processorNode = g.GetTriplesWithSubjectPredicate(objNode, processorProperty).Select(t => t.Object)
                    .FirstOrDefault();
                if (processorNode != null)
                {
                    if (ConfigurationLoader.LoadObject(g, processorNode) is ISparqlQueryProcessor processor)
                    {
                        obj = new SparqlQueryEndpoint(path, processor);
                        return true;
                    }
                }
            }
        }

        obj = null;
        return false;
    }
}