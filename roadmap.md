# OctopusUtils Roadmap

> 当前版本：**v1.5.3**（2026-05-10）  
> 下一里程碑：v1.5.x 长期维护（用户反馈驱动）

---

## 已完成

| 版本 | 时间 | 主要内容 |
|------|------|---------|
| ✅ v1.0.0 | 2024-11 | 全文搜索、中文分词、控制台工具、AI 客户端 |
| ✅ v1.1.0 | 2024-11 | 单元测试工具、MiniProfiler、类型名规范化 |
| ✅ v1.2.0 | 2026-02 | OctopusEx.WebCore 初版：DDD脚手架、Hangfire、审计、敏感词 |
| ✅ v1.2.1 | 2026-02 | DomainCore 完整 CRUD 脚手架 |
| ✅ v1.2.2 | 2026-02 | HealthCheckExtensions，四个健康端点 |
| ✅ v1.2.3 | 2026-03 | WhereIf 条件查询扩展，自动注入最佳实践 |
| ✅ v1.3.0 | 2026-05 | 对象映射（Mapster）、全局异常中间件、软删除（ISoftDelete） |
| ✅ v1.3.1 | 2026-05 | v1.3.0 收尾强化、ICurrentUser、ProjectTo、单元测试项目落地 |
| ✅ v1.3.2 | 2026-05 | 多级缓存（L1+L2+穿透/雪崩/击穿防护）、Hangfire 持久化存储 |
| ✅ v1.3.3 | 2026-05 | JWT 脚手架（含 refresh token）、ASP.NET Core 限流封装 |
| ✅ v1.4.0 | 2026-05 | Microsoft.Extensions.AI 集成、PromptTemplate、ChatHistory、结构化输出 |
| ✅ v1.4.1 | 2026-05 | 向量搜索抽象、内存实现、混合检索（RRF）、Channel 索引管道 |
| ✅ v1.4.2 | 2026-05 | IChineseSegmenter 抽象、词典热加载、新词发现、POS 驱动 NER |
| ✅ v1.5.0 | 2026-05 | 领域事件总线（IEventBus）、IDomainEventCollector、死信队列、自动扫描注册处理器 |
| ✅ v1.5.1 | 2026-05 | 多租户（ICurrentTenant、Header/Query/Subdomain/JWT 解析、EF 全局过滤器） |
| ✅ v1.5.2 | 2026-05 | OctopusEx.Aspire 包：ServiceDefaults、OTLP、HTTP 弹性、服务发现 |
| ✅ v1.5.3 | 2026-05 | RedisEventBus、Outbox、租户连接路由 + Hangfire 队列、Aspire 接线、Telemetry、Benchmarks |

---

## v1.3.x — 工程效率升级

> **主题：** 补全 Web 开发中最高频的横切需求，让脚手架更完整。

### v1.3.0 — 对象映射 + 全局异常处理（预计 2026-07）

**OctopusEx.WebCore**

- **对象映射集成**（`MapperExtensions`）
  - 内置轻量映射引擎，无需手写 `MapToDto` / `MapToEntity`
  - 支持 Mapster / AutoMapper 双适配器，注册时按需选择
  - `CrudServiceBase` 自动接入映射器，子类可选择零配置 CRUD
  - 支持扁平化映射、忽略字段、自定义规则

- **全局异常处理中间件**（`GlobalExceptionMiddleware`）
  - 统一捕获业务异常 / 验证异常 / 未处理异常
  - 按异常类型映射 HTTP 状态码（400 / 401 / 403 / 404 / 422 / 500）
  - 输出标准 `BaseResponse` 格式，与现有控制器响应一致
  - 生产环境自动屏蔽堆栈信息，开发环境完整输出
  - 一行注册：`app.UseGlobalExceptionHandler()`

- **软删除支持**（`ISoftDelete`）
  - 接口标记：`IsDeleted`、`DeletedAt`、`DeletedBy`
  - EF Core 全局查询过滤器自动注入，查询默认过滤已删除数据
  - `DeleteAsync` 自动判断软删除，不需要修改业务代码
  - 提供 `IgnoreSoftDelete()` 扩展方法查询回收站数据

---

### v1.3.1 — v1.3.0 收尾 + 健壮性强化（预计 2026-06）

> **缘起：** v1.3.0 落地后审核发现若干潜在问题与设计可改进项，先把 v1.3.0 打磨完整再进入下一个大功能。

**🔴 Bug 修复**

- `EFRepository.DeleteRangeAsync` 重写，确保批量删除也走软删除流程（避免绕过软删除直接物理删除）
- `GlobalExceptionMiddleware` 检查 `Response.HasStarted`，避免响应已开始时二次写入崩溃
- `GlobalExceptionMiddleware` 显式处理 `OperationCanceledException`，客户端断开返回 499 而非 500
- `GlobalExceptionMiddleware` 改用 `IOptions<JsonOptions>` 序列化，与框架统一命名规则
- `SoftDeleteModelBuilderExtensions` 用 `AndAlso` 合并已有查询过滤器，避免覆盖租户隔离等已有过滤逻辑

**🟡 设计改进**

- **`ICurrentUser` 抽象**（新）
  - 统一封装"当前操作人"获取逻辑（HttpContext / JWT / Hangfire 后台任务）
  - 同时打通 `AuditInterceptor` 与 `EFRepository` 软删除的 `DeletedBy` 自动填充
  - 提供 `NullCurrentUser` 默认实现，无 HttpContext 场景下静默降级

- **`IObjectMapper` 增强**
  - 新增 `ProjectTo<TDest>(IQueryable<TSrc>)` — 在 SQL 层投影列，列表查询性能提升显著
  - 新增 `MapList<TSrc, TDest>` 集合映射快捷方法
  - `AddSimpleMapper(Action<TypeAdapterConfig>? configure)` 提供配置回调，DI 风格自定义映射规则

- **软删除 API 完善**
  - 新增 `IgnoreSoftDelete<T>()` `IQueryable` 扩展（语义比裸 `IgnoreQueryFilters` 清晰）
  - 新增 `EFRepository.RestoreAsync(TKey id)` 恢复软删除实体
  - `EFRepository.DeleteAsync` 自动填充 `DeletedBy`（来自 `ICurrentUser`）

- **异常响应增强**
  - 响应增加 `traceId` 字段（W3C TraceContext / `Activity.Current.Id`），方便排查
  - 开发环境堆栈裁剪到顶层 N 帧，避免响应体过大
  - 新增 `ValidationException` 携带字段级错误 `IDictionary<string, string[]>`，前端可做表单错误高亮
  - `BaseResponse` 新增可选 `Errors` 字段

**🟢 工程质量**

- **单元测试项目落地**（`OctopusEx.WebCore.Tests`）
  - xUnit + FluentAssertions + Moq + EF Core InMemory
  - 覆盖核心：异常映射、软删除流程、Mapster 集成、CrudServiceBase 钩子
  - 路线图技术债"无单元测试项目"由此开始消化

---

### v1.3.2 — 多级缓存（预计 2026-08）

**OctopusEx.WebCore**

- **缓存抽象层**（`ICacheService`）
  - 统一接口：`GetAsync` / `SetAsync` / `RemoveAsync` / `ExistsAsync`
  - L1 内存缓存 + L2 Redis 分布式缓存，自动降级
  - 缓存穿透防护（空值缓存 + BloomFilter）
  - 缓存雪崩防护（随机过期时间 + 互斥锁）
  - 缓存击穿防护(SemaphoreSlim 单飞模式)

- **`[Cache]` 特性装饰器**
  - 标注在 Service 方法上，自动拦截并缓存返回值
  - 支持参数模板 key：`[Cache("user:{0}", ttl: 300)]`
  - 支持手动失效：`[CacheEvict("user:*")]`

- **Hangfire 持久化存储**
  - 当前内存存储 → 支持 Redis / SQL Server / PostgreSQL
  - 一行切换：`AddSimpleHangfire(storage: HangfireStorage.Redis)`
  - 重启后任务不丢失，适合生产环境

---

### v1.3.3 — 限流 + JWT 开箱即用（预计 2026-09）

**OctopusEx.WebCore**

- **请求限流**（`RateLimitExtensions`）
  - 固定窗口 / 滑动窗口 / 令牌桶三种策略
  - 支持 IP、用户、接口维度独立限流
  - 与 ASP.NET Core 原生 Rate Limiting 中间件集成
  - 一行注册：`builder.AddSimpleRateLimit()`

- **JWT 认证脚手架**（`JwtExtensions`）
  - `AddSimpleJwt(secret, issuer)` 一行开启 JWT
  - 内置 `TokenService`：生成 / 刷新 / 吊销 token
  - 刷新 token 支持滑动过期
  - Claims 扩展：`GetUserId()` / `GetUserName()` / `GetRoles()`

---

## v1.4.x — AI 与搜索增强

> **主题：** 拥抱 AI Native 开发范式，让搜索从关键词走向语义理解。

### v1.4.0 — Microsoft.Extensions.AI 集成（预计 2026-11）

**OctopusEx.Tools**

- **重写 AIClient**
  - 迁移至 `Microsoft.Extensions.AI` 标准接口（`IChatClient`）
  - 支持 OpenAI、Azure OpenAI、Ollama、本地 Llama 一致调用
  - 流式输出（Streaming）支持
  - 内置对话历史管理（滑动窗口，防 token 超限）
  - Prompt 模板引擎：变量替换 + 多轮上下文注入

- **结构化输出**
  - `AskAsync<T>()` 泛型方法，AI 返回自动反序列化为强类型对象
  - 内置重试 + 格式校验

**OctopusEx.WebCore**

- **AI 中间件集成**（`AiExtensions`）
  - `AddOctopusAI()` 一行注册，配置注入 `IChatClient`
  - 与 `SensitiveWordFilterPlugin` 深度融合，敏感词检测自动走 AI 语义通道

---

### v1.4.1 — 向量搜索 + 混合检索（预计 2026-12）

**OctopusEx.SearchCore**

- **向量搜索支持**（`IVectorSearchEngine`）
  - 接入 Milvus / Qdrant / PostgreSQL pgvector
  - 文本嵌入：调用本地或远程 Embedding 模型生成向量
  - 语义相似度搜索：`SearchBySimilarity(text, topK)`

- **混合检索**（`HybridSearchEngine`）
  - 关键词（BM25）+ 语义（向量余弦相似度）双路召回
  - RRF（倒数排名融合）重排序
  - 一个接口同时驱动 Lucene 和向量库

- **增量索引管道**
  - 基于 Channel 的异步索引队列，批量写入替代逐条 commit
  - 写入吞吐量提升 10x+

---

### v1.4.2 — 中文分词升级（预计 2027-01）

**OctopusEx.Segment**

- **盘古分词接入**（`PanguSegmenter`）
  - 作为 JiebaSegmenter 的可替换实现
  - 共同实现 `IChineseSegmenter` 接口，运行时按需切换

- **分词质量提升**
  - 基于词频统计的动态词典热更新（无需重启）
  - 新词发现：自动识别高频未登录词并建议加入词典

- **NER 命名实体识别**（`NamedEntityRecognizer`）
  - 识别人名、地名、机构名、时间表达式
  - 基于规则 + HMM 双路实现

---

## v1.5.x — 云原生 + 生态扩展

> **主题：** 让组件库与现代云原生基础设施无缝对接。

### v1.5.0 — 事件总线（预计 2027-03）

**OctopusEx.WebCore**

- **领域事件总线**（`IEventBus`）
  - 内存模式（进程内，零依赖）开箱即用
  - Redis Pub/Sub 模式（跨进程）
  - 事件处理器自动扫描注册（继承 `IEventHandler<T>`）
  - 支持事件溯源（Event Sourcing）基础结构

- **集成事件**
  - 事务提交后自动发布领域事件（与 `IUnitOfWork` 深度集成）
  - 死信队列 + 重试机制
  - 与现有 `AuditInterceptor` 协同，审计变更自动触发事件

---

### v1.5.1 — 多租户支持（预计 2027-04）

**OctopusEx.WebCore**

- **多租户解析**（`ITenantResolver`）
  - 支持子域名 / Header / Query / JWT Claims 解析租户 ID
  - `IMultiTenant` 实体接口，EF Core 全局过滤器自动隔离
  - Hangfire 任务按租户隔离队列

---

### v1.5.2 — .NET Aspire 深度集成（预计 2027-05）

- Aspire ServiceDefaults 集成包（`OctopusEx.Aspire`，新包）
- 开箱即用的 Aspire AppHost 模板
- 统一服务发现 + 配置中心支持

---

### v1.5.3 — 全方位增强（2026-05-10 落地）

**事件总线**
- ✅ `RedisEventBus` + `IRedisEventBusConnection` 抽象（不强绑 StackExchange.Redis）
- ✅ Outbox Pattern：`IOutboxStore` + `InMemoryOutboxStore` + `OutboxDispatcher` 后台服务
- ⏸ 独立 `IEventStore`：Outbox 已覆盖事件溯源主要场景，推迟到有明确需求

**多租户**
- ✅ `ITenantConnectionResolver` + `DictionaryTenantConnectionResolver`（每租户独立数据库路由）
- ✅ `HangfireTenantQueueAttribute` —— 按租户路由到 `tenant-{id}` 队列

**Aspire 深化**
- ✅ `AddOctopusAspireWiring()` —— 自动检测 Redis / 配置中心资源
- ✅ `AddRemoteKvSource` 远程 KV 配置源占位（用户扩展具体 Provider）
- ⏸ AppHost 模板：单独的 `dotnet new` 模板包工程，留待后续

**可观测性**
- ✅ `OctopusTelemetry`：单一 ActivitySource + Meter，5 类核心指标
- ✅ Cache + EventBus 已接入指标
- ✅ Mapster.Tool Source Generator 集成指引（CLI 工具，按需安装）

**性能基线**
- ✅ `tests/OctopusEx.Benchmarks` 项目落地（BenchmarkDotNet 0.14.0）
- ✅ Cache / Mapster / VectorMath 三组基准

**包结构**
- ✅ [docs/PACKAGE-SPLIT-ANALYSIS.md](docs/PACKAGE-SPLIT-ANALYSIS.md)：评估结论"v1.5.x 暂不拆"
- 触发条件已记录，命名空间已按未来拆包预留

---

## 长期技术债

> 不绑定版本，持续改进。

| 项目 | 问题 | 状态 |
|------|------|------|
| `OctopusEx.SearchCore` | 目标框架 net8.0 | ✅ 已升级到 net10（2026-05-10）|
| `OctopusEx.SearchCore` | Lucene.NET 仍为 beta | ⏸ 阻塞于上游：Lucene.NET 4.8 至今仅有 beta00017，无 GA。已订阅，待发布即升级 |
| `OctopusEx.Tools` | `DictionaryCache` 标记 Obsolete 但未提供替代 | ✅ 已重写 Obsolete 消息指向 `ICacheService`（v1.5+ 缓存抽象） |
| 全局 | 1236 个 CS1591 XML 注释警告 | ✅ 已通过 Directory.Build.props 全局抑制（vendored 第三方代码大量缺注释成本过高），新代码 code review 保证完整 |
| 全局 | 无单元测试项目 | ✅ v1.3.1 落地，v1.5.x 已扩展到 105 个测试覆盖核心模块 |
| `ResourceHelper` | 后缀匹配性能低于精确匹配 | ✅ 已在 v1.3.1 改造为 static readonly 缓存 ResourceNames |

---

## 版本节奏

```
v1.2.3 ──► v1.3.0 ──► v1.3.1 ──► v1.3.2 ──► v1.3.3
  已完成    已完成      收尾强化    多级缓存    限流+JWT
  2026-03   2026-05     2026-06    2026-08    2026-09

           ──► v1.4.0 ──► v1.4.1 ──► v1.4.2
                AI集成    向量搜索    分词升级
                2026-11   2026-12    2027-01

           ──► v1.5.0 ──► v1.5.1 ──► v1.5.2
                事件总线   多租户     Aspire
                2027-03   2027-04    2027-05
```

---

## 参与贡献

- 功能建议：提交 [Issue](https://github.com/Fat-Snail/OctopusUtils/issues) 并打 `enhancement` 标签
- 认领任务：查看 [Projects](https://github.com/Fat-Snail/OctopusUtils/projects) 看板
- 代码贡献：Fork → 分支 → PR，提交信息遵循 [Conventional Commits](https://www.conventionalcommits.org/zh-hans/)

> 欢迎通过 Issue 讨论优先级，社区反馈将直接影响版本排期。
