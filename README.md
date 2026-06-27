<div align="center">

<img src="favicon.png" width="96" height="96" alt="OctopusUtils Logo" />

# OctopusUtils

**面向中文互联网场景的 .NET 组件库**

聚合全文搜索 · 中文分词 · 异步控制台 · 重试机制 · AI 客户端 · Web 脚手架

[![OctopusEx.Tools](https://img.shields.io/nuget/v/OctopusEx.Tools?style=flat-square&logo=nuget&label=OctopusEx.Tools&color=004880)](https://www.nuget.org/packages/OctopusEx.Tools)
[![OctopusEx.Segment](https://img.shields.io/nuget/v/OctopusEx.Segment?style=flat-square&logo=nuget&label=OctopusEx.Segment&color=004880)](https://www.nuget.org/packages/OctopusEx.Segment)
[![OctopusEx.SearchCore](https://img.shields.io/nuget/v/OctopusEx.SearchCore?style=flat-square&logo=nuget&label=OctopusEx.SearchCore&color=004880)](https://www.nuget.org/packages/OctopusEx.SearchCore)
[![OctopusEx.WebCore](https://img.shields.io/nuget/v/OctopusEx.WebCore?style=flat-square&logo=nuget&label=OctopusEx.WebCore&color=004880)](https://www.nuget.org/packages/OctopusEx.WebCore)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OctopusEx.WebCore?style=flat-square&logo=nuget&label=downloads&color=004880)](https://www.nuget.org/packages/OctopusEx.WebCore)
[![License: MIT](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-Standard%202.0%20%7C%20net8%20%7C%20net10-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com)
[![Build](https://img.shields.io/github/actions/workflow/status/Fat-Snail/OctopusUtils/dotnet.yml?style=flat-square&logo=github)](https://github.com/Fat-Snail/OctopusUtils/actions)

[快速开始](#-快速开始) · [功能模块](#-功能模块) · [文档](#-文档) · [更新日志](CHANGELOG.md) · [贡献指南](#-贡献)

<br />

[![OctopusUtils 介绍](docs/intro.gif)](docs/intro.mp4)

<sub>▶️ 点击查看 <a href="docs/intro.mp4">高清完整版 (MP4)</a></sub>

</div>

---

## ✨ 特性概览

<table>
<tr>
<td width="50%">

**🔍 全文搜索引擎**
基于 Lucene.NET 4.8，原生中文分词支持，支持模糊搜索、多字段权重、分页排序。

</td>
<td width="50%">

**✂️ 中文分词**
移植自结巴分词，内置 HMM 未登录词识别，支持 TF-IDF / TextRank 关键词提取。

</td>
</tr>
<tr>
<td>

**⚡ 异步彩色控制台**
非阻塞队列写入，内置 Info / Debug / Warn / Error 四级日志，带时间戳前缀。

</td>
<td>

**🔄 智能重试机制**
同步/异步通用，可配置次数、间隔、回调，一行代码包装任意操作。

</td>
</tr>
<tr>
<td>

**🤖 AI 客户端**
兼容 OpenAI / Llama API，内置会话缓存，支持批量翻译、自动写作等场景。

</td>
<td>

**🏗️ Web 脚手架**
DDD + CQRS + Repository，自动依赖注入、审计日志、健康检查、Hangfire 一键接入。

</td>
</tr>
</table>

---

## 📦 快速开始

### 安装

根据需要选择安装对应包：

```bash
# 核心工具（控制台、重试、AI客户端、缓存）
dotnet add package OctopusEx.Tools

# 中文分词
dotnet add package OctopusEx.Segment

# 全文搜索引擎
dotnet add package OctopusEx.SearchCore

# ASP.NET Core Web 脚手架
dotnet add package OctopusEx.WebCore
```

### 从源码构建

```bash
git clone https://github.com/Fat-Snail/OctopusUtils.git
cd OctopusUtils
dotnet restore && dotnet build
```

---

## 🗂️ 功能模块

### Octopus.Tools · `netstandard2.0`

<details>
<summary><b>⚡ 异步彩色控制台 (ConsoleEx)</b></summary>

```csharp
using Octopus;

// 带时间戳的日志级别输出
ConsoleEx.Info("服务启动成功");                    // [INFO]  绿色
ConsoleEx.Debug("用户 ID = 12345");               // [DEBUG] 蓝色
ConsoleEx.Warn("磁盘剩余空间不足 10%");            // [WARN]  黄色
ConsoleEx.Error("数据库连接失败，请检查配置");       // [ERROR] 红色

// 自定义颜色
ConsoleEx.WriteLine("处理完成 ✓", ConsoleColor.Cyan);

// 应用退出前优雅关闭（等待队列清空）
await ConsoleEx.ShutdownAsync(timeout: 3000);
```

> 写入操作通过内部 `BlockingCollection` 队列异步消费，**不阻塞主线程**。

</details>

<details>
<summary><b>🔄 智能重试 (Utils.RetryMethod)</b></summary>

```csharp
using Octopus;

// 同步重试（有返回值）
var result = Utils.RetryMethod(
    () => FetchDataFromApi(),
    maxRetryCount: 5,
    sleepTime: 500,
    onRetry: (attempt, ex) => ConsoleEx.Warn($"第 {attempt} 次重试: {ex.Message}")
);

// 异步重试（无返回值）
await Utils.RetryMethodAsync(async () =>
{
    await UploadFileAsync();
}, maxRetryCount: 3, throwOnFailure: true);
```

</details>

<details>
<summary><b>📊 控制台进度条 (ConsoleProgressBar)</b></summary>

```csharp
using var progress = new ConsoleProgressBar();

for (int i = 0; i <= 100; i++)
{
    progress.Report(i / 100.0);   // [########--] 80% |
    await Task.Delay(50);
}
```

</details>

<details>
<summary><b>🤖 AI 客户端 (AIClient)</b></summary>

```csharp
using Octopus;

// 全局配置（一次即可）
AIClient.SetClientParams(s =>
{
    s.ApiDomain = "https://api.openai.com";
    s.ApiKey    = "sk-...";
    s.DefaultModel = "gpt-4o-mini";
});

// 获取客户端（按名称缓存）
var client = AIClient.CreateAiChat("translator");
var response = await client.CreateChatCompletionAsync(
    client.CreateNormalRequest(req =>
    {
        req.Messages = [new() { Role = "user", Content = "将以下文本翻译为英文：你好世界" }];
    })
);

Console.WriteLine(response.Choices[0].Message.Content);
```

</details>

<details>
<summary><b>🗃️ TTL 缓存 (DictionaryCache)</b></summary>

```csharp
using NewLife;

var cache = new DictionaryCache<String, UserInfo>
{
    Expire      = 300,   // 300 秒过期
    Asynchronous = true  // 过期后后台刷新，旧值继续可用
};

var user = cache.GetItem("user:42", key => LoadUserFromDb(key));
```

</details>

---

### Octopus.Segment · `netstandard2.1`

<details>
<summary><b>✂️ 中文分词 (JiebaSegmenter)</b></summary>

```csharp
using JiebaNet.Segmenter;

var seg = new JiebaSegmenter();

// 精确模式（默认）
var words = seg.Cut("我来到北京清华大学");
// → ["我", "来到", "北京", "清华大学"]

// 搜索引擎模式（更高召回）
var searchWords = seg.CutForSearch("小明硕士毕业于中国科学院");
// → ["小明", "硕士", "毕业", "于", "中国", "科学", "学院", "中国科学院"]

// 动态添加自定义词
seg.AddWord("OctopusUtils", freq: 1000, tag: "eng");
```

</details>

<details>
<summary><b>🔑 关键词提取</b></summary>

```csharp
using JiebaNet.Analyser;

// TF-IDF 提取
var tfidf = new TfidfExtractor();
var keywords = tfidf.ExtractTagsWithWeight(
    "此次发布包含多项性能优化和安全修复", topK: 5);
// → [("性能优化", 0.48), ("安全修复", 0.35), ...]

// TextRank 图算法提取（无需语料库）
var textrank = new TextRankExtractor();
var tags = textrank.ExtractTags(article, topK: 10);
```

</details>

---

### Octopus.SearchCore · `net8.0`

<details>
<summary><b>🔍 全文搜索引擎</b></summary>

**第一步：定义可索引实体**

```csharp
using Octopus.SearchCore;

public class Article : LuceneIndexableBaseEntity
{
    [LuceneIndex] public String Title   { get; set; }
    [LuceneIndex] public String Content { get; set; }
    [LuceneIndex] public String Author  { get; set; }

    public override Document ToDocument()
    {
        var doc = new Document();
        doc.AddTextField("Title",   Title,   Field.Store.YES);
        doc.AddTextField("Content", Content, Field.Store.NO);
        doc.AddStringField("Author", Author,  Field.Store.YES);
        return doc;
    }
}
```

**第二步：注册服务并构建索引**

```csharp
services.AddLuceneSearchEngine<Article>(options =>
{
    options.IndexPath = "wwwroot/indexes/article";
});

// 全量建索引
await searchEngine.CreateIndex();
```

**第三步：搜索**

```csharp
// 按关键词搜索，支持 "短语" 和 -排除词
var results = searchEngine.ScoredSearch<Article>(new SearchOptions(
    keywords : "人工智能 -广告",
    page     : 1,
    size     : 20,
    fields   : ["Title", "Content"]
));

Console.WriteLine($"共命中 {results.TotalHits} 条，耗时 {results.Elapsed.TotalMilliseconds:F0}ms");
foreach (var item in results.Results)
    Console.WriteLine($"[{item.Score:F2}] {item.Entity.Title}");
```

</details>

---

### OctopusEx.WebCore · `net10.0`

<details>
<summary><b>🏗️ 自动依赖注入</b></summary>

```csharp
// 1. 服务接口继承生命周期接口（推荐方式）
public interface IOrderService : IScopeDependency
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request);
}

// 2. 实现类只继承业务接口
public class OrderService : IOrderService
{
    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request) { ... }
}

// 3. 一行代码开启扫描注册，无需手动 AddScoped
builder.Host.AddUtil();
// OrderService 自动以 IOrderService 注册为 Scoped
```

</details>

<details>
<summary><b>🗄️ CRUD 脚手架（Repository + Service + Controller）</b></summary>

```csharp
// Entity
public class Product : BaseEntity<Int32> { ... }

// Service — 只需重写映射，CRUD 逻辑全部继承
public class ProductService
    : CrudServiceBase<Product, Int32, ProductDto, CreateProductDto, UpdateProductDto>
{
    protected override ProductDto MapToDto(Product entity) => new() { ... };
    protected override Product MapToEntity(CreateProductDto dto) => new() { ... };
    protected override Int32 GetEntityId(Product e) => e.Id;
    protected override Int32 GetUpdateRequestId(UpdateProductDto dto) => dto.Id;

    // 可选：删除前业务校验
    protected override async Task<DeleteCheckResult> CanDeleteAsync(Int32 id)
    {
        var hasOrders = await orderRepo.ExistsAsync(o => o.ProductId == id);
        return hasOrders
            ? DeleteCheckResult.NotAllowed("存在关联订单，无法删除")
            : DeleteCheckResult.Allowed();
    }
}

// Controller — 七个 REST 端点自动生成
[ApiController, Route("api/products")]
public class ProductController
    : CURDControllerBase<Product, Int32, ProductDto, CreateProductDto, UpdateProductDto>
{
    public ProductController(IProductService service) : base(service) { }
}
```

生成的端点：

| 方法 | 路径 | 说明 |
|------|------|------|
| `GET` | `/api/products/{id}` | 获取单个 |
| `GET` | `/api/products` | 分页列表 |
| `GET` | `/api/products/all` | 全量获取 |
| `POST` | `/api/products` | 创建 |
| `PUT` | `/api/products/{id}` | 更新 |
| `DELETE` | `/api/products/{id}` | 删除 |
| `POST` | `/api/products/bulk-delete` | 批量删除 |

</details>

<details>
<summary><b>📋 审计日志 (AuditInterceptor)</b></summary>

```csharp
// Program.cs — 一行注册，SaveChanges 自动捕获所有变更
builder.Services.AddAuditService(config =>
{
    config.GetCurrentUser = sp =>
    {
        var http = sp.GetRequiredService<IHttpContextAccessor>();
        return new AuditUser
        {
            UserId   = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = http.HttpContext?.User.Identity?.Name
        };
    };
});
```

每次 `SaveChangesAsync()` 后自动写入：

```json
{
  "EntityName": "Product",
  "Action": "UPDATE",
  "OldValues": { "Price": 99.00 },
  "NewValues": { "Price": 129.00 },
  "ChangedProperties": ["Price"],
  "UserId": "u-001",
  "Timestamp": "2026-05-09T10:30:00Z"
}
```

</details>

<details>
<summary><b>⏰ 后台任务 (HangfireExtensions)</b></summary>

```csharp
// 注册（内存存储，零依赖）
builder.Services.AddSimpleHangfire(workerCount: 2);

// 一次性任务
provider.AddBackgroundJob("send-welcome-email", async () =>
    await emailService.SendWelcomeAsync(userId));

// 延迟任务
provider.AddDelayedJob("expire-coupon", async () =>
    await couponService.ExpireAsync(couponId), delay: TimeSpan.FromHours(24));

// 定时任务（Cron 表达式）
provider.AddRecurringJob("daily-report", async () =>
    await reportService.GenerateDailyAsync(), cronExpression: "0 8 * * *");
```

</details>

<details>
<summary><b>🏥 健康检查 (HealthCheckExtensions)</b></summary>

```csharp
// 注册
builder.Services
    .AddDatabaseHealthCheck(connectionString, "sqlserver")
    .AddExternalApiHealthCheck("payment-gateway", "https://pay.example.com/ping")
    .AddCacheHealthCheck("redis", sp => sp.GetRequiredService<IDistributedCache>());

// 映射端点
app.MapHealthCheckEndpoints();
```

| 端点 | 用途 |
|------|------|
| `GET /health/ready` | K8s 就绪探针 |
| `GET /health/live` | K8s 存活探针 |
| `GET /health/full` | 全项检查 |
| `GET /health` | 详细状态（含耗时 / 描述） |

</details>

<details>
<summary><b>🛡️ 敏感词过滤 (SensitiveWordFilterPlugin)</b></summary>

```csharp
var filter = new SensitiveWordFilterPlugin(kernel); // kernel 可选，不传则仅词典模式

// 快速检测（ToolGood.Words，毫秒级）
var result1 = filter.DetectSensitiveWords(text);

// AI 语义检测（Semantic Kernel）
var result2 = await filter.DetectSensitiveWordsWithAI(text);

// 综合检测（词典 → AI 兜底）
var result3 = await filter.ComprehensiveDetectSensitiveWords(text);

// 管理词库
filter.SetSensitiveWords(["词1", "词2"]);
filter.AddSensitiveWord("新词");
```

</details>

---

## 🏛️ 架构总览

```
OctopusUtils.sln
│
├── Octopus.Tools          [netstandard2.0]  控制台 · 重试 · AI客户端 · 缓存 · 字符串扩展
├── Octopus.Segment        [netstandard2.1]  结巴分词 · TF-IDF · TextRank · 词性标注
├── Octopus.SearchCore     [net8.0]          Lucene.NET 全文搜索 · Tag提取 · 中文分析器
└── OctopusEx.WebCore      [net10.0]         DDD脚手架 · 自动DI · 审计 · 健康检查 · Hangfire
         │
         ├── Dependency/        自动依赖注入（生命周期接口 + 程序集扫描）
         ├── DomainCore/        IRepository · IUnitOfWork · CrudServiceBase · WhereIf
         ├── Extensions/        ApiUI · Aspire · Audit · Hangfire · HealthCheck
         └── Plugins/           SensitiveWordFilterPlugin
```

**WebCore 数据流**

```
HTTP Request
  └─► CURDControllerBase    路由 / 统一响应格式
        └─► CrudServiceBase  验证 / 生命周期钩子 / DTO映射
              └─► IRepository (IQuery + ICommand)
                    └─► IUnitOfWork.SaveChangesAsync()
                          └─► AuditInterceptor  ──► AuditLog
```

---

## 📖 文档

| 文档 | 说明 |
|------|------|
| [CHANGELOG.md](CHANGELOG.md) | 版本更新日志 |
| [REQUIREMENTS.md](REQUIREMENTS.md) | 完整功能需求文档 |
| [Search.md](Search.md) | 全文搜索使用指南 |
| [AIClient.md](AIClient.md) | AI 客户端使用指南 |
| [Google.md](Google.md) | Google Drive 下载指南 |
| [ConsoleShow.md](ConsoleShow.md) | 控制台进度条指南 |
| [UnitTest.md](UnitTest.md) | 单元测试工具指南 |
| [HUSKY.md](HUSKY.md) | 代码提交质量检查 |
| [OctopusEx.WebCore/README.md](OctopusEx.WebCore/README.md) | Web 脚手架详细文档 |

---

## 🗺️ 路线图

查看 [roadmap.md](roadmap.md) 了解后续计划，欢迎通过 [Issue](https://github.com/Fat-Snail/OctopusUtils/issues) 提交建议。

---

## 🤝 贡献

欢迎任何形式的贡献！

1. Fork 本仓库
2. 创建特性分支 `git checkout -b feat/your-feature`
3. 提交变更（遵循 [Conventional Commits](https://www.conventionalcommits.org/zh-hans/)）
4. 发起 Pull Request

本项目使用 [Husky.NET](HUSKY.md) 在提交时自动格式化代码，首次克隆后运行：

```bash
dotnet tool restore
dotnet husky install
```

---

## 📄 许可证

本项目基于 [MIT License](LICENSE) 开源。

Copyright © 2024–2025 [Fatty Coder](https://github.com/Fat-Snail)

---

<div align="center">

如果这个项目对你有帮助，欢迎点个 ⭐ Star 支持一下！

</div>
