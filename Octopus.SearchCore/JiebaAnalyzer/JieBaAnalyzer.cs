using System.IO;
using JiebaNet.Segmenter;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis.Jieba;

public class JieBaAnalyzer : Analyzer
{
    private readonly TokenizerMode _mode;
    private readonly Boolean _defaultUserDict;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mode"></param>
    /// <param name="defaultUserDict"></param>
    public JieBaAnalyzer(TokenizerMode mode, Boolean defaultUserDict = false)
    {
        _mode = mode;
        _defaultUserDict = defaultUserDict;
    }

    protected override TokenStreamComponents CreateComponents(String filedName, TextReader reader)
    {
        var tokenizer = new JiebaNet.JieBaTokenizer(reader, _mode, _defaultUserDict);
        var tokenstream = new LowerCaseFilter(Lucene.Net.Util.LuceneVersion.LUCENE_48, tokenizer);
        tokenstream.AddAttribute<ICharTermAttribute>();
        tokenstream.AddAttribute<IOffsetAttribute>();
        return new TokenStreamComponents(tokenizer, tokenstream);
    }
}