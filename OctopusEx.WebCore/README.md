# OctopusEx.WebCore

一个功能丰富的 ASP.NET Core Web 应用程序脚手架，提供了一系列现代化的扩展功能，简化企业级应用的开发流程。

## 🚀 特性概述

### 1. 服务健康检测扩展 (HealthCheckExtensions)
**全面的服务健康监控和检查端点**

```csharp
// 添加通用健康检查
builder.AddCommonHealthChecks();

// 添加数据库健康检查
builder.AddDatabaseHealthCheck(
    name: "my-database",
    connectionString: "Server=myserver;Database=mydb;User Id=user;Password=pass;",
    databaseType: "PostgreSQL");

// 添加外部 API 健康检查
builder.AddExternalApiHealthCheck(
    name: "payment-service",
    apiEndpoint: "https://paymentservice.mycompany.com/health");

// 添加缓存健康检查
builder.AddCacheHealthCheck(
    name: "redis-cache",
    cacheType: "Redis",
    connectionString: "redis.mycompany.com:6379");

// 添加自定义业务逻辑健康检查
builder.AddBusinessLogicHealthCheck("order-processing", async (cancellationToken) =>
{
    // 自定义业务逻辑验证
    await Task.Delay(100, cancellationToken);
    return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("Order processing is healthy");
});

// 映射所有健康检查端点
app.MapHealthCheckEndpoints();
```

**可用的健康检查端点：**
- `GET /health/ready` - 就绪探针（检查所有标记为 "ready" 的检查）
- `GET /health/live` - 存活探针（检查所有标记为 "live" 的检查）
- `GET /health/full` - 完整健康检查（所有检查）
- `GET /health` - 详细健康状态和指标

**内置健康检查类型：**
- `DatabaseHealthCheck` - 数据库连接性监控
- `ExternalApiHealthCheck` - 外部服务/API 监控
- `CacheHealthCheck` - 缓存服务监控
- 自定义健康检查 - 支持自定义业务逻辑验证

### 2. API UI 扩展 (ApiUIExtensions)
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

运行后可访问/swagger或/scalar浏览接口文档

### 3. .NET Aspire 扩展 (AspireExtensions)
**简化分布式应用的可观测性配置**

```csharp
// 快速配置 OpenTelemetry 链路追踪
builder.AddAspireOpenTelemetry();
```

### 4. 数据库审计扩展 (AuditServiceExtensions)
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

### 5. Hangfire 扩展 (HangfireExtensions)
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

### 6. 敏感词过滤插件 (SensitiveWordFilterPlugin)
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

### 7. 领域仓储层 (DomainCore)
**泛型仓储和 CRUD 服务基类，快速搭建通用功能**

提供完整的仓储模式实现，包含泛型仓储、工作单元、CRUD 服务和控制器基类。

#### 核心组件

**DomainCore 包含：**
- `IRepository<TEntity, TKey>` - 泛型仓储接口
- `Repository<TEntity, TKey>` - 泛型仓储实现
- `IUnitOfWork` - 工作单元接口
- `UnitOfWork` - 工作单元实现
- `CrudServiceBase<TEntity, TKey, TDto>` - CRUD 服务基类
- `CURDControllerBase<TEntity, TKey, TDto>` - CRUD 控制器基类

#### 1. 配置泛型仓储

```csharp
// 配置服务和数据库
var services = new ServiceCollection();

// 注册内存数据库的泛型仓储服务
services.AddGenericRepositoryEfCoreInMemory();

// 注册具体业务服务
services.AddScoped<ProductService>();

// 构建服务提供程序
var serviceProvider = services.BuildServiceProvider();
```

#### 2. 使用仓储和工作单元

```csharp
using var scope = serviceProvider.CreateScope();
var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();

// 获取产品仓储
var productRepo = unitOfWork.GetRepository<Product, int>();

// 添加新产品
var newProduct = new Product
{
    Name = "示例产品",
    Description = "这是一个示例产品",
    Price = 99.99m,
    StockQuantity = 10,
    IsActive = true,
    CategoryId = 1
};

await productRepo.AddAsync(newProduct);
await unitOfWork.SaveChangesAsync();
Console.WriteLine($"✅ 产品添加成功，ID: {newProduct.Id}");

// 查询产品
var product = await productRepo.GetByIdAsync(newProduct.Id);
Console.WriteLine($"✅ 查询产品: {product?.Name}");

// 更新产品
if (product != null)
{
    product.Price = 109.99m;
    await productRepo.UpdateAsync(product);
    await unitOfWork.SaveChangesAsync();
    Console.WriteLine("✅ 产品更新成功");
}

// 删除产品
await productRepo.DeleteByIdAsync(newProduct.Id);
await unitOfWork.SaveChangesAsync();
Console.WriteLine("✅ 产品删除成功");
```

#### 3. 批量操作

```csharp
var productRepo = unitOfWork.GetRepository<Product, int>();

// 批量添加
var newProducts = new List<Product>
{
    new Product { Name = "批量产品A", Price = 100, StockQuantity = 10, IsActive = true, CategoryId = 1 },
    new Product { Name = "批量产品B", Price = 200, StockQuantity = 20, IsActive = true, CategoryId = 2 },
    new Product { Name = "批量产品C", Price = 300, StockQuantity = 30, IsActive = true, CategoryId = 3 }
};

await productRepo.AddRangeAsync(newProducts);
await unitOfWork.SaveChangesAsync();

// 批量查询
var productIds = newProducts.Select(p => p.Id).ToList();
var fetchedProducts = await productRepo.GetByIdsAsync(productIds);

// 批量更新
foreach (var product in fetchedProducts)
{
    product.Price *= 1.5m;
}
await productRepo.UpdateRangeAsync(fetchedProducts);
await unitOfWork.SaveChangesAsync();

// 批量删除
await productRepo.DeleteRangeAsync(fetchedProducts);
await unitOfWork.SaveChangesAsync();
```

#### 4. 查询构建器和 FindAllAsync

```csharp
var productRepo = unitOfWork.GetRepository<Product, int>();

// 方法1：仅条件查询
var products1 = await productRepo.FindAllAsync(
    condition: p => p.IsActive && p.Price > 1000,
    cancellationToken: default);

// 方法2：条件查询 + 排序
var products2 = await productRepo.FindAllAsync(
    condition: p => p.IsActive,
    orderBy: q => q.OrderByDescending(p => p.Price),
    cancellationToken: default);

// 方法3：条件查询 + 排序 + 关联加载
var products3 = await productRepo.FindAllAsync(
    condition: p => p.IsActive,
    orderBy: q => q.OrderBy(p => p.Name),
    includes: p => p.Category);

// 方法4：查询构建器模式
var products4 = await productRepo.FindAllAsync(
    queryBuilder: builder =>
    {
        return builder
            .Where(p => p.IsActive)
            .Where(p => p.Price > 1000)
            .OrderBy(p => p.Price)
            .Include(p => p.Category)
            .Take(5)
            .AsNoTracking();
    });
```

#### 5. 联表查询

```csharp
var productRepo = unitOfWork.GetRepository<Product, int>();

// 方法1：使用 Include 直接加载关联实体
var productsWithCategory = await productRepo.FindAllAsync(
    condition: p => p.IsActive,
    includes: p => p.Category);

// 方法2：使用 LINQ Join 进行显式连接查询
var query = from p in dbContext.Set<Product>()
            join c in dbContext.Set<Category>() on p.CategoryId equals c.Id
            where p.IsActive && c.IsActive
            select new
            {
                ProductName = p.Name,
                ProductPrice = p.Price,
                CategoryName = c.Name,
                CategoryDescription = c.Description
            };

var joinResults = await query.Take(3).ToListAsync();
```

#### 6. 复杂查询和聚合

```csharp
// 按分类统计产品数量、平均价格、总库存
var statsQuery = from p in dbContext.Set<Product>()
                 join c in dbContext.Set<Category>() on p.CategoryId equals c.Id
                 where p.IsActive && c.IsActive
                 group p by new { c.Id, c.Name } into g
                 select new
                 {
                     CategoryName = g.Key.Name,
                     ProductCount = g.Count(),
                     AvgPrice = g.Average(p => p.Price),
                     TotalStock = g.Sum(p => p.StockQuantity),
                     MaxPrice = g.Max(p => p.Price),
                     MinPrice = g.Min(p => p.Price)
                 };

var stats = await statsQuery.ToListAsync();
```

#### 7. 事务操作

```csharp
await unitOfWork.ExecuteTransactionAsync(async () =>
{
    // 操作1：添加新分类
    var newCategory = new Category
    {
        Name = "新分类",
        Description = "事务中添加的分类",
        IsActive = true
    };
    await categoryRepo.AddAsync(newCategory);
    await unitOfWork.SaveChangesAsync();

    // 操作2：添加新产品
    var newProduct = new Product
    {
        Name = "事务产品",
        Description = "在事务中添加的产品",
        Price = 199.99m,
        StockQuantity = 50,
        IsActive = true,
        CategoryId = newCategory.Id
    };
    await productRepo.AddAsync(newProduct);
    await unitOfWork.SaveChangesAsync();

    // 操作3：更新现有产品价格
    var existingProduct = await productRepo.GetByIdAsync(1);
    if (existingProduct != null)
    {
        existingProduct.Price *= 1.1m;
        await productRepo.UpdateAsync(existingProduct);
        await unitOfWork.SaveChangesAsync();
    }

    Console.WriteLine("✅ 事务操作成功完成！");
});
```

#### 8. 使用 CrudServiceBase

创建自定义服务类继承 `CrudServiceBase<TEntity, TKey, TDto>`：

```csharp
public class ProductService : CrudServiceBase<Product, int, ProductDto>
{
    public ProductService(IUnitOfWork unitOfWork, IMapper mapper)
        : base(unitOfWork, mapper)
    {
    }

    // 可以添加自定义业务方法
    public async Task<List<ProductDto>> SearchProductsAsync(
        string keyword,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var repository = UnitOfWork.GetRepository<Product, int>();

        var products = await repository.FindAllAsync(
            condition: p =>
                (string.IsNullOrEmpty(keyword) || p.Name.Contains(keyword)) &&
                (!minPrice.HasValue || p.Price >= minPrice.Value) &&
                (!maxPrice.HasValue || p.Price <= maxPrice.Value) &&
                (!categoryId.HasValue || p.CategoryId == categoryId.Value),
            cancellationToken: cancellationToken);

        return Mapper.Map<List<ProductDto>>(products);
    }

    // 获取统计信息
    public async Task<ProductStatisticsDto> GetProductStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var repository = UnitOfWork.GetRepository<Product, int>();

        var allProducts = await repository.FindAllAsync(
            condition: p => p.IsActive,
            cancellationToken: cancellationToken);

        return new ProductStatisticsDto
        {
            TotalCount = allProducts.Count,
            ActiveCount = allProducts.Count(p => p.IsActive),
            HighPriceCount = allProducts.Count(p => p.Price > 1000)
        };
    }
}
```

#### 9. 使用 CURDControllerBase

创建控制器继承 `CURDControllerBase<TEntity, TKey, TDto>`：

```csharp
/// <summary>
/// 产品控制器（继承自通用CRUD控制器基类）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : CURDControllerBase<Product, int, ProductDto>
{
    public ProductsController(ProductService productService)
        : base(productService)
    {
    }

    /// <summary>
    /// 从DTO中获取实体主键值
    /// </summary>
    protected override int GetEntityIdFromDto(ProductDto dto)
    {
        return dto.Id ?? throw new ArgumentException("Product Id is required");
    }

    /// <summary>
    /// 验证创建请求
    /// </summary>
    protected override async Task<ValidationResult> ValidateCreateRequestAsync(
        ProductDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return ValidationResult.Fail("产品名称不能为空");
        }

        if (request.Price <= 0)
        {
            return ValidationResult.Fail("产品价格必须大于0");
        }

        return await base.ValidateCreateRequestAsync(request, cancellationToken);
    }

    /// <summary>
    /// 验证更新请求
    /// </summary>
    protected override async Task<ValidationResult> ValidateUpdateRequestAsync(
        int id,
        ProductDto request,
        CancellationToken cancellationToken)
    {
        if (request.Id.HasValue && request.Id.Value != id)
        {
            return ValidationResult.Fail("请求中的ID与路由中的ID不匹配");
        }

        return await base.ValidateUpdateRequestAsync(id, request, cancellationToken);
    }

    /// <summary>
    /// 检查是否可以删除
    /// </summary>
    protected override async Task<DeleteCheckResult> CanDeleteAsync(
        int id,
        CancellationToken cancellationToken)
    {
        // 检查产品是否有关联的订单
        // 如果产品已经被使用，则不能删除
        return await base.CanDeleteAsync(id, cancellationToken);
    }

    /// <summary>
    /// 自定义端点：根据价格范围查询产品
    /// </summary>
    [HttpGet("by-price-range")]
    public async Task<ActionResult<BaseResponse<List<ProductDto>>>> GetProductsByPriceRange(
        [FromQuery] decimal minPrice,
        [FromQuery] decimal maxPrice,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            {
                return BadRequest(BaseResponse<List<ProductDto>>.Error("价格范围参数无效"));
            }

            var filteredProducts = await ((ProductService)Service)
                .SearchProductsAsync(null, minPrice, maxPrice, null, cancellationToken);

            return Ok(BaseResponse<List<ProductDto>>.Success(filteredProducts, "获取成功"));
        }
        catch (Exception ex)
        {
            return HandleException(ex);
        }
    }
}
```

#### 10. 完整的 Program.cs 配置

```csharp
// 注册泛型仓储服务
builder.Services.AddGenericRepositoryEfCoreInMemory();

// 注册 AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// 注册业务服务
services.AddScoped<ProductService>();

// 注册控制器
builder.Services.AddControllers();
```

**完整示例项目**: [auditing-demo.zip](https://github.com/Fat-Snail/X-Net-Mod/blob/main/auditing-demo.zip)

### 8. 自动依赖注入
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

**额外依赖：**

```xml
<!-- 敏感词过滤功能 -->
<PackageReference Include="Microsoft.SemanticKernel" Version="1.0.0" />
<PackageReference Include="Microsoft.SemanticKernel.Connectors.OpenAI" Version="1.0.0" />
<PackageReference Include="ToolGood.Words" Version="3.1.0" />

<!-- 领域仓储层 -->
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />
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

// 添加健康检查
builder.AddCommonHealthChecks();
builder.AddDatabaseHealthCheck("app-db", "Data Source=app.db");
builder.AddExternalApiHealthCheck("external-service", "https://api.example.com/health");

var app = builder.Build();

// 映射健康检查端点
app.MapHealthCheckEndpoints();

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

### 5. 配置领域仓储层（可选）
```csharp
// 注册泛型仓储服务
builder.Services.AddGenericRepositoryEfCoreInMemory();

// 注册 AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// 注册业务服务
builder.Services.AddScoped<ProductService>();
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

### DomainCore
- `IRepository<TEntity, TKey>` - 泛型仓储接口
- `Repository<TEntity, TKey>` - 泛型仓储实现
- `IUnitOfWork` - 工作单元接口
- `UnitOfWork` - 工作单元实现
- `CrudServiceBase<TEntity, TKey, TDto>` - CRUD 服务基类
- `CURDControllerBase<TEntity, TKey, TDto>` - CRUD 控制器基类
- `AddGenericRepositoryEfCoreInMemory()` - 注册内存数据库仓储服务

### HealthCheckExtensions
- `AddCommonHealthChecks()` - 添加通用健康检查
- `AddDatabaseHealthCheck()` - 添加数据库健康检查
- `AddExternalApiHealthCheck()` - 添加外部 API 健康检查
- `AddCacheHealthCheck()` - 添加缓存健康检查
- `AddBusinessLogicHealthCheck()` - 添加自定义业务逻辑健康检查
- `MapHealthCheckEndpoints()` - 映射所有健康检查端点
- `GetHealthCheckConfiguration()` - 获取健康检查配置

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
- 领域仓储层（CRUD 服务和控制器）
- 健康检查端点配置

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