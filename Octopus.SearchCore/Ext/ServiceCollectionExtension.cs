using JiebaNet.Segmenter;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Jieba;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Octopus.SearchCore.Interfaces;

namespace Octopus.SearchCore.Ext;

public static class ServiceCollectionExtension
{
    /// <summary>
    /// 依赖注入
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    /// <param name="services"></param>
    /// <param name="option"></param>
    public static IServiceCollection AddSearchEngine(this IServiceCollection services, LuceneIndexerOptions option)
    {
        services.AddSingleton(option);
        services.AddMemoryCache();
        services.TryAddSingleton<Lucene.Net.Store.Directory>(s => Lucene.Net.Store.FSDirectory.Open(option.Path));
        services.TryAddSingleton<Analyzer>(s => new JieBaAnalyzer(TokenizerMode.Search));
        //services.TryAddScoped<ILuceneIndexer, LuceneIndexer>();//生成索引暂时由工具负责
        services.TryAddScoped<ILuceneIndexSearcher, LuceneIndexSearcher>();
        //services.TryAddScoped(typeof(ISearchEngine<>), typeof(SearchEngine<>));
        services.TryAddScoped<ISearchEngine, SearchEngine>();
        return services;
    }
}