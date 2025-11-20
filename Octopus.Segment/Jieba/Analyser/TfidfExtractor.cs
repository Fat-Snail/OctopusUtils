using System;
using System.Collections.Generic;
using System.Linq;
using JiebaNet.Segmenter;
using JiebaNet.Segmenter.Common;
using JiebaNet.Segmenter.PosSeg;

namespace JiebaNet.Analyser
{
    public sealed class TfidfExtractor : KeywordExtractor
    {
        private static readonly String DefaultIdfFile = ConfigManager.IdfFile;
        private static readonly Int32 DefaultWordCount = 20;

        private JiebaSegmenter Segmenter { get; set; }
        private PosSegmenter PosSegmenter { get; set; }
        private IdfLoader Loader { get; set; }

        private IDictionary<String, Double> IdfFreq { get; set; }
        private Double MedianIdf { get; set; }

        public TfidfExtractor(JiebaSegmenter segmenter = null)
        {
            Segmenter = segmenter.IsNull() ? new JiebaSegmenter() : segmenter;
            PosSegmenter = new PosSegmenter(Segmenter);
            SetStopWords(ConfigManager.StopWordsFile);
            if (StopWords.IsEmpty())
            {
                StopWords.UnionWith(DefaultStopWords);
            }

            Loader = new IdfLoader(DefaultIdfFile);

            IdfFreq = Loader.IdfFreq;
            MedianIdf = Loader.MedianIdf;
        }

        public void SetIdfPath(String idfPath)
        {
            Loader.SetNewPath(idfPath);
            IdfFreq = Loader.IdfFreq;
            MedianIdf = Loader.MedianIdf;
        }

        private IEnumerable<String> FilterCutByPos(String text, IEnumerable<String> allowPos)
        {
            var posTags = PosSegmenter.Cut(text).Where(p => allowPos.Contains(p.Flag));
            return posTags.Select(p => p.Word);
        }

        private IDictionary<String, Double> GetWordIfidf(String text, IEnumerable<String> allowPos)
        {
            IEnumerable<String> words = null;
            if (allowPos.IsNotEmpty())
            {
                words = FilterCutByPos(text, allowPos);
            }
            else
            {
                words = Segmenter.Cut(text);
            }

            // Calculate TF
            var freq = new Dictionary<String, Double>();
            foreach (var word in words)
            {
                var w = word;
                if (String.IsNullOrEmpty(w) || w.Trim().Length < 2 || StopWords.Contains(w.ToLower()))
                {
                    continue;
                }
                freq[w] = freq.GetDefault(w, 0.0) + 1.0;
            }
            var total = freq.Values.Sum();
            foreach (var k in freq.Keys.ToList())
            {
                freq[k] *= IdfFreq.GetDefault(k, MedianIdf) / total;
            }

            return freq;
        }

        public override IEnumerable<String> ExtractTags(String text, Int32 count = 20, IEnumerable<String> allowPos = null)
        {
            if (count <= 0) { count = DefaultWordCount; }

            var freq = GetWordIfidf(text, allowPos);
            return freq.OrderByDescending(p => p.Value).Select(p => p.Key).Take(count);
        }

        public override IEnumerable<WordWeightPair> ExtractTagsWithWeight(String text, Int32 count = 20, IEnumerable<String> allowPos = null)
        {
            if (count <= 0) { count = DefaultWordCount; }

            var freq = GetWordIfidf(text, allowPos);
            return freq.OrderByDescending(p => p.Value).Select(p => new WordWeightPair()
            {
                Word = p.Key,
                Weight = p.Value
            }).Take(count);
        }
    }

    public class WordWeightPair
    {
        public String Word { get; set; }
        public Double Weight { get; set; }
    }
}