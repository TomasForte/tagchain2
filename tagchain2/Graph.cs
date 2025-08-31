using System.IO.Compression;
using Model;


public class Graph
{
    private readonly Dictionary<int, Item> _nodes = new();
    private readonly List<Edge> _edges = new();

    private readonly HashSet<ChainNode> _chains = new();



    public Graph(Dictionary<int, Item> nodes, List<Edge> edges)
    {
        _nodes = nodes;
        _edges = edges;
        HashSet<ChainNode> _chains = new HashSet<ChainNode>(); 
    }
    public void AddItem(Item item)
    {
        if (!_nodes.ContainsKey(item.Id))
            _nodes[item.Id] = item;
    }

    public void AddEdge(Edge edge)
    {
        _edges.Add(edge);
        edge.From.AddEdgesOut(new List<Edge> { edge });
    }

    public List<Edge> GetConnectionsFrom(Item item)
    {
        return _edges.Where(e => e.From.Id == item.Id).ToList();
    }

    public void Start()
    {
        foreach (var node in _nodes)
        {
            _chains.Add(new ChainNode(node.Value));
        }
    }
}
