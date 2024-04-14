# 全文索引（基于Lucene.net）
现在如果使用全文搜索，一般都会使用Elasticsearch，因为简单易用且功能强大。但是有些简单的项目（博客、企业站、小商城），使用基于Lucene会更合适。

## 参考项目
* [Masuit.LuceneEFCore.SearchEngine](https://github.com/ldqk/Masuit.LuceneEFCore.SearchEngine)
* [EasyLuceneNET](https://github.com/coolqingcheng/EasyLuceneNET)
* [jieba.NET](https://github.com/linezero/jieba.NET)
* [JIEba-netcore](https://github.com/SilentCC/JIEba-netcore)

## 工具类实现的基本功能
- 全文搜索（Lucene.NET 4.8.0-beta00016）
- Tag分类提取
- 中文分词（基于结巴，后期会考虑加入盘古）

## 可能遇到的问题
- 分词报错

    看看是否分词文件txt，json没有选择为EmbeddedSource
类型
- 索引生成不成功

    每次生成索引记得writer.Commit()，不然会存在保存不完全和生成了索引搜索不出来，实在不知道有没有生成成功或者搜索不行，可以直接使用Get({文档Id})的方法进行测试，当然，在LuceneIndexSearcher 178有测试方法

- 项目生成失败
    
    建议使用最新的Visual Studio或者Rider

## 未实现的功能
- 拼音搜索 其实Masuit.LuceneEFCore.SearchEngine已实现，但觉得先去掉一些非必要的依赖就先注释掉代码
- 盘古分词的支持
- 搜索热词提示

Enjoy～  ^_^





## 新生命开发团队
![XCode](https://newlifex.com/logo.png)

新生命团队（NewLife）成立于2002年，是新时代物联网行业解决方案提供者，致力于提供软硬件应用方案咨询、系统架构规划与开发服务。  
团队主导的70多个开源项目已被广泛应用于各行业，Nuget累计下载量高达100余万次。  
团队开发的大数据中间件NewLife.XCode、蚂蚁调度计算平台AntJob、星尘分布式平台Stardust、缓存队列组件NewLife.Redis以及物联网平台FIoT，均成功应用于电力、高校、互联网、电信、交通、物流、工控、医疗、文博等行业，为客户提供了大量先进、可靠、安全、高质量、易扩展的产品和系统集成服务。

我们将不断通过服务的持续改进，成为客户长期信赖的合作伙伴，通过不断的创新和发展，成为国内优秀的IoT服务供应商。

`新生命团队始于2002年，部分开源项目具有20年以上漫长历史，源码库保留有2010年以来所有修改记录`  
网站：https://newlifex.com  
开源：https://github.com/newlifex  
QQ群：1600800/1600838 


