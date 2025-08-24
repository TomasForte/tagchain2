
namespace Model
{
    class ChainNode
    {
        public Item CurrentNode { get; }
        public ChainNode? Parent { get; }
        public Edge? ConnectingEdge { get; }

        public HashSet<int> TagsInChain { get; }

        public List<Edge> ConnectedTo { get; }
        public int ChainSize { get; }

        public ChainNode(Item currentNode, ChainNode? parent = null, Edge? connectingEdge = null)
        {
            CurrentNode = currentNode;
            Parent = parent;
            ConnectingEdge = connectingEdge;

            ChainSize = parent?.ChainSize + 1 ?? 1;
            TagsInChain = new HashSet<int>(parent?.TagsInChain ?? Enumerable.Empty<int>());
            ConnectedTo = currentNode.EdgesOut.Where(e => !TagsInChain.Contains(e.TagId)).ToList();
            if (connectingEdge is not null)
            {
                TagsInChain.Add(connectingEdge.TagId);
            }


        }

        public List<Edge> GetPathEdges()
        {
            List<Edge> path = new List<Edge>();
            ChainNode temp = this;

            while (temp.Parent is not null)
            {
                path.Add(temp.ConnectingEdge);
                temp = temp.Parent;
            }

            return path;
        }

    }
}