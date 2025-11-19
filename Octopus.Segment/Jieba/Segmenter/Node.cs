namespace JiebaNet.Segmenter
{
    public class Node
    {
    public Char Value { get; private set; }
    public Node Parent { get; private set; }

        public Node(Char value, Node parent)
        {
            Value = value;
            Parent = parent;
        }
    }
}