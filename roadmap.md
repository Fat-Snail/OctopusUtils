# OctopusUtils Roadmap

> 当前版本：**v1.5.5**（2026-07-07，健康检查 + 诊断 + 示例项目）
> 下一里程碑：**v1.5.6** —— 分布式协调（预计 2026-08）

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
| ✅ v1.5.4 | 2026-06 | 持久化层补全（EFOutboxStore、EFAuditStore）、幂等性（IdempotencyMiddleware、EF/Redis 实现） |
| ✅ v1.5.5 | 2026-07 | 模块健康检查（Cache/EventBus/Outbox/Tenant）、诊断端点（/octopus/diagnostics）、Hangfire 多租户 Dashboard、示例项目（WebApi + Worker） |

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

### v1.5.4 — 持久化层补全 + 幂等性（预计 2026-06）

> **主题：** 把 v1.5.0–v1.5.3 留下的"仅 InMemory"实现升级为生产可用的持久化实现，并补齐配套的幂等性保证。

**Outbox 持久化**
- `EFOutboxStore`（`OctopusEx.WebCore`）—— EF Core 实现，与业务事务同 `DbContext` 同事务落库
  - 自动生成 `outbox_messages` 表；用户 `DbContext.OnModelCreating` 调用 `modelBuilder.AddOctopusOutbox()` 接入
  - 支持 SQL Server / PostgreSQL / SQLite，按各 DB 的 `RowVersion` / `xmin` 做乐观并发
  - `FetchPendingAsync` 用 `FOR UPDATE SKIP LOCKED`（PG）/ `READPAST + UPDLOCK`（MSSQL）保证多 dispatcher 实例不冲突

**审计日志持久化**
- `EFAuditStore`（替换 v1.2 的占位实现）—— `AuditInterceptor` 写入持久存储
- 按租户 / 时间分区索引
- 保留期清理后台任务（每日凌晨）

**幂等性保证**
- `IIdempotencyStore` 抽象 + `EFIdempotencyStore` / `RedisIdempotencyStore` 实现
- `IdempotencyMiddleware`：基于 `Idempotency-Key` 请求头去重（标准 RFC 草案）
- 事件消费幂等：`IEventHandler` 装饰器 `[Idempotent]` 自动按 `EventId` 去重
- 配合 Outbox "至少一次"语义，避免重复消费

**Outbox 重试策略可配**
- `OutboxOptions.RetryStrategy = Linear | Exponential | ExponentialWithJitter`
- 失败后 next-retry 时间字段持久化，dispatcher 跳过未到期消息

---

### v1.5.5 — 健康检查、诊断与示例项目（2026-07-07 落地）

> **主题：** 提升运维可观测性与开发者上手体验。

**模块健康检查**（基于 `IHealthCheck`，挂到 v1.2 已有的 `/health/full` 端点）
- ✅ `OctopusCacheHealthCheck` —— 基于 ICacheService 的真实连通性检测（读写测试 key）
- ✅ `EventBusHealthCheck` —— DeadLetterQueue size 阈值告警（Degraded 10 / Unhealthy 100）
- ✅ `OutboxHealthCheck` —— 待处理消息积压告警（Degraded 100 / Unhealthy 500）
- ✅ `TenantHealthCheck` —— 检查 `ICurrentTenant` / `ITenantConnectionResolver` 已正确注册

**诊断端点**
- ✅ `app.MapOctopusDiagnostics()` —— 暴露 `/octopus/diagnostics`（Development 自动开启，Production 需显式授权）
- ✅ 输出：缓存状态、Outbox 积压、DeadLetter 列表、当前 ICurrentUser/ICurrentTenant
- ✅ JSON + 简单 HTML 视图（根据 Accept 头自动选择）

**Hangfire Dashboard 多租户扩展**
- ✅ `app.UseHangfireTenantDashboard()` —— 注入租户上下文，按 TenantId 过滤展示
- ✅ admin 角色可见所有租户，非 admin 仅可见自己租户的任务
- ✅ 支持 `?tenant=` query / cookie 切换租户视图

**示例项目**
- ✅ `samples/OctopusEx.Sample.WebApi` —— 完整 demo：JWT + 多租户 + 软删除 + Mapster + Hangfire + 事件总线 + Outbox + 健康检查 + 诊断
- ✅ `samples/OctopusEx.Sample.Worker` —— 后台服务 demo：事件总线集成，演示独立 Worker 如何处理领域事件

---

### v1.5.6 — 分布式协调（预计 2026-08）

> **主题：** 让 OctopusEx.WebCore 在多实例部署下具备可靠、可观测的协调能力。

**范围原则**
- 本版聚焦 Redis 单后端的生产可用能力，不同时引入数据库锁、读写分离等高复杂度特性
- 所有协调 key 默认包含应用名与环境名，避免不同应用或环境相互污染
- Redis 故障时支持 `FailOpen` / `FailClosed` 策略，并为各功能给出安全默认值

**发布工程（先行门禁）**
- GitHub Actions 从干净检出执行 `restore → build → test → pack`，Sample 项目必须纳入解决方案验证
- 增加本地 NuGet 源安装冒烟测试，验证 Tools → WebCore、Segment → SearchCore 的包依赖闭环
- 校验每个 `.nupkg` 的版本、依赖版本、README、图标和符号包；README 禁止使用 NuGet 不支持的结构性 HTML
- 保持“产品线前缀 + `yyyy.MMdd`”版本规则，并输出可预测的发布清单和依赖发布顺序

**分布式锁**
- ✅ `IDistributedLockProvider` + `IDistributedLockHandle : IAsyncDisposable` 抽象
- ✅ `InMemoryDistributedLockProvider`（单实例 / 测试）
- ✅ 租约过期恢复、等待超时、取消令牌、自动续租与幂等释放
- `RedisDistributedLockProvider`（租约 TTL + 自动续期）
- 支持等待超时、租约时间、取消令牌与租约丢失状态
- Redis 释放锁使用原子 Compare-and-Delete，防止误删其他实例重新获取的锁
- Outbox Dispatcher、审计清理及缓存维护任务可选择通过分布式锁保证单活

**缓存模式失效（P1）**
- `ICacheService.RemoveByPatternAsync(pattern)`，先稳定服务能力，再考虑方法拦截特性
- 内存实现维护 key 索引；Redis 实现使用 `SCAN + UNLINK`，禁止使用阻塞式 `KEYS`
- `[CacheEvict]` 延后到统一拦截器机制成熟后实现

**分布式 Rate Limiter**
- `RedisRateLimiter` 首版实现固定窗口，使用 Lua 保证计数与过期设置原子性
- 支持 IP、用户、租户三个限流维度
- 沿用 `AddSimpleRateLimit` API，仅切换 backend
- 超限返回标准 `429`、`Retry-After` 与统一 `BaseResponse` 响应

**可观测性与诊断**
- 指标：锁获取成功、等待超时、租约丢失、持锁时长、限流通过与拒绝次数
- Activity 只记录哈希或分类后的业务 key，避免标签高基数和敏感数据泄漏
- `/octopus/diagnostics` 显示锁与限流后端状态，不暴露 Redis 地址或凭据

**验收标准**
- 两个独立进程竞争同一把锁时，同一时刻只有一个进入临界区
- 持锁进程异常退出后，锁可在租约到期后恢复；续期失败时 handle 明确进入 `LeaseLost` 状态
- 100 并发请求下 Redis 固定窗口计数不超发
- Redis 不可用时 `FailOpen` / `FailClosed` 行为均有集成测试
- 单元测试覆盖锁状态机，并使用真实 Redis 完成跨进程集成测试
- Sample 演示“跨实例后台任务单活”和“按租户限流”
- `dotnet build`、全部测试与 NuGet pack 通过，新增公共 API 具备 XML 文档

**实施顺序**
1. **M0 发布基线**：补齐 clean-checkout、pack、README 和本地源安装验证
2. **M1 API 冻结**：确定锁、租约、故障策略、key 命名和限流配置接口
3. **M2 单机实现**：完成 InMemory 锁、状态机、并发与取消测试
4. **M3 Redis 锁**：完成原子获取/释放、自动续期、租约丢失与跨进程测试
5. **M4 业务接入**：为 Outbox 和维护任务增加可选单活模式，接入 Telemetry 与诊断端点
6. **M5 分布式限流**：完成 Redis 固定窗口、租户维度、`Retry-After` 与故障策略测试
7. **M6 发布候选**：视进度实现缓存模式失效，补齐 Sample、迁移文档、包安装测试和发布清单

**建议 API 形态（评审草案）**
```csharp
await using var handle = await lockProvider.AcquireAsync(
    "outbox:dispatcher",
    new DistributedLockOptions
    {
        LeaseTime = TimeSpan.FromSeconds(30),
        WaitTime = TimeSpan.FromSeconds(5),
        AutoRenew = true,
        FailureMode = CoordinationFailureMode.FailClosed
    },
    cancellationToken);

if (!handle.Acquired)
    return;
```
- 正常竞争失败以 `Acquired = false` 表达，不用异常控制业务流程
- Redis 故障、租约丢失和参数错误使用不同异常或状态，调用方能明确处理
- `DisposeAsync` 必须幂等；租约丢失后不得释放其他持有者重新获取的锁

**版本切片**
- 路线图里程碑使用 `v1.5.6-preview.1` / `preview.2` / `rc.1` / 正式版标签
- NuGet 继续使用产品线日期版本，例如 `1.5.2026.801-preview.1`，正式包为发布当天的 `1.5.yyyy.Mdd`
- `preview.1`：发布门禁 + API + InMemory 实现
- `preview.2`：Redis 锁 + Telemetry + 跨进程测试
- `rc.1`：Redis 限流 + Sample + 文档；进入 RC 后只接收阻断发布的问题修复

**明确延期**
- `EFDistributedLockProvider`：数据库方言与锁语义差异较大，后续以实验功能单独评估
- Redis 滑动窗口限流、RedLock 多节点算法：待固定窗口和单 Redis 租约锁稳定后演进
- `IDbContextRouter` / `[ReadReplica]`：独立规划为后续版本，单独解决事务一致性与“读己之写”问题

---

### v1.5.7 — 批量操作 + 安全合规（预计 2026-09）

> **主题：** 高吞吐数据操作与合规需求。

**批量数据操作**
- `IRepository<T,K>.BulkInsertAsync / BulkUpdateAsync / BulkDeleteAsync`
- 默认用 EF Core 10 原生 `ExecuteUpdateAsync` / `ExecuteDeleteAsync`；
  超过 1000 条自动切换到 `EFCore.BulkExtensions` 路径（按需引用）
- 进度回调：`onBatchCommitted: (int batchIdx, int count) => ...`

**字段级加密**
- `[Encrypted]` 标在 Entity 属性上 —— EF Core ValueConverter 自动加解密
- AES-GCM，密钥从 `IDataProtectionProvider`（ASP.NET Core 内置）派生
- 密钥轮换支持（旧密钥可读、新密钥写）

**响应级 PII 脱敏**
- `[Sensitive]` / `[Sensitive(MaskPattern = "***-****-{last4}")]` 标在 DTO 属性上
- ASP.NET Core OutputFormatter 响应序列化时按当前用户角色脱敏
- 与 Audit Log 联动：记录原始值与脱敏值，便于合规审计

**审计日志保留期**
- `AuditOptions.RetentionDays = 90`，每日自动清理过期记录
- 清理前可选导出到对象存储（`IAuditArchiver` 抽象，用户实现 S3 / OSS / Azure Blob）

**Secret Manager 集成**
- `ISecretProvider` 抽象 + `EnvSecretProvider` / `AzureKeyVaultSecretProvider` / `HashiCorpVaultSecretProvider`
- `IConfiguration` 扩展：`config.AddOctopusSecrets()` 把 `secret://path` 占位符替换为真实值

---

## v1.6–v2.0 ERP 基础平台路线

> **长期目标：** 为 `/Users/lobster/Test/test-cc-umc` 这类多系统 ERP 提供统一的应用框架、模块化运行时和微服务基础设施，同时保持现有 WebCore 用户可平滑升级。

### 总体架构

```text
ERP 业务模块（UMC / OA / PLM / CRM / WMS / MES / FIN / HRM / OMS）
                 │
     ┌───────────┴───────────┐
     │                       │
模块化单体宿主            独立微服务宿主
     │                       │
     └───────────┬───────────┘
                 │
       OctopusEx ERP 基础平台
                 │
 WebCore · Identity · Policy · Audit · Events · Outbox · Telemetry
```

**兼容策略**
- v1.6–v1.9 保留现有 `OctopusEx.WebCore` API，新增能力优先通过扩展包提供
- 旧 API 先标记 `[Obsolete]`，提供迁移诊断和替代 API，不在 v1.x 直接删除
- v2.0 固化包边界、删除已废弃 API，并提供现有 ERP 系统迁移指南

### v1.6 — ERP 应用框架（预计 2026-10～12）

> **目标：** 让 UMC、OA、CRM 等系统共享一致的基础应用能力。

**Phase 1：基础上下文统一**
- `ICurrentUser`、`ICurrentTenant`、`ICurrentOrganization`、`ICorrelationContext`
- 统一租户、公司、部门、职位和数据权限上下文
- 统一 `BaseResponse`、分页、错误码、ProblemDetails 和 TraceId

**Phase 2：权限与策略**
- `IPermissionChecker`、`IDataScopeEvaluator`、`IAuthorizationPolicyProvider`
- 菜单/按钮权限、组织范围、本人/部门/公司/全部数据范围
- 与 UMC RBAC/组织关系映射，支持缓存和权限版本失效

**Phase 3：应用服务基座**
- `ApplicationServiceBase`、请求验证、事务边界、幂等入口
- CRUD、批量操作、导入导出、操作日志和审计统一管道
- 统一服务注册与启动诊断，消除各 ERP 项目重复的异常中间件和日志过滤器

**Phase 4：ERP 样板接入**
- UMC：身份/组织/权限接入样板
- CRM：客户→询盘→报价→合同业务流样板
- 提供从现有项目逐模块迁移的适配层

**v1.6 验收**
- UMC 和 CRM 至少各迁移一个完整业务链路
- 关键 API 集成测试覆盖认证、数据权限、审计、幂等和错误响应
- 不改变现有 WebCore 使用者的编译结果

### v1.7 — 模块化运行时（预计 2027-01～03）

> **目标：** 同一业务模块可部署在模块化单体，也可独立拆成微服务。

**Phase 1：模块契约**
- `IOctopusModule`、`IModuleManifest`、模块版本和依赖声明
- 依赖拓扑排序、循环依赖检测、模块启停状态
- 模块独立配置、服务、Endpoint、HealthCheck、Telemetry 注册

**Phase 2：模块宿主**
- `AddOctopusModules()` 和模块生命周期
- 模块间只通过公共契约、领域事件和集成事件通信
- 模块数据库迁移、Seed 和权限资源按模块隔离

**Phase 3：WebCore 拆包**
- `OctopusEx.WebCore.Caching`
- `OctopusEx.WebCore.Events`
- `OctopusEx.WebCore.MultiTenancy`
- `OctopusEx.WebCore.Diagnostics`
- `OctopusEx.WebCore.Hangfire`
- 原聚合包继续作为兼容入口

**Phase 4：模块化 ERP 样板**
- 把 CRM、OA 审批、PLM 商品目录做成可装配模块
- 同一套模块分别以单体宿主和独立 API 宿主运行

**v1.7 验收**
- 新建模块不修改宿主核心代码即可注册
- 模块依赖错误在启动阶段可读地报告
- 聚合包与拆分包的功能和 API 行为有兼容测试

### v1.8 — 微服务基础设施 SDK（预计 2027-04～06）

> **目标：** 统一 ERP 服务间通信、可靠投递和故障处理。

**Phase 1：服务间调用**
- `IOctopusHttpClient`、服务发现、超时、重试、熔断和 Trace 传播
- HMAC Webhook 客户端/服务端统一封装
- 标准化签名、时间戳、Nonce、防重放和错误响应

**Phase 2：可靠消息链路**
- Outbox/Inbox 统一模型
- 事件幂等、重试、死信、补偿和消费进度
- HTTP Webhook、Redis EventBus 和未来消息队列使用同一事件契约

**Phase 3：跨服务一致性**
- Saga/Process Manager 基础抽象
- 业务补偿动作、状态机、人工介入点和操作审计
- 不在本版承诺分布式事务或强一致性跨库提交

**Phase 4：云原生接入**
- Aspire 服务注册、配置、健康检查、Telemetry 和诊断统一接线
- 服务依赖图和跨服务 CorrelationId 查询

**v1.8 验收**
- CRM→OA、OA→CRM、MES→WMS 至少各有一条可靠集成链路
- 重复投递、超时、服务不可用和签名错误均有自动化测试
- 故障后可从诊断端点定位事件、重试和补偿状态

### v1.9 — ERP 开发平台与模板（预计 2027-07～09）

> **目标：** 把成熟模式变成可复制的开发工具。

**Phase 1：项目模板**
- `dotnet new octopus-erp-api`
- `dotnet new octopus-module`
- `dotnet new octopus-plugin`
- 预置认证、权限、审计、健康检查、OpenAPI、测试项目和 CI

**Phase 2：通用业务组件**
- 编码规则和单据号生成
- 附件/对象存储抽象
- 导入导出、字典、参数配置、通知和站内消息
- 多语言、时区、金额/税率和工作日历基础抽象

**Phase 3：质量门禁**
- 模块契约测试、架构依赖测试、数据库迁移测试
- NuGet 本地源安装测试和包内 README/依赖校验
- ERP Sample 全链路演示：询盘→报价→审批→合同→库存/生产→回款

### v2.0 — 稳定平台（预计 2027-10～12）

**Phase 1：API 冻结**
- 公共 API 审核、包边界冻结、版本兼容矩阵
- 删除 v1.x 已标记且有迁移替代的 API

**Phase 2：ERP 迁移**
- UMC、OA、CRM、PLM、WMS、MES、FIN、HRM、OMS 分批迁移
- 每个系统提供迁移前后行为对照和回滚方案

**Phase 3：生产门禁**
- 全链路集成测试、性能基线、安全扫描、包安装验证
- v2.0 发布后只接受兼容性和安全修复

### 长期不纳入核心的能力

- 低代码页面设计器和完整工作流 SaaS
- 强绑定某一家云厂商的基础设施实现
- 跨库分布式事务和全量 Event Sourcing
- 与具体 ERP 业务绑定的财务、生产、仓储规则

这些能力应通过 ERP 业务模块或插件实现，避免基础库反向吞并业务系统。

---

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

---

## 参与贡献

- 功能建议：提交 [Issue](https://github.com/Fat-Snail/OctopusUtils/issues) 并打 `enhancement` 标签
- 认领任务：查看 [Projects](https://github.com/Fat-Snail/OctopusUtils/projects) 看板
- 代码贡献：Fork → 分支 → PR，提交信息遵循 [Conventional Commits](https://www.conventionalcommits.org/zh-hans/)

> 欢迎通过 Issue 讨论优先级，社区反馈将直接影响版本排期。
