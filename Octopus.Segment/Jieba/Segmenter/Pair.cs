namespace JiebaNet.Segmenter
{
    public class Pair<TKey>
    {
    public TKey Key { get; set; }
    public Double Freq { get; set; }

        public Pair(TKey key, Double freq)
        {
            Key = key;
            Freq = freq;
        }

        public override String ToString()
        {
            return "Candidate [Key=" + Key + ", Freq=" + Freq + "]";
        }
    }
}