# OctopusEx.WebCore

一个功能丰富的 ASP.NET Core Web 应用程序脚手架，提供了一系列现代化的扩展功能，简化企业级应用的开发流程。

## 🚀 特性概述

### 1. API UI 扩展 (ApiUIExtensions)
**支持 Swagger UI 和 Scalar UI 的灵活配置**

```csharp
// 配置服务
builder.Services.AddSwaggerUIServices();

// 配置开发环境下的 OpenAPI UI
if (app.Environment.IsDevelopment())
{
    // 使用 SwaggerUI 和 ScalarUI
    app.UseBothApiUIs();
    
    // 或者单独使用
    // app.UseSwaggerUI();
    // app.UseScalarUI();
}
```

### 2. .NET Aspire 扩展 (AspireExtensions)
**简化分布式应用的可观测性配置**

```csharp
// 快速配置 OpenTelemetry 链路追踪
builder.AddAspireOpenTelemetry();
```

### 3. 数据库审计扩展 (AuditServiceExtensions)
**基于领域的细粒度审计配置**

```csharp
// 添加审计服务并配置
builder.Services.AddAuditing(config =>
{
    config.Enabled = true;
    
    // 配置产品领域 - 使用 Lambda 表达式
    config.ConfigureDomain<Product>(cfg =>
    {
        cfg.Enabled = true;
        cfg.Ignore(p => p.CreatedAt);
        cfg.Ignore(p => p.InternalCode);
    });
    
    // 配置订单领域 - 使用字符串
    config.ConfigureDomain<Order>(cfg =>
    {
        cfg.Enabled = true;
        cfg.Ignore("InternalOrderCode", "DiscountAmount");
    });
    
    // 禁用特定领域
    config.ConfigureDomain<SystemLog>(cfg => cfg.Enabled = false);
});

// 配置 DbContext 并启用审计拦截器
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    options.UseSqlite("Data Source=auditing.db")
           .UseAuditing(serviceProvider));
```

**完整示例项目**: [auditing-demo.zip](https://github.com/Fat-Snail/X-Net-Mod/blob/main/auditing-demo.zip)

### 4. Hangfire 扩展 (HangfireExtensions)
**简化后台作业配置，支持单任务执行和 Dashboard 认证**

```csharp
// 配置 Hangfire（简化扩展方法）
builder.Services.AddSimpleHangfire(workerCount: 1);

// 初始化作业
using var scope = app.Services.CreateScope();
var serviceProvider = scope.ServiceProvider;
var jobExecutionService = serviceProvider.GetRequiredService<IJobExecutionService>();

// 添加定时作业
serviceProvider.AddRecurringJob(
    "api-cleanup-temp-data",
    () => jobExecutionService.ExecuteApiServiceCleanupAsync(),
    "0 * * * *"); // 每小时执行一次

serviceProvider.AddRecurringJob(
    "api-generate-daily-report",
    () => jobExecutionService.ExecuteApiServiceGenerateReportAsync(),
    "0 0 * * *"); // 每天午夜执行

// 添加一次性作业
serviceProvider.AddBackgroundJob("startup-notification",
    () => Console.WriteLine("Hangfire 作业系统已启动"));

// 配置 Hangfire Dashboard（带认证）
app.UseHangfireDashboard("/hangfire",
    new DashboardOptions
    {
        DashboardTitle = "独立作业系统控制台",
        StatsPollingInterval = 10000,
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });
```

**配置 Dashboard 认证（appsettings.json）**

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Aspire.Hosting.Dcp": "Warning"
    }
  },
  "HangfireDashboard": {
    "Username": "jobadmin1",
    "Password": "jobadmin1"
  }
}
```

**说明：**
- `HangfireDashboard.Username` - Dashboard 登录用户名
- `HangfireDashboard.Password` - Dashboard 登录密码
- `HangfireAuthorizationFilter` - 内置认证过滤器，自动从配置读取凭据

### 5. 敏感词过滤插件 (SensitiveWordFilterPlugin)
**基于 Semantic Kernel 和 AI 的智能敏感词检测**

结合 ToolGood.Words 的快速匹配和 AI 的智能识别，提供多层次的敏感词过滤方案。

```csharp
// 配置 Kernel 和插件
var builder = Kernel.CreateBuilder();

// 添加日志服务
builder.Services.AddLogging(c => c.AddConsole().SetMinimumLevel(LogLevel.Warning));

// 配置连接到本地 Ollama（用于 AI 识别）
builder.AddOpenAIChatCompletion(
    modelId: "llama3.2:3b",
    apiKey: "ollama",
    endpoint: new Uri("http://localhost:11434/v1")
);

// 添加敏感词过滤插件
builder.Plugins.AddFromType<SensitiveWordFilterPlugin>("SensitiveFilter");

var kernel = builder.Build();

// 更新整个敏感词库
var setResult = await kernel.InvokeAsync<string>("SensitiveFilter", "SetSensitiveWords", new()
{
    ["sensitiveWordsJson"] = "[\"AK48\", \"M16\", \"手枪\", \"步枪\", \"爆炸物\", \"危险品\", \"毒品\", \"大麻\"]"
});

// 进行敏感词检测
var inputText = "这是一段包含敏感信息的文本";

// 方式1: 仅使用 ToolGood.Words 快速检测
var toolGoodResult = await kernel.InvokeAsync<string>("SensitiveFilter", "DetectSensitiveWords", new()
{
    ["input"] = inputText
});

// 方式2: 仅使用 AI 智能识别
var aiResult = await kernel.InvokeAsync<string>("SensitiveFilter", "DetectSensitiveWordsWithAI", new()
{
    ["input"] = inputText
});

// 方式3: 综合检测（ToolGood.Words + AI）
var combinedResult = await kernel.InvokeAsync<string>("SensitiveFilter", "ComprehensiveDetectSensitiveWords", new()
{
    ["input"] = inputText
});

// 添加单个敏感词到词库
var addResult = await kernel.InvokeAsync<string>("SensitiveFilter", "AddSensitiveWord", new()
{
    ["word"] = "新的敏感词"
});
```

**检测方法对比：**

| 检测方法 | 特点 | 优势 | 适用场景 |
|---------|------|------|---------|
| **ToolGood.Words** | 基于关键词匹配 | ⚡ 极快速度 | 大批量文本过滤 |
| **AI 识别** | 基于语义理解 | 🧠 智能识别 | 复杂语境判断 |
| **综合检测** | 结合两者优势 | 🎯 高精度 + 高速度 | 生产环境推荐 |

**返回结果模型：**

```csharp
// ToolGood.Words 检测结果
public class SensitiveWordDetectionResult
{
    public string OriginalText { get; set; }
    public bool HasSensitiveWords { get; set; }
    public List<string> SensitiveWords { get; set; }
    public string DetectionMethod { get; set; } // "ToolGood.Words"
}

// AI 识别结果
public class AITextAnalysisResult
{
    public string OriginalText { get; set; }
    public bool HasSensitiveWords { get; set; }
    public List<string> SensitiveWords { get; set; }
    public List<string> SensitiveTypes { get; set; }
    public double Confidence { get; set; }
    public string DetectionMethod { get; set; } // "AI"
    public string ErrorMessage { get; set; }
}

// 综合检测结果
public class CombinedDetectionResult
{
    public string OriginalText { get; set; }
    public bool FinalHasSensitiveWords { get; set; }
    public List<string> FinalSensitiveWords { get; set; }
    public List<string> FinalSensitiveTypes { get; set; }
    public double CombinedConfidence { get; set; }
    public string DetectionMethod { get; set; } // "Combined"
}
```

### 6. 自动依赖注入
**基于接口的智能依赖注入系统**

```csharp
// 启用自动注入
builder.AsBuild().AddUtil();

var app = builder.Build();

// 服务实现示例
public interface IJobExecutionService : Util.Dependency.IScopeDependency
{
    Task ExecuteApiServiceCleanupAsync();
    Task ExecuteApiServiceGenerateReportAsync();
    Task ExecuteApiServiceSyncDataAsync();
    Task ExecuteApiServiceSendNotificationsAsync();
}

public class JobExecutionService : IJobExecutionService
{
    // 实现方法...
}
```

## 📦 安装

### 通过 NuGet 安装
```bash
dotnet add package OctopusEx.WebCore
```

### 项目引用
```xml
<PackageReference Include="OctopusEx.WebCore" Version="1.0.2025.1225" />
```

**额外依赖（如使用敏感词过滤功能）:**
```xml
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.0" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.0.0" />
<PackageReference Include="ToolGood.Words" Version="3.1.0" />
```

## 🔧 快速开始

### 1. 创建新的 ASP.NET Core 项目
```bash
dotnet new webapi -n MyAwesomeApi
cd MyAwesomeApi
```

### 2. 配置 Program.cs
```csharp
using OctopusEx.WebCore.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 启用所有扩展功能
builder.AsBuild().AddUtil();
builder.Services.AddSwaggerUIServices();
builder.AddAspireOpenTelemetry();
builder.Services.AddSimpleHangfire();

var app = builder.Build();

// 配置 Hangfire Dashboard（带认证）
app.UseHangfireDashboard("/hangfire",
    new DashboardOptions
    {
        DashboardTitle = "独立作业系统控制台",
        StatsPollingInterval = 10000,
        Authorization = new[] { new HangfireAuthorizationFilter() }
    });

if (app.Environment.IsDevelopment())
{
    app.UseBothApiUIs();
}

app.Run();
```

### 3. 配置 appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "HangfireDashboard": {
    "Username": "jobadmin1",
    "Password": "jobadmin1"
  }
}
```

### 4. 配置审计（可选）
```csharp
// 在 Program.cs 中添加
builder.Services.AddAuditing(config =>
{
    config.Enabled = true;
    // 配置领域...
});

// 配置 DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db")
           .UseAuditing(builder.Services.BuildServiceProvider()));
```

## 🎯 核心扩展类

### ApiUIExtensions
- `AddSwaggerUIServices()` - 配置 Swagger 和 Scalar 服务
- `UseBothApiUIs()` - 同时启用 Swagger 和 Scalar UI
- `UseSwaggerUI()` - 仅启用 Swagger UI
- `UseScalarUI()` - 仅启用 Scalar UI

### AspireExtensions  
- `AddAspireOpenTelemetry()` - 配置 OpenTelemetry 链路追踪

### AuditServiceExtensions
- `AddAuditing()` - 配置领域审计
- `ConfigureDomain<T>()` - 配置特定领域的审计规则
- `UseAuditing()` - 启用审计拦截器

### HangfireExtensions
- `AddSimpleHangfire()` - 简化 Hangfire 配置
- `AddRecurringJob()` - 添加定时作业
- `AddBackgroundJob()` - 添加一次性作业
- `UseHangfireDashboard()` - 配置 Dashboard 认证（支持 appsettings.json 配置）

### SensitiveWordFilterPlugin
- `DetectSensitiveWords()` - 使用 ToolGood.Words 快速检测敏感词
- `DetectSensitiveWordsWithAI()` - 使用 AI 智能识别敏感内容
- `ComprehensiveDetectSensitiveWords()` - 综合检测（ToolGood.Words + AI）
- `SetSensitiveWords()` - 批量设置敏感词库
- `AddSensitiveWord()` - 添加单个敏感词到词库

### HostBuilderExtensions
- `AsBuild().AddUtil()` - 启用自动依赖注入

## 🔄 依赖注入生命周期

支持三种生命周期：
- `IScopeDependency` - 作用域生命周期
- `ISingletonDependency` - 单例生命周期  
- `ITransientDependency` - 瞬态生命周期

## 📚 示例项目

完整的示例项目包含：
- 数据库审计实现
- Hangfire 作业调度
- OpenAPI 文档
- 自动依赖注入
- 链路追踪配置
- 敏感词过滤演示

下载地址: [auditing-demo.zip](https://github.com/Fat-Snail/X-Net-Mod/blob/main/auditing-demo.zip)

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

MIT License - 详见 [LICENSE](../LICENSE) 文件

## 📞 支持

- 项目主页: [GitHub Repository](https://github.com/Fat-Snail/OctopusUtils)
- 问题反馈: [Issues](https://github.com/Fat-Snail/OctopusUtils/issues)
- 文档: [Wiki](https://github.com/Fat-Snail/OctopusUtils/wiki)

---

**OctopusEx.WebCore** - 让 ASP.NET Core 开发更简单、更高效！ 🚀