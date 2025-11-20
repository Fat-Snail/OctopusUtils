using System.Collections.Generic;
using System.Linq;

namespace JiebaNet.Segmenter
{
    public class Constants
    {
        public static readonly Double MinProb = -3.14e100;

        public static readonly List<String> NounPos = new List<String>() { "n", "ng", "nr", "nrfg", "nrt", "ns", "nt", "nz" };
        public static readonly List<String> VerbPos = new List<String>() { "v", "vd", "vg", "vi", "vn", "vq" };
        public static readonly List<String> NounAndVerbPos = NounPos.Union(VerbPos).ToList();
        public static readonly List<String> IdiomPos = new List<String>() { "i" };
    }
}