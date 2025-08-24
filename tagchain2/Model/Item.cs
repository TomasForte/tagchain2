namespace Model
{
    public class Item
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public HashSet<string> Tags { get; set; }
        public List<Edge> EdgesOut { get; set; }

        public List<Edge> EdgesIn { get; set; }

        public DateOnly Date { get; set; }

        public Item(int id, string title, DateOnly date, HashSet<string>? tags = null)
        {
            Id = id;
            Title = title;
            Tags = tags ?? new HashSet<string>();
            Date = date;
            EdgesOut = new List<Edge>();
        }


        public void AddTag(string tag)
        {

            // creat empty list if null
            Tags ??= new HashSet<string>();

            Tags.Add(tag);
        }


        public void AddEdgesOut(List<Edge> edges)
        {
            EdgesOut = edges;
        }

        public void AddEdgesIn(List<Edge> edges)
        {
            EdgesOut = edges;
        }

    }
} 