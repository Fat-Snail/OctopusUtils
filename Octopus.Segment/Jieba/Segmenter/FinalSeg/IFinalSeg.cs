namespace JiebaNet.Segmenter.FinalSeg;

public interface IFinalSeg
{
    IEnumerable<String> Cut(String sentence);
}
