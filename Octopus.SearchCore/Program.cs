using System.Text;
using JiebaNet.Segmenter;
using Lucene.Net.Analysis.Jieba;
using Lucene.Net.Store;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Octopus.SearchCore;
using Octopus.SearchCore.IndexDemo;
using Octopus.SearchCore.Interfaces;
using Octopus.SearchCore.TagSource;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
//分词
var segmenter = new JiebaSegmenter();
var words= segmenter.Cut("男子洗澡时发现舒肤佳香皂内嵌刀");

foreach (var word in words)
{
    Console.WriteLine(word);
}


var option = new LuceneIndexerOptions { Path = "lucene" };
MemoryCacheOptions cacheOps = new MemoryCacheOptions()
{
    //缓存最大为100份
    //##注意netcore中的缓存是没有单位的，缓存项和缓存的相对关系
    SizeLimit = 100,
    //缓存满了时，压缩20%（即删除20份优先级低的缓存项）
    CompactionPercentage = 0.2,
    //两秒钟查找一次过期项
    ExpirationScanFrequency = TimeSpan.FromSeconds(3)
};

var indexer = new LuceneIndexer(FSDirectory.Open(option.Path), new JieBaAnalyzer(TokenizerMode.Search));

var newsList = new List<News>();
newsList.Add(new News()
{
    Id=1,
    Title = "请求回归国家队！皇马昔日标王太唏嘘！32岁辗转10队，或黯然退役",
    Content = "<div>对于哈梅斯-罗德里格斯一些老球迷还是相当记忆犹新的，当年在2014年世界杯上，一脚世界波，J罗也可谓是横空出世。直接成为了夏窗转会市场上最火爆的球员，大量豪门球队都对J罗感兴趣。J罗帅气的外表和强大的实力，最终也让皇马直接为他支付了7500万欧元，用夏窗标王的价格签约下来了他。</div>",
    PostDate = DateTime.Now
});
newsList.Add(new News()
{
    Id=2,
    Title = "曼城锁定16岁天才边锋，人称小梅西，违约金6000万欧，5大豪门来抢",
    Content = "<div>埃斯特旺-威廉，土生土长的巴西球员，2007年4月出生，今年只有16岁，还处于职业球员的起步阶段。别看他身体条件不算太出众，看上去也略显瘦弱，但他天赋过人，脚下技术出色，控球能力一流，另外，作为边锋，他速度快，爆发力足，且还有令人惊叹的前场创造力。\n\n\u3000\u3000值得一提的是，在去年的U17世界杯上，他表现非常出彩，出战5场贡献3球，成为巴西队的关键先生。另外，由于埃斯特旺-威廉和梅西的拼音字母有点相似，大家都叫他足坛小梅西。</div>",
    PostDate = DateTime.Now
});
newsList.Add(new News()
{
    Id=3,
    Title = "又一次百发百中！可惜快船后场大将的战术地位还是太低了一些？",
    Content = "<div>在此前的一场NBA常规赛中，主场作战的洛杉矶快船在末节完成了大逆转，他们以120比118战胜了克利夫兰骑士。本场比赛后，拿下了3连胜的快船的战绩提升到了50胜28负，继续排名西部第4位；而遭遇了3连败的骑士的战绩则是下滑到了46胜33负，排名也是下滑到了东部第5位</div>",
    PostDate = DateTime.Now
});

newsList.Add(new News()
{
    Id=4,
    Title = "4曼城锁定16岁天才边锋，人称小梅西，违约金6000万欧，5大豪门来抢",
    Content = "<div>埃斯特旺-威廉，土生土长的巴西球员，2007年4月出生，今年只有16岁，还处于职业球员的起步阶段。别看他身体条件不算太出众，看上去也略显瘦弱，但他天赋过人，脚下技术出色，控球能力一流，另外，作为边锋，他速度快，爆发力足，且还有令人惊叹的前场创造力。\n\n\u3000\u3000值得一提的是，在去年的U17世界杯上，他表现非常出彩，出战5场贡献3球，成为巴西队的关键先生。另外，由于埃斯特旺-威廉和梅西的拼音字母有点相似，大家都叫他足坛小梅西。</div>",
    PostDate = DateTime.Now
});

newsList.Add(new News()
{
    Id=5,
    Title = "5曼城锁定16岁天才边锋，人称小梅西，违约金6000万欧，5大豪门来抢",
    Content = "<div>埃斯特旺-威廉，土生土长的巴西球员，2007年4月出生，今年只有16岁，还处于职业球员的起步阶段。别看他身体条件不算太出众，看上去也略显瘦弱，但他天赋过人，脚下技术出色，控球能力一流，另外，作为边锋，他速度快，爆发力足，且还有令人惊叹的前场创造力。\n\n\u3000\u3000值得一提的是，在去年的U17世界杯上，他表现非常出彩，出战5场贡献3球，成为巴西队的关键先生。另外，由于埃斯特旺-威廉和梅西的拼音字母有点相似，大家都叫他足坛小梅西。</div>",
    PostDate = DateTime.Now
});

var tagSegmenter = TagUtils.GetSegmenter();
var tagExtractor = new JiebaNet.Analyser.TfidfExtractor(tagSegmenter);

foreach (var n in newsList)
{
    var tags = tagExtractor.ExtractTags(n.Title + " " + n.Content, 10, new List<string> { "role", "scene" });
    if (tags.Any())
    {
        foreach (var tag in tags)
        {
            Console.WriteLine(tag);
        }
        
    }
}

indexer.CreateIndex(newsList,true);

var engine2 = new SearchEngine(FSDirectory.Open(option.Path),new JieBaAnalyzer(TokenizerMode.Search),new MemoryCache(cacheOps));

var searchOpt=new SearchOptions("天才", 1, 10, "Title");
searchOpt.Score = 0.1f;

var result2 = engine2.ScoredSearch<News>(searchOpt);

// var loggerFactory = LoggerFactory.Create(builder =>
// {
//     builder.AddConsole(); // 输出日志到控制台
// });
//
// var logger = loggerFactory.CreateLogger<Program>();

//var engine = new EasyLuceneNetDefaultProvider();

 // var newsList = new List<News2>();
 // newsList.Add(new News2()
 // {
 //     Id=1,
 //     Title = "请求回归国家队！皇马昔日标王太唏嘘！32岁辗转10队，或黯然退役",
 //     Content = "<div>对于哈梅斯-罗德里格斯一些老球迷还是相当记忆犹新的，当年在2014年世界杯上，一脚世界波，J罗也可谓是横空出世。直接成为了夏窗转会市场上最火爆的球员，大量豪门球队都对J罗感兴趣。J罗帅气的外表和强大的实力，最终也让皇马直接为他支付了7500万欧元，用夏窗标王的价格签约下来了他。</div>",
 //     
 // });
 // newsList.Add(new News2()
 // {
 //     Id=2,
 //     Title = "曼城锁定16岁天才边锋，人称小梅西，违约金6000万欧，5大豪门来抢",
 //     Content = "<div>埃斯特旺-威廉，土生土长的巴西球员，2007年4月出生，今年只有16岁，还处于职业球员的起步阶段。别看他身体条件不算太出众，看上去也略显瘦弱，但他天赋过人，脚下技术出色，控球能力一流，另外，作为边锋，他速度快，爆发力足，且还有令人惊叹的前场创造力。\n\n\u3000\u3000值得一提的是，在去年的U17世界杯上，他表现非常出彩，出战5场贡献3球，成为巴西队的关键先生。另外，由于埃斯特旺-威廉和梅西的拼音字母有点相似，大家都叫他足坛小梅西。</div>",
 //     
 // });
 // newsList.Add(new News2()
 // {
 //     Id=3,
 //     Title = "又一次百发百中！可惜快船后场大将的战术地位还是太低了一些？",
 //     Content = "<div>在此前的一场NBA常规赛中，主场作战的洛杉矶快船在末节完成了大逆转，他们以120比118战胜了克利夫兰骑士。本场比赛后，拿下了3连胜的快船的战绩提升到了50胜28负，继续排名西部第4位；而遭遇了3连败的骑士的战绩则是下滑到了46胜33负，排名也是下滑到了东部第5位</div>",
 //     
 // });

 // newsList.Add(new News2()
 // {
 //     Id=4,
 //     Title = "4曼城锁定16岁天才边锋，人称小梅西，违约金6000万欧，5大豪门来抢",
 //     Content = "<div>埃斯特旺-威廉，土生土长的巴西球员，2007年4月出生，今年只有16岁，还处于职业球员的起步阶段。别看他身体条件不算太出众，看上去也略显瘦弱，但他天赋过人，脚下技术出色，控球能力一流，另外，作为边锋，他速度快，爆发力足，且还有令人惊叹的前场创造力。\n\n\u3000\u3000值得一提的是，在去年的U17世界杯上，他表现非常出彩，出战5场贡献3球，成为巴西队的关键先生。另外，由于埃斯特旺-威廉和梅西的拼音字母有点相似，大家都叫他足坛小梅西。</div>",
 // });
 //
 // newsList.Add(new News2()
 // {
 //     Id=5,
 //     Title = "5曼城锁定16岁天才边锋，人称小梅西，违约金6000万欧，5大豪门来抢",
 //     Content = "<div>埃斯特旺-威廉，土生土长的巴西球员，2007年4月出生，今年只有16岁，还处于职业球员的起步阶段。别看他身体条件不算太出众，看上去也略显瘦弱，但他天赋过人，脚下技术出色，控球能力一流，另外，作为边锋，他速度快，爆发力足，且还有令人惊叹的前场创造力。\n\n\u3000\u3000值得一提的是，在去年的U17世界杯上，他表现非常出彩，出战5场贡献3球，成为巴西队的关键先生。另外，由于埃斯特旺-威廉和梅西的拼音字母有点相似，大家都叫他足坛小梅西。</div>",
 //
 // });


 // engine.DeleteAll();
 // engine.AddIndex(newsList);
 //
 // var result = engine!.Search<News2>(new SearchRequest()
 // {
 //     keyword = "天才",
 //     index = 1,
 //     size = 20,
 //     fields = new string[] { "Title", "Content" },
 //     OrderByField = "Id",
 // });

// engine.DeleteIndex();
// engine.CreateIndex();



Console.WriteLine("Hello, World!");