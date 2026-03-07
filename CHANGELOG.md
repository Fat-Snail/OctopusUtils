# 更新日志

## [1.2.3] - 2026-03-07

### 更新
- 📝 完善自动依赖注入最佳实践说明
  - 📖 添加不推荐直接继承生命周期接口的说明
  - ✅ 推荐使用服务接口继承生命周期接口
  - 📝 添加完整的示例代码展示最佳实践
- 📝 新增 `EFQueryableExtensions.WhereIf()` 文档
  - 🆕 支持条件查询扩展方法（WhereIf）
  - ✅ 简化带条件的查询表达式编写
  - 📝 添加多种 WhereIf 重载方法说明
  - 📝 添加完整的查询示例代码

### 新增特性
- 🔧 **EFQueryableExtensions** - Entity Framework 查询扩展
  - 🆕 `WhereIf<T>(IQueryable, Expression, Boolean)` - 条件查询扩展
  - 🆕 `WhereIf<T>(IQueryable, Boolean, Expression)` - 条件在前版本
  - 🆕 `WhereIf<T>(IQueryable, Expression<Func<T,Int32,Boolean>>, Boolean)` - 带索引版本
  - 🆕 `WhereIf<T>(IEnumerable, Func<T,Boolean>, Boolean)` - IEnumerable 版本
  - 🆕 `WhereIf<T>(IEnumerable, Func<T,int,bool>, Boolean)` - IEnumerable 带索引版本

---

## [1.2.2] - 2026-02-12

### 新增
- ✨ **OctopusEx.WebCore** - 服务健康检测扩展
  - 🔍 **HealthCheckExtensions** - 全面的服务健康监控和检查端点
    - 🆕 `AddCommonHealthChecks()` - 添加通用健康检查（数据库、外部API、缓存）
    - 🆕 `AddDatabaseHealthCheck()` - 添加数据库健康检查（支持自定义连接字符串和数据库类型）
    - 🆕 `AddExternalApiHealthCheck()` - 添加外部 API 健康检查（支持超时配置）
    - 🆕 `AddCacheHealthCheck()` - 添加缓存健康检查（支持 Redis、Memory Cache 等）
    - 🆕 `AddBusinessLogicHealthCheck()` - 添加自定义业务逻辑健康检查
    - 🆕 `MapHealthCheckEndpoints()` - 映射所有健康检查端点
    - 🆕 `GetHealthCheckConfiguration()` - 获取健康检查配置
  - 🏥 **内置健康检查实现**
    - 🆕 `DatabaseHealthCheck` - 数据库连接性监控（支持连接字符串脱敏）
    - 🆕 `ExternalApiHealthCheck` - 外部服务/API 监控（支持响应时间统计）
    - 🆕 `CacheHealthCheck` - 缓存服务监控（支持命中率统计）
    - 🆕 `ICustomHealthCheck` - 自定义健康检查接口
  - 🌐 **健康检查端点**
    - 🆕 `GET /health/ready` - 就绪探针（检查所有标记为 "ready" 的检查）
    - 🆕 `GET /health/live` - 存活探针（检查所有标记为 "live" 的检查）
    - 🆕 `GET /health/full` - 完整健康检查（所有检查）
    - 🆕 `GET /health` - 详细健康状态和指标（包含每个检查的详细信息）

---

## [1.2.1] - 2026-02-07

### 新增
- ✨ **OctopusEx.WebCore** - ASP.NET Core Web 应用脚手架
  - 🗄️ **DomainCore** - 领域仓储层和 CRUD 脚手架
    - 🆕 泛型仓储接口和实现 `IRepository<TEntity, TKey>` 和 `Repository<TEntity, TKey>`
    - 🆕 工作单元模式 `IUnitOfWork` 和 `UnitOfWork`
    - 🆕 CRUD 服务基类 `CrudServiceBase<TEntity, TKey, TDto>`
    - 🆕 CRUD 控制器基类 `CURDControllerBase<TEntity, TKey, TDto>`
    - 🆕 支持批量操作、复杂查询、事务处理
    - 🆕 支持联表查询、查询构建器模式
    - 🆕 完整的验证和异常处理机制
    - 🆕 灵活的扩展点和自定义能力

## [1.2.0] - 2026-02-07

### 新增
- ✨ **OctopusEx.WebCore** - ASP.NET Core Web 应用脚手架
  - 🎯 **ApiUIExtensions** - Swagger UI 和 Scalar UI 集成扩展
  - 🔍 **AspireExtensions** - .NET Aspire 链路追踪简化配置
  - 📊 **AuditServiceExtensions** - 基于领域的数据库审计系统
  - ⚡ **HangfireExtensions** - 后台作业调度扩展（支持单任务执行）
    - 🆕 支持 appsettings.json 配置 Dashboard 用户名密码
    - 🆕 内置 `HangfireAuthorizationFilter` 认证过滤器
  - 🗄️ **DomainCore** - 领域仓储层和 CRUD 脚手架
    - 🆕 泛型仓储接口和实现 `IRepository<TEntity, TKey>` 和 `Repository<TEntity, TKey>`
    - 🆕 工作单元模式 `IUnitOfWork` 和 `UnitOfWork`
    - 🆕 CRUD 服务基类 `CrudServiceBase<TEntity, TKey, TDto>`
    - 🆕 CRUD 控制器基类 `CURDControllerBase<TEntity, TKey, TDto>`
    - 🆕 支持批量操作、复杂查询、事务处理
    - 🆕 支持联表查询、查询构建器模式
    - 🆕 完整的验证和异常处理机制
    - 🆕 灵活的扩展点和自定义能力
  - 🛡️ **SensitiveWordFilterPlugin** - 智能敏感词过滤插件
    - ⚡ 基于 ToolGood.Words 快速匹配
    - 🧠 结合 Semantic Kernel AI 智能识别
    - 🎯 支持三种检测模式：快速检测、AI 识别、综合检测
    - 📚 提供详细的检测结果和置信度评分
  - 🔧 **HostBuilderExtensions** - 自动依赖注入脚手架
  - 📝 **完整示例项目** - 包含前后端代码示例

### 工具和配置
- ✨ 更新 **.editorconfig** - 支持 .NET 10 最新语法糖
- ✨ 配置 **Husky.NET** - 提交时自动格式化代码
- ✨ 支持 **.NET 10.0** - 升级 SDK 版本到 10.0.100

### 新增特性详情
- **ApiUIExtensions** - 支持 Swagger 和 Scalar UI 灵活切换
- **AspireExtensions** - 简化 OpenTelemetry 配置
- **AuditServiceExtensions** - 基于模型的可配置审计系统
- **HangfireExtensions** - 简化后台作业配置，支持单任务执行
  - `AddSimpleHangfire()` - 简化 Hangfire 配置
  - `AddRecurringJob()` - 添加定时作业
  - `AddBackgroundJob()` - 添加一次性作业
  - `UseHangfireDashboard()` - 配置 Dashboard 认证
- **DomainCore** - 领域仓储层和 CRUD 脚手架
  - `AddGenericRepositoryEfCoreInMemory()` - 注册泛型仓储服务
  - `IRepository<TEntity, TKey>` - 提供标准的 CRUD 方法
  - `IUnitOfWork` - 管理多个仓储的事务
  - `CrudServiceBase<TEntity, TKey, TDto>` - 服务层基类
  - `CURDControllerBase<TEntity, TKey, TDto>` - 控制器基类
  - `EFQueryableExtensions.WhereIf()` - 条件查询扩展方法
  - 支持批量操作：`AddRangeAsync`, `UpdateRangeAsync`, `DeleteRangeAsync`
  - 支持复杂查询：`FindAllAsync` 多重载，查询构建器
  - 支持联表查询：Include, Join, 复杂聚合
  - 支持事务处理：`ExecuteTransactionAsync`
  - 灵活的验证机制：`ValidateCreateRequestAsync`, `ValidateUpdateRequestAsync`, `CanDeleteAsync`
- **SensitiveWordFilterPlugin** - 多层次敏感词检测
  - `DetectSensitiveWords()` - ToolGood.Words 快速检测
  - `DetectSensitiveWordsWithAI()` - AI 智能识别
  - `ComprehensiveDetectSensitiveWords()` - 综合检测
  - `SetSensitiveWords()` - 批量配置敏感词库
  - `AddSensitiveWord()` - 添加单个敏感词
- **自动依赖注入** - 基于接口的智能服务注册
  - `IScopeDependency` - 作用域生命周期接口
  - `ISingletonDependency` - 单例生命周期接口
  - `ITransientDependency` - 瞬态生命周期接口
  - 推荐使用服务接口继承生命周期接口

### 更新 (2026-01-17)
- 📝 更新 **OctopusEx.WebCore/README.md** - 新增领域仓储层文档
- 📝 添加 DomainCore 详细使用示例（10个完整示例）
- 📝 新增敏感词过滤插件文档
- 📝 新增敏感词过滤使用示例和检测方法对比
- 📝 完善 Hangfire Dashboard 认证配置说明
- 📝 添加 appsettings.json 配置示例

---

## [1.1.0] - 2024-11-19

### 新增
- ✨ 添加单元测试工具类 UnitTest
- ✨ 新增性能分析工具 MiniProfiler 支持
- ✨ 添加测试断言工具 Assert
- ✨ 新增测试报告格式化功能

### 优化
- 🔧 将所有基本类型升级为完整的 .NET 类型名称
- 🔧 string → String
- 🔧 int → Int32
- 🔧 bool → Boolean
- 🔧 long → Int64
- 🔧 float → Single
- 🔧 double → Double
- 🔧 object → Object
- 🔧 char → Char

### 修复
- 🐛 修复 Rider IDE 临时文件被 Git 跟踪的问题
- 🐛 更新 .gitignore 忽略 .idea/ 目录
- 🐛 修复 .NET SDK 版本兼容性问题

---

## [1.0.0] - 2024-11-18

### 新增
- ✨ 聚合全文索引工具类（基于 Lucene.NET 4.8.0）
- ✨ Tag分类提取功能
- ✨ 中文分词功能（基于结巴分词）
- ✨ 谷歌云盘下载工具类
- ✨ 简单控制台进度条
- ✨ AI客户端（支持OpenAI、Llama）
- ✨ 支持对话交互和批量操作功能

---

> **版本规则**：遵循 [语义化版本 2.0.0](https://semver.org/lang/zh-CN/)
> **更新日志**：遵循 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)
