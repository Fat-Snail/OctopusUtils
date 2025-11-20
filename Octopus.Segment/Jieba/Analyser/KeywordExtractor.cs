//using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JiebaNet.Segmenter.Common;

namespace JiebaNet.Analyser
{
    public abstract class KeywordExtractor
    {
        protected static readonly List<String> DefaultStopWords = new List<String>()
        {
            "the", "of", "is", "and", "to", "in", "that", "we", "for", "an", "are",
            "by", "be", "as", "on", "with", "can", "if", "from", "which", "you", "it",
            "this", "then", "at", "have", "all", "not", "one", "has", "or", "that"
        };

        protected virtual ISet<String> StopWords { get; set; }

        public void SetStopWords(String stopWordsFile)
        {
            StopWords = new HashSet<String>();
            var lines = FileExtension.ReadEmbeddedAllLines(stopWordsFile);
            foreach (var line in lines)
            {
                StopWords.Add(line.Trim());
            }
        }

        public abstract IEnumerable<String> ExtractTags(String text, Int32 count = 20, IEnumerable<String> allowPos = null);
        public abstract IEnumerable<WordWeightPair> ExtractTagsWithWeight(String text, Int32 count = 20, IEnumerable<String> allowPos = null);
    }
}