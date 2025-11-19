namespace JiebaNet.Segmenter
{
    public class Token
    {
    public String Word { get; set; }
    public Int32 StartIndex { get; set; }
    public Int32 EndIndex { get; set; }

        public Token(String word, Int32 startIndex, Int32 endIndex)
        {
            Word = word;
            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public override String ToString()
        {
            return String.Format("[{0}, ({1}, {2})]", Word, StartIndex, EndIndex);
        }
    }
}