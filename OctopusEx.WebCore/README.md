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
**简化后台作业配置，支持单任务执行**

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
```

### 5. 自动依赖注入
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
<PackageReference Include="OctopusEx.WebCore" Version="1.0.0" />
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

if (app.Environment.IsDevelopment())
{
    app.UseBothApiUIs();
}

app.Run();
```

### 3. 配置审计（可选）
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