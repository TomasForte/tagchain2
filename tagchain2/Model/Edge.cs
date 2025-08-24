namespace Model
{
    public class Edge
    {
        public Item From { get; }
        public Item To { get; }
        public int TagId { get; }

        public int MaxChainSize { get; set; }

        public Edge(Item from, Item to, int tagId)
        {
            From = from;
            To = to;
            TagId = tagId;
        }

        public void UpDateMax(int newMax)
        {
            MaxChainSize = newMax;
        }


    }
}