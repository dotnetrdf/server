using VDS.RDF.Configuration;
using VDS.RDF.Query;
using VDS.RDF.Update;

namespace VDS.RDF.Server;

public class ServerConfiguration : IObjectFactory
{
    private const string SparqlQueryEndpoint = "VDS.RDF.Server.SparqlQueryEndpoint";
    private const string SparqlUpdateEndpoint = "VDS.RDF.Server.SparqlUpdateEndpoint";
    public bool TryLoadObject(IGraph g, INode objNode, Type targetType, out object? obj)
    {
        switch (targetType.FullName)
        {
            case SparqlQueryEndpoint:
                return TryLoadSparqlQueryEndpoint(g, objNode, out obj);
            case SparqlUpdateEndpoint:
                return TryLoadSparqlUpdateEndpoint(g, objNode, out obj);
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
            case SparqlUpdateEndpoint:
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

    private bool TryLoadSparqlUpdateEndpoint(IGraph g, INode objNode, out object? obj)
    {
        if (objNode is IUriNode endpointNode && endpointNode.Uri.Scheme.Equals("dotnetrdf"))
        {
            var path = endpointNode.Uri.AbsolutePath;
            var processorProperty = g.GetUriNode(g.UriFactory.Create(ConfigurationLoader.PropertyUpdateProcessor));
            if (processorProperty != null)
            {
                var processorNode = g.GetTriplesWithSubjectPredicate(objNode, processorProperty).Select(t => t.Object)
                    .FirstOrDefault();
                if (processorNode != null)
                {
                    if (ConfigurationLoader.LoadObject(g, processorNode) is ISparqlUpdateProcessor processor)
                    {
                        obj = new SparqlUpdateEndpoint(path, processor);
                        return true;
                    }
                }
            }
        }
        obj = null;
        return false;
    }
}