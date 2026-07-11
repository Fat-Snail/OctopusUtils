using OctopusEx.Sample.Worker;
using OctopusEx.WebCore.Events;
using OctopusEx.WebCore.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// ===== 事件总线（内存模式——单实例时开箱即用） =====
builder.Services.AddSimpleEventBus();

// ===== 事件处理器自动扫描 =====
// AddSimpleEventBus 已默认扫描 EntryAssembly，本项目的 handlers 会自动注册

// ===== 示例后台服务（演示独立 Worker 如何订阅事件） =====
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
