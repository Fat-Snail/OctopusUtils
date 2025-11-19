namespace JiebaNet.Segmenter.PosSeg
{
    public class Pair
    {
    public String Word { get; set; }
    public String Flag { get; set; }
    public Pair(String word, String flag)
        {
            Word = word;
            Flag = flag;
        }

        public override String ToString()
        {
            return String.Format("{0}/{1}", Word, Flag);
        }
    }
}