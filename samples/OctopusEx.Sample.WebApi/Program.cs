using Microsoft.EntityFrameworkCore;
using OctopusEx.WebCore.Diagnostics;
using OctopusEx.WebCore.Events;
using OctopusEx.WebCore.Extensions;
using OctopusEx.WebCore.Extensions.HealthChecks;
using OctopusEx.WebCore.Helpers;
using OctopusEx.WebCore.MultiTenancy;
using OctopusEx.Sample.WebApi;

var builder = WebApplication.CreateBuilder(args);

// ===== 1. 基础服务 =====
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ===== 2. JWT 认证 =====
builder.Services.AddSimpleJwt(options =>
{
    options.Secret = "OctopusSample-SuperSecretKey-AtLeast32Chars!";
    options.Issuer = "OctopusSample";
    options.Audience = "OctopusSample";
});

// ===== 3. 多租户 =====
builder.Services.AddSimpleMultiTenancy();

// ===== 4. EF Core（SQLite） =====
builder.Services.AddDbContext<SampleDbContext>(options =>
    options.UseSqlite("Data Source=octopus_sample.db"));

// ===== 5. 对象映射 =====
builder.Services.AddSimpleMapper();

// ===== 6. 内存缓存 =====
builder.Services.AddMemoryCache();
builder.Services.AddSimpleCache();

// ===== 7. 事件总线 + Outbox =====
builder.Services.AddSimpleEventBus();
builder.Services.AddOutbox();

// ===== 8. Hangfire 后台任务 =====
builder.Services.AddSimpleHangfire(workerCount: 2);

// ===== 9. 审计日志 =====
builder.Services.AddAuditing();

// ===== 10. 当前用户 =====
builder.Services.AddCurrentUser();

// ===== 11. 健康检查（v1.5.5 新增） =====
builder.AddOctopusCacheHealthCheck();
builder.AddEventBusHealthCheck(cfg =>
{
    cfg.DegradedThreshold = 5;
    cfg.UnhealthyThreshold = 20;
});
builder.AddOutboxHealthCheck(cfg =>
{
    cfg.DegradedThreshold = 50;
    cfg.UnhealthyThreshold = 200;
});
builder.AddTenantHealthCheck();
builder.AddCommonHealthChecks();

// ===== 12. 示例业务服务 =====
builder.Services.AddScoped<TodoService>();
builder.Services.AddScoped<ITenantAwareHangfireJob, TenantAwareHangfireJob>();

var app = builder.Build();

// 确保数据库已创建
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SampleDbContext>();
    db.Database.EnsureCreated();
}

// ===== 中间件管道 =====
// Sample 项目始终开放 OpenAPI 与诊断页，方便直接体验；生产项目应根据
// 环境限制 OpenAPI，并为诊断端点启用授权。
app.MapOpenApi();
app.MapOctopusDiagnostics(); // /octopus/diagnostics

app.UseGlobalExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseMultiTenancy();

// ===== 端点 =====
app.MapGet("/", () => Results.Content(SampleLandingPage.Html, "text/html; charset=utf-8"))
    .AllowAnonymous()
    .ExcludeFromDescription();
app.MapControllers();
app.MapHealthCheckEndpoints();

// ===== 启动时注册示例定时任务 =====
app.Lifetime.ApplicationStarted.Register(() =>
{
    using var scope2 = app.Services.CreateScope();
    scope2.ServiceProvider.AddRecurringJob("sample-heartbeat",
        () => Console.WriteLine($"[{DateTime.UtcNow:O}] Octopus Sample heartbeat"),
        "* * * * *");
});

app.Run();
