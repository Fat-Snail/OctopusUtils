# OctopusUtils — Codex 工作指南

## 项目概览

OctopusUtils 是一个 .NET 组件库，面向中文互联网应用场景，聚合了全文搜索、中文分词、异步控制台、重试机制、AI 客户端以及 ASP.NET Core Web 脚手架等工具。

- **公司/版权：** Fatty Coder，2024-2025
- **许可证：** MIT
- **仓库：** https://github.com/Fat-Snail/OctopusUtils
- **强命名密钥：** `octopus-key.snk`（所有项目均已签名）

---

## 解决方案结构

```
OctopusUtils.sln
├── Octopus.Tools/          # netstandard2.0 — 核心工具库
├── Octopus.Segment/        # netstandard2.1 — 中文分词（结巴）
├── Octopus.SearchCore/     # net8.0         — Lucene.NET 全文搜索引擎
└── OctopusEx.WebCore/      # net10.0        — ASP.NET Core Web 脚手架
```

---

## 各项目模块说明

### Octopus.Tools（核心工具）

| 类/模块 | 命名空间 | 说明 |
|---|---|---|
| `ConsoleEx` + `AsyncConsole` | `Octopus` | 异步非阻塞彩色控制台，带 Info/Debug/Warn/Error 日志级别 |
| `ConsoleProgressBar` | `Octopus` | 控制台进度条，支持动画 spinner |
| `Utils.RetryMethod[Async]` | `Octopus` | 同步/异步重试，可配置次数、间隔、回调 |
| `AIClient` | `Octopus` | OpenAI/Llama API 客户端，缓存 chat session |
| `GoogleFileDownloader` | `Octopus` | Google Drive 大文件下载，处理确认跳转 |
| `DictionaryCache<K,V>` | `NewLife` | 泛型带 TTL 缓存，支持自动刷新、并发安全 |
| `StringHelper` | `NewLife` | 字符串扩展：EqualIgnoreCase、Split、GetBytes 等 |
| `MiniProfiler` / `SimplePerformanceProfiler` | `Octopus.UnitTest` | 单元测试性能分析工具 |

### Octopus.Segment（中文分词）

基于 JiebaNet，嵌入式词典资源。

| 类 | 说明 |
|---|---|
| `JiebaSegmenter` | 核心分词引擎，支持精确/全模式，HMM 未登录词识别 |
| `PosSegmenter` | 词性标注 |
| `KeywordExtractor` / `TfidfExtractor` | TF-IDF 关键词提取 |
| `TextRankExtractor` | TextRank 图算法关键词提取 |
| `WordDictionary` | Trie 词典，DAG 构建 |
| `Viterbi` | HMM 维特比解码 |

嵌入资源：`dict.txt`、`idf.txt`、`stopwords.txt`、HMM 概率矩阵 JSON。

### Octopus.SearchCore（全文搜索）

依赖 Lucene.NET 4.8.0-beta 和 Octopus.Segment。

| 接口/类 | 说明 |
|---|---|
| `ISearchEngine` | 主门面，组合 Indexer + Searcher |
| `ILuceneIndexer` | CRUD 索引操作（Add/Delete/CreateIndex） |
| `ILuceneIndexSearcher` | 全文搜索，泛型结果，内存缓存 |
| `ILuceneIndexable` | 实体必须实现 `ToDocument()` |
| `LuceneIndexAttribute` | 标注实体属性参与索引 |
| `JieBaAnalyzer` / `JieBaTokenizer` | Lucene 中文分词 Analyzer |
| `TagUtils` | 标签/分类提取，内置 `tag_role.txt`、`tag_scene.txt` |
| `SearchOptions` | 搜索配置：关键词、分页、排序、字段过滤 |

### OctopusEx.WebCore（Web 脚手架）

目标框架 net10.0，DDD + CQRS 风格。

#### 依赖注入

- `IScopeDependency` / `ISingletonDependency` / `ITransientDependency` — 生命周期标记接口
- `DependencyServiceRegistrar` — 程序集扫描，自动注册服务
- **最佳实践：** 服务接口继承生命周期接口，而非实现类直接继承

#### 领域核心（DomainCore）

| 抽象 | 说明 |
|---|---|
| `IRepository<TEntity, TKey>` | 继承 IQuery + ICommand 的泛型仓储 |
| `IUnitOfWork` | 事务协调器，管理多仓储提交 |
| `CrudServiceBase<TEntity,TKey,TDto,TCreateDto,TUpdateDto>` | CRUD 服务基类，含验证 hook |
| `CURDControllerBase<...>` | CRUD 控制器基类，自动生成 REST 端点 |
| `EFQueryableExtensions.WhereIf()` | 条件查询扩展，多重载 |
| `AuditInterceptor` | SaveChanges 拦截器，记录变更前后值 |

#### 扩展（Extensions/）

| 扩展类 | 功能 |
|---|---|
| `ApiUIExtensions` | Swagger + Scalar UI 集成 |
| `AspireExtensions` | OpenTelemetry 链路追踪 |
| `AuditServiceExtensions` | 审计日志注册 |
| `HangfireExtensions` | 后台任务：一次性/延迟/定时任务 |
| `HealthCheckExtensions` | 健康检查：`/health/ready`、`/health/live`、`/health/full`、`/health` |

#### 插件

- `SensitiveWordFilterPlugin` — 三级检测：ToolGood.Words 快速匹配 → Semantic Kernel AI 识别 → 综合检测

---

## 构建与开发

```bash
# 还原依赖
dotnet restore

# 构建
dotnet build

# 运行（SearchCore 为控制台 Demo）
dotnet run --project Octopus.SearchCore
```

### 代码风格（.editorconfig 强制执行）

- 类型名使用 .NET 完整类型名（`String` 非 `string`，`Int32` 非 `int`，`Boolean` 非 `bool`）
- Husky.NET 提交前自动格式化，提交信息需遵循 Conventional Commits
- 强命名签名：所有程序集已用 `octopus-key.snk` 签名，**不要删除强命名配置**

### NuGet 打包

各项目均配置 NuGet 打包，图标使用 `favicon.png`，生成 `.snupkg` 符号包。

---

## 版本历史摘要

| 版本 | 日期 | 主要内容 |
|---|---|---|
| 1.2.3 | 2026-03-07 | WhereIf 条件查询扩展，自动注入最佳实践文档 |
| 1.2.2 | 2026-02-12 | HealthCheckExtensions，四个健康端点 |
| 1.2.1 | 2026-02-07 | DomainCore 完整 CRUD 脚手架 |
| 1.2.0 | 2026-02-07 | OctopusEx.WebCore 初版：DDD、Hangfire、审计、敏感词 |
| 1.1.0 | 2024-11-19 | 单元测试工具类，MiniProfiler |
| 1.0.0 | 2024-11-18 | 初版：搜索、分词、控制台工具、AI 客户端 |

---

## 关键约定

1. **不要在实现类上直接继承生命周期接口**，应通过服务接口间接继承（见 DependencyServiceRegistrar）。
2. 新增可索引实体须实现 `ILuceneIndexable` 并用 `[LuceneIndex]` 标注属性。
3. 审计日志通过 `AuditInterceptor` 自动捕获，无需在业务代码中手动记录。
4. Hangfire 使用内存存储（`MemoryStorage`），重启后任务丢失，适合非持久化场景。
5. `DictionaryCache` 并发访问安全，过期项自动清理，可替代简单的 `ConcurrentDictionary`。
